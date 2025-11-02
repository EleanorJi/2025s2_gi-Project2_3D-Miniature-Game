using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

[RequireComponent(typeof(ParticleSystem))]
public class PlayerPoisonShooter : MonoBehaviour
{
    public ParticleSystem poisonPS;        // Drag the current ParticleSystem here

    [Header("Spray")]
    public float sprayRate = 220f;         // Particles per second (like a water gun)
    public float coneAngle = 3f;           // Spread angle (degrees)

    // ---------------- SFX: looping spray sound ----------------
    [Header("SFX (Spray Loop)")]
    public AudioClip sprayLoopClip;                    // Loops while spraying
    [Range(0f,1f)] public float sprayVolume = 1f;      // Spray volume
    public bool sprayAs2D = true;                      // true = 2D; false = 3D
    public AudioMixerGroup output;                     // Optional: route to your SFX mixer
    [Tooltip("Fade-in time")]
    public float fadeInTime = 0.05f;
    [Tooltip("Fade-out time; set 0 to stop immediately")]
    public float fadeOutTime = 0.0f;

    [Header("3D Settings (when sprayAs2D = false)")]
    [Range(0f,1f)] public float spatialBlend3D = 1f;
    public float minDistance3D = 1f;
    public float maxDistance3D = 15f;

    private ParticleSystem.EmissionModule _emission;
    private ParticleSystem.ShapeModule _shape;
    private bool _spraying;
    private readonly List<ParticleCollisionEvent> _events = new();

    // ---- Internals: fade-in/out state for audio ----
    AudioSource _sfx;
    float _volCurrent = 0f, _volTarget = 0f, _volVel = 0f;

    void Awake()
    {
        if (!poisonPS) poisonPS = GetComponent<ParticleSystem>();
        _emission = poisonPS.emission;
        _shape = poisonPS.shape;

        _emission.rateOverTime = 0f;       // Script controls emission
        _shape.angle = coneAngle;          // Can tweak at runtime

        // Dedicated audio source
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
        // 确保粒子系统已初始化
        if (!poisonPS) return;
        
        // Start spraying (Left Mouse Button)
        if (Input.GetMouseButtonDown(0)) StartSpray();
        // Stop on release (Left Mouse Button)
        if (Input.GetMouseButtonUp(0)) StopSpray();

        // Allow tweaking the cone at runtime
        if (_shape.angle != coneAngle) _shape.angle = coneAngle;

        TickSfxVolume(); // Handle fade-in/out each frame
    }

    void StartSpray()
    {
        _spraying = true;

        // Particles
        _emission.rateOverTime = sprayRate;
        if (!poisonPS.isPlaying) poisonPS.Play();

        // Audio (fade in and ensure it's playing)
        if (sprayLoopClip)
        {
            _sfx.clip = sprayLoopClip;
            ApplySpatial();
            if (!_sfx.isPlaying) _sfx.Play();
            _volTarget = sprayVolume; // Fade up to target volume
        }
    }

    void StopSpray()
    {
        _spraying = false;

        // Particles
        _emission.rateOverTime = 0f;
        poisonPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Audio (fade out or stop immediately)
        if (sprayLoopClip)
        {
            if (fadeOutTime <= 0f)
            {
                _volTarget = 0f;
                _volCurrent = 0f;
                if (_sfx.isPlaying) _sfx.Stop(); // Hard stop now
            }
            else
            {
                _volTarget = 0f; // Let it fade out
            }
        }
    }

    void TickSfxVolume()
    {
        if (!sprayLoopClip) return;

        // Pick the time constant for fade-in vs fade-out
        float tau = (_volTarget > _volCurrent)
                    ? Mathf.Max(0.0001f, fadeInTime)
                    : Mathf.Max(0.0001f, fadeOutTime);

        _volCurrent = Mathf.SmoothDamp(_volCurrent, _volTarget, ref _volVel, tau);
        _volCurrent = Mathf.Clamp01(_volCurrent);
        _sfx.volume = _volCurrent;

        // After fully faded out, stop the source
        if (!_spraying && _sfx.isPlaying && _volCurrent <= 0.0005f)
            _sfx.Stop();
    }

    // Hit callback: keep your previous kill check
    void OnParticleCollision(GameObject other)
    {
        int count = ParticlePhysicsExtensions.GetCollisionEvents(poisonPS, other, _events);
        var death = other.GetComponent<InsectDeath>() ?? other.GetComponentInParent<InsectDeath>();
        if (death != null) death.Kill();
        _events.Clear();
    }
}
