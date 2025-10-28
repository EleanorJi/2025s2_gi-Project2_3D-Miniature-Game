using UnityEngine;
using UnityEngine.UI;

public class TopEdgeDirectionIndicator : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                    // 不设就用 Camera.main
    public Transform target;              // 关卡终点/Boss
    public RectTransform indicator;       // 顶部的箭头 Image/TMP等

    [Header("Layout (UI anchored at Top-Center)")]
    public float topMargin = 24f;         // 距离顶边的内边距（像素）
    public float sidePadding = 32f;       // 左右内边距，防止顶到屏幕边缘

    [Header("Mapping")]
    [Tooltip("把水平夹角[-max,+max]映射到屏幕左右，常设90度")]
    public float maxAngle = 90f;          // 视线±90°映射到左右边界
    public bool hideWhenVeryBehind = true;
    [Tooltip("目标在身后多少角度(度)开始淡出/隐藏")]
    public float behindFadeAngle = 120f;

    [Header("Feel")]
    public float followLerp = 12f;        // 水平移动/旋转平滑（越大越快）

    public enum RotationMode { None, Tilt, Exact }
    [Header("Rotation")]
    public RotationMode rotationMode = RotationMode.Exact; // 默认精准指向
    [Tooltip("Tilt模式下的最大倾角")]
    public float rotateMaxDeg = 25f;
    [Tooltip("贴图不是正朝上时的修正(度)，贴图朝右一般填-90")]
    public float rotationOffsetDeg = 0f;
    [Tooltip("在身后时是否强制掉头继续指向（Exact模式）")]
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
            // 要求：indicator锚点=Top Middle（X=0.5, Y=1）
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

        // 目标在相机空间坐标（右x、上y、前z）
        Vector3 toTargetWS = (target.position - cam.transform.position);
        Vector3 toTargetCS = cam.transform.InverseTransformDirection(toTargetWS.normalized);

        // 水平角：右为正、左为负；正前方为0°
        float angle = Mathf.Atan2(toTargetCS.x, toTargetCS.z) * Mathf.Rad2Deg;

        // —— 顶端水平位置映射 —— //
        float t = Mathf.Clamp(angle / Mathf.Max(1f, maxAngle), -1f, 1f);
        float halfW = (_canvasRT.rect.width * 0.5f) - sidePadding;
        float targetX = t * halfW;

        // 平滑移动（不受 timeScale 影响）
        _curX = Mathf.Lerp(_curX, targetX, 1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime));
        indicator.anchoredPosition = new Vector2(_curX, -topMargin); // 保持顶边间距

        // —— 旋转 —— //
        switch (rotationMode)
        {
            case RotationMode.None:
                // 不转
                break;

            case RotationMode.Tilt:
            {
                // 轻微倾斜：更像顶栏指示条
                float rz = Mathf.Clamp(angle / Mathf.Max(1f, maxAngle), -1f, 1f) * rotateMaxDeg;
                float targetZ = -rz + rotationOffsetDeg; // 贴图朝上时取负号
                float z = Mathf.LerpAngle(indicator.localEulerAngles.z, targetZ,
                                          1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime));
                indicator.localEulerAngles = new Vector3(0, 0, z);
                break;
            }

            case RotationMode.Exact:
            {
                // 精准指向：箭头真实朝向目标（仅用水平角）
                float yaw = angle;
                if (flipWhenBehind && toTargetCS.z < 0f) yaw += 180f; // 身后时掉头继续指向
                float targetZ = -(yaw + rotationOffsetDeg);           // 贴图默认朝上
                float z = Mathf.LerpAngle(indicator.localEulerAngles.z, targetZ,
                                          1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime));
                indicator.localEulerAngles = new Vector3(0, 0, z);
                break;
            }
        }

        // —— 身后淡出/隐藏（可选） —— //
        float absYaw = Mathf.Abs(angle);
        bool veryBehind = absYaw > behindFadeAngle || toTargetCS.z < -0.1f;

        if (hideWhenVeryBehind && !flipWhenBehind)
        {
            _grp.alpha = veryBehind ? 0f : 1f;
        }
        else if (!hideWhenVeryBehind)
        {
            // 渐隐：90°后开始淡，120°全隐（你也可以按需改范围）
            float a = Mathf.InverseLerp(behindFadeAngle, 90f, absYaw);
            _grp.alpha = Mathf.Clamp01(a);
        }
        else
        {
            // flipWhenBehind=true 时通常保持可见
            _grp.alpha = 1f;
        }
    }
}
