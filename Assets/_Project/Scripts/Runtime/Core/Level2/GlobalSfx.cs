using UnityEngine;
using UnityEngine.Audio;

public class GlobalSfx : MonoBehaviour
{
    public static GlobalSfx Instance { get; private set; }

    [Header("Output (optional)")]
    public AudioMixerGroup output;          // Can route to your SFX mixer group

    // ───────────── Death SFX ─────────────
    [Header("Unified \"Death\" SFX")]
    public AudioClip deathClip;
    [Range(0f,1f)] public float deathVolume = 1f;
    [Tooltip("Rapid repeats within a short interval are suppressed")]
    public float deathCooldown = 0.2f;

    // ───────────── Cookie pickup SFX (new) ─────────────
    [Header("Cookie: Pickup SFX")]
    public AudioClip cookieClip;
    [Range(0f,1f)] public float cookieVolume = 1f;
    [Tooltip("Rate-limit frequent pickups (avoid audio spam)")]
    public float cookieCooldown = 0.03f;
    [Tooltip("true = 2D playback; false = 3D attenuation by world position")]
    public bool cookieAs2D = true;

    // ───────────── Shared spatial/3D params ─────────────
    [Header("Spatialization (shared 3D playback params)")]
    public bool playAs2D = true;            // Checked = default 2D (only affects the 'Death' static methods)
    [Range(0f,1f)] public float spatialBlend3D = 1f;
    public float minDistance = 1f;
    public float maxDistance = 30f;

    // ───────────── Leaf pickup SFX (new) ─────────────
    [Header("Leaf: Pickup SFX")]
    public AudioClip leafPickupClip;
    [Range(0f,1f)] public float leafPickupVolume = 1f;
    [Tooltip("Rate-limit frequent triggers (avoid audio spam)")]
    public float leafPickupCooldown = 0.03f;
    [Tooltip("true = 2D playback; false = 3D attenuation by world position")]
    public bool leafPickupAs2D = true;
    

    // Internals
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

    // ========== Public playback API ==========
    // Death
    public void PlayDeath(Vector3? worldPos = null, float? volumeOverride = null, bool? as2DOverride = null)
    {
        if (!deathClip) return;

        // Cooldown to avoid spam
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

    // Cookie pickup (new)
    public void PlayCookie(Vector3 worldPos, float? volumeOverride = null, bool? as2DOverride = null)
    {
        if (!cookieClip) return;

        // Rate limit (nearby rapid pickups won't stack explosively)
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

    // Instance method
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

    

    // Static convenience wrappers
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
    // Private helper: controlled 3D one-shot source
    AudioSource OneShot3D(AudioClip clip, Vector3 pos, float volume)
    {
        // Create a temporary source, configure with global 3D params, route to the same mixer group
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
        Destroy(go, clip.length + 0.1f); // Simple cleanup
        return a;
    }
}
