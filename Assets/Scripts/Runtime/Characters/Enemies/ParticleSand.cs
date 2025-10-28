// Assets/_Project/Scripts/Runtime/Characters/Enemies/ParticleSand.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deal damage to the player when individual sand particles get close enough.
/// Works with a ParticleSystem set to any Simulation Space (Local/World/Custom).
/// Per-particle and global hit cooldowns are supported to avoid instant HP drain.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ParticleSand : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private Transform player;          // optional; will auto-find by tag if null

    [Header("Damage")]
    [SerializeField] private int damage = 20;           // damage per hit
    [SerializeField] private float hitRadius = 0.6f;    // particle proximity radius to count as a hit
    [Tooltip("Minimum time between ANY two hits (seconds).")]
    [SerializeField] private float globalHitCooldown = 0.25f;
    [Tooltip("Minimum time before the SAME particle can hit again (seconds).")]
    [SerializeField] private float perParticleCooldown = 0.75f;

    [Header("Safety / UX")]
    [Tooltip("Ignore collisions for the first seconds after this emitter becomes active.")]
    [SerializeField] private float spawnGraceSeconds = 0.15f;
    [Tooltip("Clamp the maximum number of particles we query per Update for performance.")]
    [SerializeField] private int particleBufferCap = 512;

    private ParticleSystem _ps;
    private PlayerHealth _playerHealth;
    private bool _warnedNoHealth;
    private float _startedAt;
    private float _lastGlobalHitAt = -999f;

    // Track last-hit time per particle via its randomSeed
    private readonly Dictionary<uint, float> _lastHitTimeBySeed = new Dictionary<uint, float>();

    // Reused buffer to avoid allocs
    private ParticleSystem.Particle[] _buffer;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        _buffer = new ParticleSystem.Particle[Mathf.Max(128, particleBufferCap)];
        _startedAt = Time.time;

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag(targetTag);
            if (pObj) player = pObj.transform;
        }
    }

    private void EnsurePlayerHealth()
    {
        if (_playerHealth != null || player == null) return;

        _playerHealth = player.GetComponent<PlayerHealth>();
        if (_playerHealth == null && !_warnedNoHealth)
        {
            _warnedNoHealth = true;
            Debug.LogWarning("[ParticleSand] PlayerHealth component not found on player.");
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Grace period just after emitter starts
        if (Time.time - _startedAt < spawnGraceSeconds) return;

        EnsurePlayerHealth();
        if (_playerHealth == null) return;

        int needed = Mathf.Min(_ps.particleCount, particleBufferCap);
        if (needed <= 0) return;

        // Fill buffer with live particles
        int count = _ps.GetParticles(_buffer, needed);
        if (count <= 0) return;

        Vector3 playerPos = player.position;
        float sqrRadius = hitRadius * hitRadius;

        Transform t = _ps.transform; // used to convert local->world when needed
        var main = _ps.main;
        bool isWorldSpace = main.simulationSpace == ParticleSystemSimulationSpace.World;

        for (int i = 0; i < count; i++)
        {
            ref ParticleSystem.Particle p = ref _buffer[i];

            // Compute this particle's world position regardless of simulation space
            Vector3 worldPos = isWorldSpace ? p.position : t.TransformPoint(p.position);

            // Quick distance check
            if ((worldPos - playerPos).sqrMagnitude > sqrRadius) continue;

            // Per-particle cooldown by randomSeed
            uint seed = p.randomSeed;
            if (_lastHitTimeBySeed.TryGetValue(seed, out float lastHitTime))
            {
                if (Time.time - lastHitTime < perParticleCooldown) continue;
            }

            // Global cooldown
            if (Time.time - _lastGlobalHitAt < globalHitCooldown) continue;

            // Apply damage (use your PlayerHealth signature: source + amount)
            _playerHealth.TakeDamage("Sand", damage);

            // Mark cooldowns
            _lastHitTimeBySeed[seed] = Time.time;
            _lastGlobalHitAt = Time.time;

            // Optional: if you want each particle to only ever hit once, uncomment:
            // p.remainingLifetime = 0f;
            // _buffer[i] = p; // only needed if you change particle data
        }

        // Only needed if you changed particle data (e.g., killed particles)
        // _ps.SetParticles(_buffer, count);
    }

    // In case you restart the effect at runtime
    private void OnEnable()
    {
        _startedAt = Time.time;
        _lastHitTimeBySeed.Clear();
        _lastGlobalHitAt = -999f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = new Color(1f, 0.65f, 0f, 0.25f);
        Gizmos.DrawWireSphere(player.position, hitRadius);
    }
#endif
}
