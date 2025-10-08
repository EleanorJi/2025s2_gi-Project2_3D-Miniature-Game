using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class AmbientZoneTrigger : MonoBehaviour
{
    [Header("Clip & Routing")]
    public AudioClip clip;
    public AudioMixerGroup output;
    [Range(0f,1f)] public float baseVolume = 1f;
    public bool loop = true;

    [Header("玩家定位")]
    public Transform playerOverride;
    public string playerTag = "Player";
    public float reFindPlayerInterval = 1.0f;

    [Header("范围")]
    [Tooltip("半径（世界单位）")]
    public float radius = 10f;
    [Tooltip("是否叠加缩放：半径 × max(lossyScale)")]
    public bool scaleWithTransform = false;

    [Header("淡入/淡出")]
    public float fadeInTime  = 0.35f;
    public float fadeOutTime = 0.35f;

    [Header("距离→音量（t=0=边缘, t=1=中心）")]
    // ⚠️ 默认曲线也改成 0->0, 1->1（边缘0音量，中心1音量）
    public AnimationCurve distanceToVolume = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(1f, 1f)
    );

    [Tooltip("边缘静音带（米）：从外半径往里这么多米音量保持为0，再开始升高")]
    public float edgeFeatherMeters = 0f;

    [Header("2D/3D 声场")]
    public bool use3D = false;
    [Range(0f,1f)] public float spatialBlend3D = 1f;
    public float minDistance3D = 1f;
    public float maxDistance3D = 30f;

    [Header("启动控制")]
    public bool onlyAfterGameStarted = true;
    public bool autoGameStartOnStart = false;

    [Header("调试")]
    public bool debugLogs = false;

    AudioSource _src;
    Transform _player;
    float _reFindTimer;
    float _currentVol = 0f;
    float _targetVol  = 0f;
    float _fadeVel;
    bool  _gameStarted = false;

    void Reset()
    {
        _src = GetComponent<AudioSource>();
        if (!_src) _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = true;
        _src.spatialBlend = 0f; // 默认2D
    }

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.Stop();
        _src.volume = 0f;
        _currentVol = 0f;

        ApplyRouting();
        ResolvePlayer(initial:true);
    }

    void Start()
    {
        if (autoGameStartOnStart) _gameStarted = true;
    }

    void OnEnable()
    {
        ApplyRouting();
        _src.Stop();
        _src.volume = 0f;
        _currentVol = 0f;
        ResolvePlayer(initial:true);
    }

    void OnValidate()
    {
        if (!_src) _src = GetComponent<AudioSource>();
        if (_src)
        {
            _src.loop = loop;
            _src.spatialBlend = use3D ? Mathf.Clamp01(spatialBlend3D) : 0f;
            _src.rolloffMode  = AudioRolloffMode.Linear;
            _src.minDistance  = Mathf.Max(0.01f, minDistance3D);
            _src.maxDistance  = Mathf.Max(_src.minDistance + 0.01f, maxDistance3D);
            if (output) _src.outputAudioMixerGroup = output;
        }
    }

    void ApplyRouting()
    {
        if (!_src) return;
        _src.clip = clip;
        _src.loop = loop;
        if (output) _src.outputAudioMixerGroup = output;

        if (use3D)
        {
            _src.spatialBlend = Mathf.Clamp01(spatialBlend3D);
            _src.rolloffMode  = AudioRolloffMode.Linear;
            _src.minDistance  = Mathf.Max(0.01f, minDistance3D);
            _src.maxDistance  = Mathf.Max(_src.minDistance + 0.01f, maxDistance3D);
        }
        else
        {
            _src.spatialBlend = 0f; // 2D：仅脚本控制音量
        }
    }

    void Update()
    {
        // 找玩家（掉引用时也能恢复）
        if (!_player)
        {
            _reFindTimer -= Time.unscaledDeltaTime;
            if (_reFindTimer <= 0f) { ResolvePlayer(); _reFindTimer = reFindPlayerInterval; }
        }

        bool canPlay = (!onlyAfterGameStarted || _gameStarted) && _player && clip;

        if (canPlay)
        {
            float r = GetWorldRadius();
            float dist = Vector3.Distance(_player.position, transform.position);
            bool inside = dist <= r;

            if (inside)
            {
                // —— 关键：反向映射，边缘=0，中心=1 —— //
                // 原先是 dist/r（中心更小），现在改为 1 - dist/r
                float t = Mathf.Clamp01(1f - dist / Mathf.Max(0.0001f, r));

                // 边缘静音带：比如设置 2m，则从半径r到r-2m这段保持0，再开始抬升
                if (edgeFeatherMeters > 0f)
                {
                    float feather01 = Mathf.Clamp01(edgeFeatherMeters / Mathf.Max(0.0001f, r));
                    // t ∈ [0,feather01) 压成 0；其余重新归一化到 (0,1]
                    t = Mathf.InverseLerp(feather01, 1f, t);
                }

                float curve = Mathf.Clamp01(distanceToVolume.Evaluate(t));
                _targetVol  = baseVolume * curve;

                if (!_src.isPlaying)
                {
                    ApplyRouting();
                    _src.volume = 0f;
                    _currentVol = 0f;
                    _src.Play();
                    if (debugLogs) Debug.Log($"[AmbientZone] Play '{clip?.name}'", this);
                }
            }
            else
            {
                _targetVol = 0f;
            }
        }
        else
        {
            _targetVol = 0f;
        }

        // 平滑到目标
        float tau = (_targetVol > _currentVol) ? Mathf.Max(0.0001f, fadeInTime)
                                               : Mathf.Max(0.0001f, fadeOutTime);
        _currentVol = Mathf.SmoothDamp(_currentVol, _targetVol, ref _fadeVel, tau);
        _currentVol = Mathf.Clamp01(_currentVol);
        _src.volume = _currentVol;

        // 全淡出后停止
        if (_src.isPlaying && _currentVol <= 0.0005f && _targetVol <= 0.0005f)
        {
            _src.Stop();
            if (debugLogs) Debug.Log("[AmbientZone] Stop()", this);
        }
    }

    float GetWorldRadius()
    {
        if (!scaleWithTransform) return Mathf.Max(0.01f, radius);
        float maxScale = Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
        return Mathf.Max(0.01f, radius * maxScale);
    }

    void ResolvePlayer(bool initial=false)
    {
        _player = playerOverride ? playerOverride
                 : GameObject.FindGameObjectWithTag(playerTag)?.transform;

        if (debugLogs && initial)
            Debug.Log($"[AmbientZone] Player = {(_player? _player.name : "null")}", this);
    }

    public void SetGameStarted(bool started = true) => _gameStarted = started;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, GetWorldRadius());
        Gizmos.DrawSphere(transform.position, 0.05f);
        // 边缘静音带可视化
        if (edgeFeatherMeters > 0f)
        {
            float r = GetWorldRadius();
            float inner = Mathf.Max(0.01f, r - edgeFeatherMeters);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.08f);
            Gizmos.DrawWireSphere(transform.position, inner);
        }
    }
#endif
}
