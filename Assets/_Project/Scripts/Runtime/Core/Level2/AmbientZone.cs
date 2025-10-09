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

    [Header("Player Location")]
    public Transform playerOverride;
    public string playerTag = "Player";
    public float reFindPlayerInterval = 1.0f;

    [Header("Range")]
    [Tooltip("radius (world units)")]
    public float radius = 10f;
    [Tooltip("whether to apply scaling: radius × max(lossyScale)")]
    public bool scaleWithTransform = false;

    [Header("Fade In/Out")]
    public float fadeInTime  = 0.35f;
    public float fadeOutTime = 0.35f;

    [Header("Distance→Volume (t=0=edge, t=1=center)")]
    //default curve changed to 0->0, 1->1 (edge 0 volume, center 1 volume)
    public AnimationCurve distanceToVolume = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(1f, 1f)
    );

    [Tooltip("edge silent zone (meters): from outer radius inward this many meters volume stays 0, then starts rising")]
    public float edgeFeatherMeters = 0f;

    [Header("2D/3D Sound Field")]
    public bool use3D = false;
    [Range(0f,1f)] public float spatialBlend3D = 1f;
    public float minDistance3D = 1f;
    public float maxDistance3D = 30f;

    [Header("Startup Control")]
    public bool onlyAfterGameStarted = true;
    public bool autoGameStartOnStart = false;

    [Header("Debug")]
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
        _src.spatialBlend = 0f; //default 2D
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
            _src.spatialBlend = 0f; //2D: only script controls volume
        }
    }

    void Update()
    {
        // find player (can recover when reference is lost)
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
                //key: reverse mapping, edge=0, center=1 —— //
                // originally dist/r (center smaller), now changed to 1 - dist/r
                float t = Mathf.Clamp01(1f - dist / Mathf.Max(0.0001f, r));

                // edge silent zone: for example set 2m, then from radius r to r-2m this section stays 0, then starts rising
                if (edgeFeatherMeters > 0f)
                {
                    float feather01 = Mathf.Clamp01(edgeFeatherMeters / Mathf.Max(0.0001f, r));
                    // t [0,feather01) compressed to 0; rest renormalized to (0,1]
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

        // smooth to target
        float tau = (_targetVol > _currentVol) ? Mathf.Max(0.0001f, fadeInTime)
                                               : Mathf.Max(0.0001f, fadeOutTime);
        _currentVol = Mathf.SmoothDamp(_currentVol, _targetVol, ref _fadeVel, tau);
        _currentVol = Mathf.Clamp01(_currentVol);
        _src.volume = _currentVol;

        // stop after completely faded out
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
        // edge silent zone visualization
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
