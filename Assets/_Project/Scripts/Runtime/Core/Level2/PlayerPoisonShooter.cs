using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

[RequireComponent(typeof(ParticleSystem))]
public class PlayerPoisonShooter : MonoBehaviour
{
    public ParticleSystem poisonPS;        // 拖当前这个PS

    [Header("Spray")]
    public float sprayRate = 220f;         // 每秒发射的粒子数（像水枪）
    public float coneAngle = 3f;           // 发散角（度）

    // ---------------- SFX：喷射循环音 ----------------
    [Header("SFX (Spray Loop)")]
    public AudioClip sprayLoopClip;                    // 喷射时循环播放
    [Range(0f,1f)] public float sprayVolume = 1f;      // 喷射音量
    public bool sprayAs2D = true;                      // true=2D；false=3D
    public AudioMixerGroup output;                     // 可选：路由到你的 SFX Mixer
    [Tooltip("淡入时间（按下P）")]
    public float fadeInTime = 0.05f;
    [Tooltip("淡出时间（松开P）；设为0则立刻停音")]
    public float fadeOutTime = 0.0f;

    [Header("3D Settings (when sprayAs2D = false)")]
    [Range(0f,1f)] public float spatialBlend3D = 1f;
    public float minDistance3D = 1f;
    public float maxDistance3D = 15f;

    private ParticleSystem.EmissionModule _emission;
    private ParticleSystem.ShapeModule _shape;
    private bool _spraying;
    private readonly List<ParticleCollisionEvent> _events = new();

    // ---- 内部: 音频淡入淡出状态 ----
    AudioSource _sfx;
    float _volCurrent = 0f, _volTarget = 0f, _volVel = 0f;

    void Awake()
    {
        if (!poisonPS) poisonPS = GetComponent<ParticleSystem>();
        _emission = poisonPS.emission;
        _shape = poisonPS.shape;

        _emission.rateOverTime = 0f;       // 由脚本控制
        _shape.angle = coneAngle;          // 运行时也能改

        // 音源（专用）
        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.loop = true;
        _sfx.clip = sprayLoopClip;
        _sfx.volume = 0f;
        if (output) _sfx.outputAudioMixerGroup = output;

        ApplySpatial();
    }

    void OnValidate()
    {
        if (_sfx)
        {
            _sfx.clip = sprayLoopClip;
            _sfx.loop = true;
            if (output) _sfx.outputAudioMixerGroup = output;
            ApplySpatial();
        }
    }

    void ApplySpatial()
    {
        if (!_sfx) return;
        if (sprayAs2D)
        {
            _sfx.spatialBlend = 0f;
        }
        else
        {
            _sfx.spatialBlend = Mathf.Clamp01(spatialBlend3D);
            _sfx.rolloffMode  = AudioRolloffMode.Linear;
            _sfx.minDistance  = Mathf.Max(0.01f, minDistance3D);
            _sfx.maxDistance  = Mathf.Max(_sfx.minDistance + 0.01f, maxDistance3D);
        }
    }

    void Update()
    {
        // 开始喷
        if (Input.GetKeyDown(KeyCode.P)) StartSpray();
        // 松开停
        if (Input.GetKeyUp(KeyCode.P)) StopSpray();

        // 允许运行时调锥角
        if (_shape.angle != coneAngle) _shape.angle = coneAngle;

        TickSfxVolume(); // 每帧处理淡入淡出
    }

    void StartSpray()
    {
        _spraying = true;

        // 粒子
        _emission.rateOverTime = sprayRate;
        if (!poisonPS.isPlaying) poisonPS.Play();

        // 音效（淡入并保证在播）
        if (sprayLoopClip)
        {
            _sfx.clip = sprayLoopClip;
            ApplySpatial();
            if (!_sfx.isPlaying) _sfx.Play();
            _volTarget = sprayVolume; // 淡入到目标音量
        }
    }

    void StopSpray()
    {
        _spraying = false;

        // 粒子
        _emission.rateOverTime = 0f;
        poisonPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 音效（淡出或立即停）
        if (sprayLoopClip)
        {
            if (fadeOutTime <= 0f)
            {
                _volTarget = 0f;
                _volCurrent = 0f;
                if (_sfx.isPlaying) _sfx.Stop(); // 立刻停
            }
            else
            {
                _volTarget = 0f; // 走淡出
            }
        }
    }

    void TickSfxVolume()
    {
        if (!sprayLoopClip) return;

        // 选淡入/淡出的时间常数
        float tau = (_volTarget > _volCurrent)
                    ? Mathf.Max(0.0001f, fadeInTime)
                    : Mathf.Max(0.0001f, fadeOutTime);

        _volCurrent = Mathf.SmoothDamp(_volCurrent, _volTarget, ref _volVel, tau);
        _volCurrent = Mathf.Clamp01(_volCurrent);
        _sfx.volume = _volCurrent;

        // 完全淡出后自动 Stop
        if (!_spraying && _sfx.isPlaying && _volCurrent <= 0.0005f)
            _sfx.Stop();
    }

    // 命中回调：维持你之前的判定
    void OnParticleCollision(GameObject other)
    {
        int count = ParticlePhysicsExtensions.GetCollisionEvents(poisonPS, other, _events);
        var death = other.GetComponent<InsectDeath>() ?? other.GetComponentInParent<InsectDeath>();
        if (death != null) death.Kill();
        _events.Clear();
    }
}
