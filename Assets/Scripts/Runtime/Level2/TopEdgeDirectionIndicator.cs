using UnityEngine;
using UnityEngine.UI;

public class TopEdgeDirectionIndicator : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                    // If not set, use Camera.main
    public Transform target;              // Level end/Boss
    public RectTransform indicator;       // Top arrow Image/TMP etc.

    [Header("Layout (UI anchored at Top-Center)")]
    public float topMargin = 24f;         // Inner padding from top edge (pixels)
    public float sidePadding = 32f;       // Left/right inner padding, prevent touching screen edge

    [Header("Mapping")]
    [Tooltip("Map horizontal angle [-max,+max] to screen left/right, commonly set to 90 degrees")]
    public float maxAngle = 90f;          // View ±90° mapped to left/right boundaries
    public bool hideWhenVeryBehind = true;
    [Tooltip("Angle (degrees) behind target to start fading/hiding")]
    public float behindFadeAngle = 120f;

    [Header("Feel")]
    public float followLerp = 12f;        // Horizontal movement/rotation smoothing (larger = faster)

    public enum RotationMode { None, Tilt, Exact }
    [Header("Rotation")]
    public RotationMode rotationMode = RotationMode.Exact; // Default precise pointing
    [Tooltip("Maximum tilt angle in Tilt mode")]
    public float rotateMaxDeg = 25f;
    [Tooltip("Correction (degrees) when texture is not facing straight up, texture facing right usually fill -90")]
    public float rotationOffsetDeg = 0f;
    [Tooltip("Whether to force flip and continue pointing when behind (Exact mode)")]
    public bool flipWhenBehind = false;

    Canvas _canvas;
    RectTransform _canvasRT;
    CanvasGroup _grp;
    float _curX;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        _canvas = GetComponentInParent<Canvas>();
        _canvasRT = _canvas ? _canvas.GetComponent<RectTransform>() : null;

        if (indicator)
        {
            // Requirement: indicator anchor = Top Middle (X=0.5, Y=1)
            indicator.anchorMin = new Vector2(0.5f, 1f);
            indicator.anchorMax = new Vector2(0.5f, 1f);
            indicator.pivot     = new Vector2(0.5f, 0.5f);
            indicator.anchoredPosition = new Vector2(0f, -topMargin);
            _grp = indicator.GetComponent<CanvasGroup>();
            if (!_grp) _grp = indicator.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        if (!cam || !indicator || !target || _canvasRT == null) return;

        // Target in camera space coordinates (right x, up y, forward z)
        Vector3 toTargetWS = (target.position - cam.transform.position);
        Vector3 toTargetCS = cam.transform.InverseTransformDirection(toTargetWS.normalized);

        // Horizontal angle: right is positive, left is negative; directly ahead is 0°
        float angle = Mathf.Atan2(toTargetCS.x, toTargetCS.z) * Mathf.Rad2Deg;

        // —— Top horizontal position mapping —— //
        float t = Mathf.Clamp(angle / Mathf.Max(1f, maxAngle), -1f, 1f);
        float halfW = (_canvasRT.rect.width * 0.5f) - sidePadding;
        float targetX = t * halfW;

        // Smooth movement (unaffected by timeScale)
        _curX = Mathf.Lerp(_curX, targetX, 1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime));
        indicator.anchoredPosition = new Vector2(_curX, -topMargin); // Maintain top edge spacing

        // —— Rotation —— //
        switch (rotationMode)
        {
            case RotationMode.None:
                // No rotation
                break;

            case RotationMode.Tilt:
            {
                // Slight tilt: more like top bar indicator
                float rz = Mathf.Clamp(angle / Mathf.Max(1f, maxAngle), -1f, 1f) * rotateMaxDeg;
                float targetZ = -rz + rotationOffsetDeg; // Negative when texture faces up
                float z = Mathf.LerpAngle(indicator.localEulerAngles.z, targetZ,
                                          1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime));
                indicator.localEulerAngles = new Vector3(0, 0, z);
                break;
            }

            case RotationMode.Exact:
            {
                // Precise pointing: arrow truly points toward target (only uses horizontal angle)
                float yaw = angle;
                if (flipWhenBehind && toTargetCS.z < 0f) yaw += 180f; // Flip and continue pointing when behind
                float targetZ = -(yaw + rotationOffsetDeg);           // Texture defaults to facing up
                float z = Mathf.LerpAngle(indicator.localEulerAngles.z, targetZ,
                                          1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime));
                indicator.localEulerAngles = new Vector3(0, 0, z);
                break;
            }
        }

        // —— Fade/hide when behind (optional) —— //
        float absYaw = Mathf.Abs(angle);
        bool veryBehind = absYaw > behindFadeAngle || toTargetCS.z < -0.1f;

        if (hideWhenVeryBehind && !flipWhenBehind)
        {
            _grp.alpha = veryBehind ? 0f : 1f;
        }
        else if (!hideWhenVeryBehind)
        {
            // Gradual fade: start fading after 90°, fully hidden at 120° (you can adjust range as needed)
            float a = Mathf.InverseLerp(behindFadeAngle, 90f, absYaw);
            _grp.alpha = Mathf.Clamp01(a);
        }
        else
        {
            // When flipWhenBehind=true, usually keep visible
            _grp.alpha = 1f;
        }
    }
}
