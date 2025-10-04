using UnityEngine;
using System.Collections;
using System.Reflection;

[RequireComponent(typeof(Collider))]
public class CameraFollowZone_PitchLock : MonoBehaviour
{
    [Header("引用")]
    public MonoBehaviour cameraFollowScript;   // 拖主相机上的相机跟随脚本
    public Transform cameraTransform;          // 可留空，自动取 Camera.main

    [Header("Offset（位置）")]
    public bool overrideOffset = true;
    public Vector3 offsetInZone = new Vector3(0f, 0.08f, -0.4f);
    public float smoothTime = 0.25f;           // 进出区平滑时间（0 = 立即）

    public enum PitchSource { FixedAngle, FromOffset }
    [Header("Pitch（上下角度）")]
    public bool lockPitch = true;              // 锁上下
    public PitchSource pitchMode = PitchSource.FromOffset;
    public float fixedPitchDeg = 10f;          // PitchSource=FixedAngle 时使用
    public float pitchSign = 1f;               // 如方向相反，可设为 -1

    // —— 内部缓存 —— //
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

        // 反射拿字段（大小写不敏感，只要包含关键字）
        _fOffset   = FindField(cameraFollowScript, "offset",   typeof(Vector3));
        _fMinPitch = FindField(cameraFollowScript, "minpitch", typeof(float));
        _fMaxPitch = FindField(cameraFollowScript, "maxpitch", typeof(float));

        // 记录初始值
        if (_fOffset   != null) _oldOffset   = (Vector3)_fOffset.GetValue(cameraFollowScript);
        if (_fMinPitch != null) _oldMinPitch = (float)_fMinPitch.GetValue(cameraFollowScript);
        if (_fMaxPitch != null) _oldMaxPitch = (float)_fMaxPitch.GetValue(cameraFollowScript);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!cameraFollowScript) return;
        if (!IsPlayer(other)) return;

        // 1) Offset 过渡
        if (overrideOffset && _fOffset != null)
        {
            StopBlend();
            if (smoothTime > 0f)
                _blendCo = StartCoroutine(BlendOffset((Vector3)_fOffset.GetValue(cameraFollowScript), offsetInZone, smoothTime));
            else
                _fOffset.SetValue(cameraFollowScript, offsetInZone);
        }

        // 2) Pitch 锁定（不取进入角度；用固定角或offset推导）
        if (lockPitch && _fMinPitch != null && _fMaxPitch != null)
        {
            float targetPitch = (pitchMode == PitchSource.FixedAngle)
                ? fixedPitchDeg
                : PitchFromOffset(offsetInZone) * pitchSign;

            _fMinPitch.SetValue(cameraFollowScript, targetPitch);
            _fMaxPitch.SetValue(cameraFollowScript, targetPitch);
            // 不改 yaw，因此鼠标左右仍可用
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!cameraFollowScript) return;
        if (!IsPlayer(other)) return;

        // 还原 Offset
        if (overrideOffset && _fOffset != null)
        {
            StopBlend();
            if (smoothTime > 0f)
                _blendCo = StartCoroutine(BlendOffset((Vector3)_fOffset.GetValue(cameraFollowScript), _oldOffset, smoothTime));
            else
                _fOffset.SetValue(cameraFollowScript, _oldOffset);
        }

        // 还原 Pitch 限制
        if (lockPitch && _fMinPitch != null && _fMaxPitch != null)
        {
            _fMinPitch.SetValue(cameraFollowScript, _oldMinPitch);
            _fMaxPitch.SetValue(cameraFollowScript, _oldMaxPitch);
        }
    }

    // —— 工具函数 —— //

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

    // 根据 offset 估出合适 pitch：pitch = atan2(y, √(x²+z²))（角度）
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
