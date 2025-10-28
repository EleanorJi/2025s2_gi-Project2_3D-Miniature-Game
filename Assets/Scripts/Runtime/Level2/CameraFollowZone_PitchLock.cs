using UnityEngine;
using System.Collections;
using System.Reflection;

[RequireComponent(typeof(Collider))]
public class CameraFollowZone_PitchLock : MonoBehaviour
{
    [Header("References")]
    public MonoBehaviour cameraFollowScript;   // drag the camera follow script on main camera
    public Transform cameraTransform;          // can be empty, will automatically get Camera.main

    [Header("Offset (Position)")]
    public bool overrideOffset = true;
    public Vector3 offsetInZone = new Vector3(0f, 0.08f, -0.4f);
    public float smoothTime = 0.25f;           // smooth time when entering/exiting zone (0 = immediate)

    public enum PitchSource { FixedAngle, FromOffset }
    [Header("Pitch (Up/Down Angle)")]
    public bool lockPitch = true;              // lock up/down
    public PitchSource pitchMode = PitchSource.FromOffset;
    public float fixedPitchDeg = 10f;          // used when PitchSource=FixedAngle
    public float pitchSign = 1f;               // if direction is opposite, can set to -1

    // —— internal cache —— //
    Vector3 _oldOffset;
    float _oldMinPitch, _oldMaxPitch;
    FieldInfo _fOffset, _fMinPitch, _fMaxPitch;
    Coroutine _blendCo;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        if (!cameraFollowScript) return;

        // get fields via reflection (case insensitive, just need to contain keyword)
        _fOffset   = FindField(cameraFollowScript, "offset",   typeof(Vector3));
        _fMinPitch = FindField(cameraFollowScript, "minpitch", typeof(float));
        _fMaxPitch = FindField(cameraFollowScript, "maxpitch", typeof(float));

        // record initial values
        if (_fOffset   != null) _oldOffset   = (Vector3)_fOffset.GetValue(cameraFollowScript);
        if (_fMinPitch != null) _oldMinPitch = (float)_fMinPitch.GetValue(cameraFollowScript);
        if (_fMaxPitch != null) _oldMaxPitch = (float)_fMaxPitch.GetValue(cameraFollowScript);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!cameraFollowScript) return;
        if (!IsPlayer(other)) return;

        // 1) Offset transition
        if (overrideOffset && _fOffset != null)
        {
            StopBlend();
            if (smoothTime > 0f)
                _blendCo = StartCoroutine(BlendOffset((Vector3)_fOffset.GetValue(cameraFollowScript), offsetInZone, smoothTime));
            else
                _fOffset.SetValue(cameraFollowScript, offsetInZone);
        }

        // 2) Pitch lock (don't take entry angle; use fixed angle or derive from offset)
        if (lockPitch && _fMinPitch != null && _fMaxPitch != null)
        {
            float targetPitch = (pitchMode == PitchSource.FixedAngle)
                ? fixedPitchDeg
                : PitchFromOffset(offsetInZone) * pitchSign;

            _fMinPitch.SetValue(cameraFollowScript, targetPitch);
            _fMaxPitch.SetValue(cameraFollowScript, targetPitch);
            // don't change yaw, so mouse left/right still works
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!cameraFollowScript) return;
        if (!IsPlayer(other)) return;

        // restore Offset
        if (overrideOffset && _fOffset != null)
        {
            StopBlend();
            if (smoothTime > 0f)
                _blendCo = StartCoroutine(BlendOffset((Vector3)_fOffset.GetValue(cameraFollowScript), _oldOffset, smoothTime));
            else
                _fOffset.SetValue(cameraFollowScript, _oldOffset);
        }

        // restore Pitch limits
        if (lockPitch && _fMinPitch != null && _fMaxPitch != null)
        {
            _fMinPitch.SetValue(cameraFollowScript, _oldMinPitch);
            _fMaxPitch.SetValue(cameraFollowScript, _oldMaxPitch);
        }
    }

    // —— utility functions —— //

    FieldInfo FindField(MonoBehaviour mb, string keyword, System.Type type)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        string k = keyword.ToLower();
        foreach (var f in mb.GetType().GetFields(flags))
        {
            if (f.FieldType == type && f.Name.ToLower().Contains(k))
                return f;
        }
        return null;
    }

    bool IsPlayer(Collider c)
    {
        var root = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform;
        return root.CompareTag("Player");
    }

    // estimate appropriate pitch from offset: pitch = atan2(y, √(x²+z²)) (in degrees)
    float PitchFromOffset(Vector3 off)
    {
        float horiz = Mathf.Sqrt(off.x * off.x + off.z * off.z);
        float ang = Mathf.Atan2(off.y, horiz) * Mathf.Rad2Deg;
        return ang;
    }

    IEnumerator BlendOffset(Vector3 from, Vector3 to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            var v = Vector3.Lerp(from, to, Mathf.Clamp01(t / dur));
            _fOffset.SetValue(cameraFollowScript, v);
            yield return null;
        }
        _fOffset.SetValue(cameraFollowScript, to);
        _blendCo = null;
    }

    void StopBlend()
    {
        if (_blendCo != null) { StopCoroutine(_blendCo); _blendCo = null; }
    }
}
