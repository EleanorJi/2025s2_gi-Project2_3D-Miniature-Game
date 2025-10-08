using UnityEngine;
using UnityEngine.Audio;

public class GlobalSfx : MonoBehaviour
{
    public static GlobalSfx Instance { get; private set; }

    [Header("输出(可选)")]
    public AudioMixerGroup output;          // 可接你的 SFX 混音组

    // ───────────── 死亡音效 ─────────────
    [Header("统一“死亡”音效")]
    public AudioClip deathClip;
    [Range(0f,1f)] public float deathVolume = 1f;
    [Tooltip("同一时间短间隔内的重复触发会被抑制")]
    public float deathCooldown = 0.2f;

    // ───────────── 吃饼干音效（新增） ─────────────
    [Header("Cookie: 拾取音效")]
    public AudioClip cookieClip;
    [Range(0f,1f)] public float cookieVolume = 1f;
    [Tooltip("短时间多次拾取的限流（防爆音）")]
    public float cookieCooldown = 0.03f;
    [Tooltip("true=2D播放；false=按世界位置做3D衰减")]
    public bool cookieAs2D = true;

    // ───────────── 空间化/3D参数（共用） ─────────────
    [Header("空间化（3D 播放通用参数）")]
    public bool playAs2D = true;            // 勾选=默认2D（仅对“死亡”静态方法生效）
    [Range(0f,1f)] public float spatialBlend3D = 1f;
    public float minDistance = 1f;
    public float maxDistance = 30f;

    // ───────────── 叶子拾取音效（新增） ─────────────
    [Header("Leaf: 拾取音效")]
    public AudioClip leafPickupClip;
    [Range(0f,1f)] public float leafPickupVolume = 1f;
    [Tooltip("短时间多次触发的限流（防爆音）")]
    public float leafPickupCooldown = 0.03f;
    [Tooltip("true=2D播放；false=按世界位置做3D衰减")]
    public bool leafPickupAs2D = true;
    

    // 内部
    AudioSource _src;
    float _lastDeathAt = -999f;
    float _lastCookieAt = -999f;
    float _lastLeafAt = -999f;


    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = false;
        if (output) _src.outputAudioMixerGroup = output;

        ApplySpatialSettings();
    }

    void OnValidate() { if (_src) ApplySpatialSettings(); }

    void ApplySpatialSettings()
    {
        if (!_src) return;
        _src.spatialBlend = playAs2D ? 0f : Mathf.Clamp01(spatialBlend3D);
        _src.minDistance  = Mathf.Max(0.01f, minDistance);
        _src.maxDistance  = Mathf.Max(_src.minDistance + 0.01f, maxDistance);
    }

    // ========== 对外播放接口 ==========
    // 死亡
    public void PlayDeath(Vector3? worldPos = null, float? volumeOverride = null, bool? as2DOverride = null)
    {
        if (!deathClip) return;

        // 冷却防炸音
        if (Time.unscaledTime - _lastDeathAt < deathCooldown) return;
        _lastDeathAt = Time.unscaledTime;

        float vol = Mathf.Clamp01(volumeOverride ?? deathVolume);
        bool as2D = as2DOverride ?? playAs2D;

        if (as2D || !worldPos.HasValue)
        {
            _src.PlayOneShot(deathClip, vol);
        }
        else
        {
            OneShot3D(deathClip, worldPos.Value, vol);
        }
    }

    // 拾取饼干（新增）
    public void PlayCookie(Vector3 worldPos, float? volumeOverride = null, bool? as2DOverride = null)
    {
        if (!cookieClip) return;

        // 限流（近距离连吃不会叠爆）
        if (Time.unscaledTime - _lastCookieAt < cookieCooldown) return;
        _lastCookieAt = Time.unscaledTime;

        float vol = Mathf.Clamp01(volumeOverride ?? cookieVolume);
        bool as2D = as2DOverride ?? cookieAs2D;

        if (as2D)
        {
            _src.PlayOneShot(cookieClip, vol);
        }
        else
        {
            OneShot3D(cookieClip, worldPos, vol);
        }
    }

    // 实例方法
    public void PlayLeafPickup(Vector3 worldPos, float? volumeOverride = null, bool? as2DOverride = null)
    {
        if (!leafPickupClip) return;

        if (Time.unscaledTime - _lastLeafAt < leafPickupCooldown) return;
        _lastLeafAt = Time.unscaledTime;

        float vol = Mathf.Clamp01(volumeOverride ?? leafPickupVolume);
        bool as2D = as2DOverride ?? leafPickupAs2D;

        if (as2D)
        {
            _src.PlayOneShot(leafPickupClip, vol);
        }
        else
        {
            OneShot3D(leafPickupClip, worldPos, vol);
        }
    }

    

    // ========== 静态便捷封装 ==========
    public static void PlayDeathSfx(Vector3? worldPos = null, float? volumeOverride = null, bool? as2DOverride = null)
    {
        if (Instance) Instance.PlayDeath(worldPos, volumeOverride, as2DOverride);
    }

    public static void PlayCookieSfx(Vector3 worldPos, float? volumeOverride = null, bool? as2DOverride = null)
    {
        if (Instance) Instance.PlayCookie(worldPos, volumeOverride, as2DOverride);
    }

    public static void PlayLeafPickupSfx(Vector3 worldPos, float? volumeOverride = null, bool? as2DOverride = null)
    {
        if (Instance) Instance.PlayLeafPickup(worldPos, volumeOverride, as2DOverride);
    }
    // ========== 私有工具：受控的 3D 一次性音源 ==========
    AudioSource OneShot3D(AudioClip clip, Vector3 pos, float volume)
    {
        // 创建临时音源，按全局 3D 参数配置，接同一个输出混音组
        var go = new GameObject($"_SFX3D_{clip.name}");
        go.transform.position = pos;

        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = false;
        a.clip = clip;
        a.volume = volume;
        a.spatialBlend = Mathf.Clamp01(spatialBlend3D);
        a.minDistance  = Mathf.Max(0.01f, minDistance);
        a.maxDistance  = Mathf.Max(a.minDistance + 0.01f, maxDistance);
        if (output) a.outputAudioMixerGroup = output;

        a.Play();
        Destroy(go, clip.length + 0.1f); // 简单回收
        return a;
    }
}
