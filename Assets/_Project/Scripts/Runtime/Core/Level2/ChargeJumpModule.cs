using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class ChargeJumpModule : MonoBehaviour
{
    [Header("Input")]
    public KeyCode jumpKey = KeyCode.C;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundRadius = 0.18f;
    public LayerMask groundMask;

    [Header("Charge Parameters (Only Affects Distance)")]
    public float chargeRate = 8f;
    public float maxCharge = 12f;

    [Header("Force Split: Height Fixed, Distance Varies with Charge")]
    public float verticalImpulse = 4.0f;
    public float baseHorizontal = 2.0f;
    public float horizontalPerCharge = 1.0f;

    [Header("Direction & Feel")]
    public float airControlMultiplier = 0.2f;
    public float maxAirSpeed = 8f;

    [Header("Only Charge on Rocks")]
    public bool requireOnRock = true;
    public string rockTag = "Rock";

    [Header("Tutorial UI (Optional)")]
    public CanvasGroup hintGroup;
    public TMP_Text hintText;
    public Image chargeBar; // Filled Horizontal

    [Header("Enable/Disable by Zone")]
    public bool chargeEnabled = false;

    // ───────────── Sound Effects (Added/Modified) ─────────────
    [Header("SFX: Charge/Release")]
    [Tooltip("Scale sound effect played when holding (usually not looped, play once).")]
    public AudioClip chargeClip;
    [Range(0f,1f)] public float chargeVolume = 1f;
    [Tooltip("2D=not position-dependent; 3D=has directional sense (left/right ear).")]
    public bool chargeAs2D = true;
    [Tooltip("Whether to loop the scale sound when holding. Your material is a complete scale, usually recommend turning off.")]
    public bool chargeClipLoops = false;

    [Tooltip("Playback speed (achieved through pitch): >1 faster and sharper; <1 slower and lower.")]
    public float chargePlaybackSpeed = 1f;

    [Tooltip("Whether to let pitch follow charge progress (your material already has scales built-in, recommend turning off).")]
    public bool chargePitchFollowsCharge = false;
    [Tooltip("x:charge/maxCharge ∈[0,1], y:additional speed multiplier(pitch). Final pitch=chargePlaybackSpeed * curve value")]
    public AnimationCurve loopPitchOverCharge = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(1f, 1.8f)
    );

    [Tooltip("Whether to fade in when starting to hold; if off, instantly start at set volume.")]
    public bool chargeFadeInOnStart = false;
    public float chargeFadeInTime = 0.08f;

    [Tooltip("Whether to stop instantly when releasing (recommended). If off, fade out over time below.")]
    public bool stopChargeInstantOnRelease = true;
    public float chargeFadeOutTime = 0.08f; // only used when stopChargeInstantOnRelease=false

    [Space(6)]
    public AudioClip releaseClip;              // "spring sound" played when releasing
    [Range(0f,1f)] public float releaseVolume = 1f;
    public bool releaseAs2D = true;

    // ───────────── Internal ─────────────
    Rigidbody rb;
    float charge;
    bool charging;
    Vector3 chargeDir = Vector3.forward;

    AudioSource _sfxSrc;
    float _currentVol = 0f, _targetVol = 0f, _volVel = 0f;
    bool _loopActive = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!groundCheck) {
            var t = new GameObject("GroundCheck").transform;
            t.SetParent(transform);
            t.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = t;
        }
        SetHint(false);

        _sfxSrc = gameObject.AddComponent<AudioSource>();
        _sfxSrc.playOnAwake = false;
        _sfxSrc.loop = false;
        _sfxSrc.spatialBlend = 0f;
        _sfxSrc.volume = 0f;
    }

    void OnDisable()
    {
        StopChargeSfx(true); // stop immediately when component disabled
    }

    public void SetChargeEnabled(bool on)
    {
        chargeEnabled = on;
        SetHint(on);
        if (!on) StopCharge();
    }

    void Update()
    {
        bool grounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
        bool allowChargeNow = (!requireOnRock) || OnRock();

        if (hintGroup) hintGroup.alpha = chargeEnabled ? 1f : 0f;
        if (hintText)  hintText.text  = "Hold <b>V</b> to charge, release to jump farther.\nStill W to keep forward.";
        if (chargeBar) chargeBar.fillAmount = Mathf.Clamp01(charge / maxCharge);

        if (!chargeEnabled) { StopCharge(); return; }

        // start charging
        if (Input.GetKeyDown(jumpKey) && grounded && allowChargeNow)
        {
            charging = true;
            charge = 0f;

            Vector3 fwd = Camera.main ? Camera.main.transform.forward : transform.forward;
            chargeDir = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
            if (chargeDir.sqrMagnitude < 0.0001f) chargeDir = transform.forward;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(chargeDir, Vector3.up), 1f);

            StartChargeSfx();
        }

        // charging
        if (charging && Input.GetKey(jumpKey))
        {
            charge += chargeRate * Time.deltaTime;
            charge = Mathf.Min(charge, maxCharge);

            UpdateChargeSfx(charge / Mathf.Max(0.0001f, maxCharge));
        }

        // release and jump
        if (charging && Input.GetKeyUp(jumpKey))
        {
            // 1) first immediately stop scale sound or fade out according to settings
            StopChargeSfx(stopChargeInstantOnRelease);

            if (grounded && allowChargeNow)
            {
                DoJump(charge, chargeDir);
                // 2) simultaneously play spring sound
                PlayReleaseSfx();
            }

            StopCharge();
        }

        // air control
        if (!grounded) ApplyAirControl(chargeDir);

        TickVolumeFade();
    }

    void StopCharge() { charging = false; charge = 0f; }

    bool OnRock()
    {
        var hits = Physics.OverlapSphere(groundCheck.position, groundRadius, groundMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag(rockTag)) return true;
            if (hits[i].GetComponentInParent<RockSurface>() != null ||
                hits[i].GetComponent<RockSurface>() != null) return true;
        }
        return false;
    }

    void DoJump(float finalCharge, Vector3 planarDir)
    {
        GetComponent<RockTracker>()?.MarkJump();

        // [FIX] 轻微抬离地面，避免本帧接触解算把水平量抬成竖直
        rb.position += Vector3.up * 0.02f;

        // [FIX] 高度固定：由竖直“速度”决定；距离只随水平“速度”变化
        float horiz = baseHorizontal + finalCharge * horizontalPerCharge;
        float vert  = verticalImpulse;

        Vector3 planar = planarDir.normalized * horiz;

        // [FIX] 直接设置目标速度，保证高度不受蓄力影响
        rb.linearVelocity = new Vector3(planar.x, vert, planar.z);
    }

    void ApplyAirControl(Vector3 planarDir)
    {
        if (planarDir.sqrMagnitude < 0.0001f) return;

        // [FIX] 仅调整水平分量，不改动 y，避免被意外抬高
        Vector3 v  = rb.linearVelocity;                     // [FIX] velocity
        Vector3 pv = new Vector3(v.x, 0f, v.z);
        Vector3 wish = planarDir * maxAirSpeed;
        Vector3 add  = (wish - pv) * airControlMultiplier * Time.deltaTime * 10f;
        rb.linearVelocity = new Vector3(pv.x + add.x, v.y, pv.z + add.z);   // [FIX] velocity
    }

    void SetHint(bool show)
    {
        if (!hintGroup) return;
        hintGroup.alpha = show ? 1f : 0f;
        hintGroup.blocksRaycasts = false;
        hintGroup.ignoreParentGroups = true;
    }

    // ───────────── SFX Implementation ─────────────
    void StartChargeSfx()
    {
        if (!chargeClip) return;

        _sfxSrc.clip = chargeClip;
        _sfxSrc.loop = chargeClipLoops;
        _sfxSrc.spatialBlend = chargeAs2D ? 0f : 1f;

        // initial volume
        _targetVol = chargeVolume;
        if (chargeFadeInOnStart && chargeFadeInTime > 0f)
        {
            _currentVol = 0f;
        }
        else
        {
            _currentVol = _targetVol; // directly max out
        }
        _sfxSrc.volume = _currentVol;

        // initial pitch (playback speed)
        float basePitch = Mathf.Max(0.01f, chargePlaybackSpeed);
        if (chargePitchFollowsCharge)
        {
            // start from 0 progress curve
            _sfxSrc.pitch = basePitch * Mathf.Clamp(loopPitchOverCharge.Evaluate(0f), 0.01f, 3f);
        }
        else
        {
            _sfxSrc.pitch = basePitch;
        }

        if (!_sfxSrc.isPlaying) _sfxSrc.Play();
        _loopActive = true;
    }

    void UpdateChargeSfx(float charge01)
    {
        if (!_loopActive || !chargeClip) return;

        if (chargePitchFollowsCharge)
        {
            float basePitch = Mathf.Max(0.01f, chargePlaybackSpeed);
            float follow    = Mathf.Clamp(loopPitchOverCharge.Evaluate(Mathf.Clamp01(charge01)), 0.01f, 3f);
            _sfxSrc.pitch   = basePitch * follow;
        }
        // volume target stays chargeVolume (if need to raise with progress, can multiply coefficient here)
        _targetVol = chargeVolume;
    }

    void StopChargeSfx(bool immediate)
    {
        if (!_loopActive) return;

        if (immediate)
        {
            _sfxSrc.Stop();
            // don't permanently set volume to 0; just stop playback
            _targetVol = 0f;
            _currentVol = 0f;
            _loopActive = false;
        }
        else
        {
            _targetVol = 0f; // hand to TickVolumeFade to automatically Stop after fade out
        }
    }

    void PlayReleaseSfx()
    {
        if (!releaseClip) return;
    
        // backup current state
        float prevVol = _sfxSrc.volume;
        float prevPitch = _sfxSrc.pitch;
        float prevBlend = _sfxSrc.spatialBlend;
    
        // 2D/3D setup
        _sfxSrc.spatialBlend = releaseAs2D ? 0f : 1f;
    
        // key: PlayOneShot final volume = AudioSource.volume * parameter
        // temporarily pull source volume to 1 here to avoid being muted by previous 0
        _sfxSrc.volume = 1f;
        _sfxSrc.pitch = 1f; // spring sound usually doesn't need speed change
    
        _sfxSrc.PlayOneShot(releaseClip, releaseVolume);
    
        // restore
        _sfxSrc.volume = prevVol;
        _sfxSrc.pitch = prevPitch;
        _sfxSrc.spatialBlend = prevBlend;
    }

    void TickVolumeFade()
    {
        if (!_loopActive) return;

        // only smooth when need to fade in/out, otherwise keep current value
        float tau = (_targetVol > _currentVol)
            ? Mathf.Max(0.0001f, chargeFadeInOnStart ? chargeFadeInTime : 0f)
            : Mathf.Max(0.0001f, stopChargeInstantOnRelease ? 0f : chargeFadeOutTime);

        if (tau <= 0f)
        {
            _currentVol = _targetVol;
        }
        else
        {
            _currentVol = Mathf.SmoothDamp(_currentVol, _targetVol, ref _volVel, tau);
        }

        _currentVol = Mathf.Clamp01(_currentVol);
        _sfxSrc.volume = _currentVol;

        // stop after fading to 0
        if (_targetVol <= 0.0001f && _currentVol <= 0.0001f)
        {
            _sfxSrc.Stop();
            _loopActive = false;
        }
    }
}
