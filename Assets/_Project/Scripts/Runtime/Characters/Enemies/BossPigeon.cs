using System.Collections;
using UnityEngine;

/// <summary>
/// Simple boss driver: periodically face the player, windup, then fire a sand burst
/// (your ParticleSystem under fireOrigin). No projectile prefabs needed.
/// </summary>
public class BossPigeon : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player root (will auto-find by tag 'Player' if left empty).")]
    public Transform player;

    [Tooltip("Where the wind originates from (an empty under the pigeon).")]
    public Transform fireOrigin;

    [Tooltip("The ParticleSystem that visually bursts sand (attach the one you already built).")]
    public ParticleSystem sandBurst;

    [Tooltip("Optional Animator on the pigeon (for flap/windup).")]
    public Animator animator;

    [Header("Timing")]
    [Tooltip("Delay before the first attack.")]
    public float initialDelay = 1.0f;

    [Tooltip("Min seconds between attacks.")]
    public float attackIntervalMin = 3.0f;

    [Tooltip("Max seconds between attacks.")]
    public float attackIntervalMax = 5.0f;

    [Tooltip("Windup duration before a burst (for telegraph/animation).")]
    public float windupTime = 0.6f;

    [Header("Aiming")]
    [Tooltip("Rotate to face player on the horizontal plane before each attack.")]
    public bool facePlayer = true;

    [Tooltip("Additional yaw offset (degrees).")]
    public float yawOffset = 0f;

    [Header("Particle Firing Mode")]
    [Tooltip("If true, call Emit(emitCount) on the ParticleSystem; if false, just Play() (use Emission.Bursts).")]
    public bool useEmitCount = false;

    [Tooltip("How many particles to emit if useEmitCount is true.")]
    public int emitCount = 88;

    [Header("SFX (optional)")]
    public AudioSource sfxSource;
    public AudioClip windupClip;
    public AudioClip burstClip;

    private Coroutine loop;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (!sandBurst)
        {
            Debug.LogWarning("[BossPigeon] SandBurst is not assigned. Assign your sand ParticleSystem on the pigeon.");
        }
        if (!fireOrigin && sandBurst)
        {
            // fall back: use particle's own transform as origin
            fireOrigin = sandBurst.transform;
        }
    }

    void OnEnable()
    {
        loop = StartCoroutine(AttackLoop());
    }

    void OnDisable()
    {
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    IEnumerator AttackLoop()
    {
        // initial delay
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

        while (enabled)
        {
            // pick next interval now (so windup不占用间隔外的随机性)
            float interval = Random.Range(attackIntervalMin, attackIntervalMax);

            // aim
            if (player && facePlayer)
            {
                Vector3 to = player.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up) * Quaternion.Euler(0f, yawOffset, 0f);
                    transform.rotation = look;
                }
            }

            // windup
            if (windupTime > 0f)
            {
                if (sfxSource && windupClip) sfxSource.PlayOneShot(windupClip);
                if (animator) animator.SetTrigger("Windup"); // 可在 Animator 里配一个 Windup 触发
                yield return new WaitForSeconds(windupTime);
            }

            // fire burst
            FireOnce();

            // cooldown till next tick
            if (interval > 0f) yield return new WaitForSeconds(interval);
        }
    }

    private void FireOnce()
    {
        if (!sandBurst) return;

        // ensure origin & rotation
        if (fireOrigin)
        {
            sandBurst.transform.SetPositionAndRotation(fireOrigin.position, fireOrigin.rotation);
        }

        // 方式 A：完全用 Emission.Bursts（推荐你现在这种配置）
        // - 粒子系统自身设置好一个 Burst（或多段 Burst），脚本只需要 Play()
        if (!useEmitCount)
        {
            // Stop → Clear → Play 的顺序更可控，避免上一次遗留状态
            sandBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sandBurst.Play();
        }
        else
        {
            // 方式 B：脚本直接 Emit 固定数量
            sandBurst.Emit(Mathf.Max(1, emitCount));
        }

        // SFX/动画
        if (sfxSource && burstClip) sfxSource.PlayOneShot(burstClip);
        if (animator) animator.SetTrigger("Flap"); // 发射时的翅膀拍打
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (fireOrigin)
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(fireOrigin.position, 0.25f);
            if (player)
            {
                Gizmos.DrawLine(fireOrigin.position, player.position);
            }
        }
    }
#endif
}
