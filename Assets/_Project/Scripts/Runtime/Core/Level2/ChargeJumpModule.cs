using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class ChargeJumpModule : MonoBehaviour
{
    [Header("输入")]
    public KeyCode jumpKey = KeyCode.C;

    [Header("落地检测")]
    public Transform groundCheck;
    public float groundRadius = 0.18f;
    public LayerMask groundMask;

    [Header("蓄力参数（只影响距离）")]
    public float chargeRate = 8f;
    public float maxCharge = 12f;

    [Header("力度拆分：高度固定，距离随蓄力")]
    public float verticalImpulse = 4.0f;
    public float baseHorizontal = 2.0f;
    public float horizontalPerCharge = 1.0f;

    [Header("方向 & 手感")]
    public float airControlMultiplier = 0.2f;
    public float maxAirSpeed = 8f;

    [Header("仅在岩石上可蓄力")]
    public bool requireOnRock = true;
    public string rockTag = "Rock";

    [Header("教程UI（可选）")]
    public CanvasGroup hintGroup;
    public TMP_Text hintText;
    public Image chargeBar; // Filled Horizontal

    [Header("由区域开启/关闭")]
    public bool chargeEnabled = false;

    // ───────────── 音效（新增/修改） ─────────────
    [Header("SFX：蓄力/释放")]
    [Tooltip("按住时播放的音阶音效（一般不循环，播一遍）。")]
    public AudioClip chargeClip;
    [Range(0f,1f)] public float chargeVolume = 1f;
    [Tooltip("2D=不随位置；3D=有方向感（左右耳）。")]
    public bool chargeAs2D = true;
    [Tooltip("是否让音阶在按住时循环。你的素材是整段音阶，通常建议关闭。")]
    public bool chargeClipLoops = false;

    [Tooltip("播放倍率（通过 pitch 实现）：>1 更快更尖；<1 更慢更低。")]
    public float chargePlaybackSpeed = 1f;

    [Tooltip("是否让音高跟随蓄力进度升高（你的素材本身已做音阶，建议关闭）。")]
    public bool chargePitchFollowsCharge = false;
    [Tooltip("x:charge/maxCharge ∈[0,1], y:附加倍速(pitch)。最终pitch=chargePlaybackSpeed * 曲线值")]
    public AnimationCurve loopPitchOverCharge = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(1f, 1.8f)
    );

    [Tooltip("开始按住时是否淡入；若关，则瞬间以设定音量开始。")]
    public bool chargeFadeInOnStart = false;
    public float chargeFadeInTime = 0.08f;

    [Tooltip("松手时是否瞬停（推荐）。若关，则按下方时间淡出。")]
    public bool stopChargeInstantOnRelease = true;
    public float chargeFadeOutTime = 0.08f; // 只在 stopChargeInstantOnRelease=false 时用

    [Space(6)]
    public AudioClip releaseClip;              // 松手播放的“弹簧音”
    [Range(0f,1f)] public float releaseVolume = 1f;
    public bool releaseAs2D = true;

    // ───────────── 内部 ─────────────
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
        StopChargeSfx(true); // 组件禁用立停
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
        if (hintText)  hintText.text  = "Hold <b>V</b> to charge, release to jump farther.\nNo W needed.";
        if (chargeBar) chargeBar.fillAmount = Mathf.Clamp01(charge / maxCharge);

        if (!chargeEnabled) { StopCharge(); return; }

        // 开始蓄力
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

        // 蓄力中
        if (charging && Input.GetKey(jumpKey))
        {
            charge += chargeRate * Time.deltaTime;
            charge = Mathf.Min(charge, maxCharge);

            UpdateChargeSfx(charge / Mathf.Max(0.0001f, maxCharge));
        }

        // 松手起跳
        if (charging && Input.GetKeyUp(jumpKey))
        {
            // 1) 先立刻停掉音阶 or 按设置淡出
            StopChargeSfx(stopChargeInstantOnRelease);

            if (grounded && allowChargeNow)
            {
                DoJump(charge, chargeDir);
                // 2) 同时播弹簧音
                PlayReleaseSfx();
            }

            StopCharge();
        }

        // 空中微调
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

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float horiz = baseHorizontal + finalCharge * horizontalPerCharge;
        float vert  = verticalImpulse;

        Vector3 impulse = planarDir.normalized * horiz + Vector3.up * vert;
        rb.AddForce(impulse, ForceMode.Impulse);
    }

    void ApplyAirControl(Vector3 planarDir)
    {
        if (planarDir.sqrMagnitude < 0.0001f) return;
        Vector3 v = rb.linearVelocity;
        Vector3 pv = new Vector3(v.x, 0f, v.z);
        Vector3 wish = planarDir * maxAirSpeed;
        Vector3 add = (wish - pv) * airControlMultiplier * Time.deltaTime * 10f;
        rb.linearVelocity = new Vector3(pv.x + add.x, v.y, pv.z + add.z);
    }

    void SetHint(bool show)
    {
        if (!hintGroup) return;
        hintGroup.alpha = show ? 1f : 0f;
        hintGroup.blocksRaycasts = false;
        hintGroup.ignoreParentGroups = true;
    }

    // ───────────── SFX 实现 ─────────────
    void StartChargeSfx()
    {
        if (!chargeClip) return;

        _sfxSrc.clip = chargeClip;
        _sfxSrc.loop = chargeClipLoops;
        _sfxSrc.spatialBlend = chargeAs2D ? 0f : 1f;

        // 初始音量
        _targetVol = chargeVolume;
        if (chargeFadeInOnStart && chargeFadeInTime > 0f)
        {
            _currentVol = 0f;
        }
        else
        {
            _currentVol = _targetVol; // 直接顶满
        }
        _sfxSrc.volume = _currentVol;

        // 初始 pitch（播放倍率）
        float basePitch = Mathf.Max(0.01f, chargePlaybackSpeed);
        if (chargePitchFollowsCharge)
        {
            // 从 0 进度的曲线开始
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
        // 音量目标保持 chargeVolume（如需随进度再抬，可在此乘系数）
        _targetVol = chargeVolume;
    }

    void StopChargeSfx(bool immediate)
    {
        if (!_loopActive) return;

        if (immediate)
        {
            _sfxSrc.Stop();
            // 不再把 volume 永久置 0；仅停止播放
            _targetVol = 0f;
            _currentVol = 0f;
            _loopActive = false;
        }
        else
        {
            _targetVol = 0f; // 交给 TickVolumeFade 淡出后自动 Stop
        }
    }

    void PlayReleaseSfx()
    {
        if (!releaseClip) return;
    
        // 备份当前状态
        float prevVol = _sfxSrc.volume;
        float prevPitch = _sfxSrc.pitch;
        float prevBlend = _sfxSrc.spatialBlend;
    
        // 2D/3D 设置
        _sfxSrc.spatialBlend = releaseAs2D ? 0f : 1f;
    
        // 关键：PlayOneShot 的最终音量 = AudioSource.volume * 参数
        // 这里临时把源音量拉到 1，避免被之前的 0 静音
        _sfxSrc.volume = 1f;
        _sfxSrc.pitch = 1f; // 一般弹簧音不用变速
    
        _sfxSrc.PlayOneShot(releaseClip, releaseVolume);
    
        // 还原
        _sfxSrc.volume = prevVol;
        _sfxSrc.pitch = prevPitch;
        _sfxSrc.spatialBlend = prevBlend;
    }

    void TickVolumeFade()
    {
        if (!_loopActive) return;

        // 只在需要淡入/淡出时平滑，否则保持当前值
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

        // 淡出到0后停
        if (_targetVol <= 0.0001f && _currentVol <= 0.0001f)
        {
            _sfxSrc.Stop();
            _loopActive = false;
        }
    }
}
