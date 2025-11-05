using UnityEngine;

public class InsectDeath : MonoBehaviour
{
   
    [Header("Optional FX / SFX")]
    [Tooltip("(Fallback) If you’re not using a child object, instantiate this explosion prefab")]
    public GameObject explosionFxPrefab;

    [Header("Explosion SFX (supports fade/loop)")]
    public AudioClip explosionSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Tooltip("Play with an AudioSource (can fade/loop). If off, uses PlayClipAtPoint (no fades)")]
    public bool useAudioSource = true;
    [Tooltip("Loop playback (usually unnecessary; for longer ambience-style rips)")]
    public bool sfxLoop = false;
    [Min(0f)] public float sfxFadeIn = 0f;
    [Min(0f)] public float sfxFadeOut = 0.2f;
    [Range(0.1f, 3f)] public float sfxPitch = 1f;

    [Header("3D audio params (only when useAudioSource is true)")]
    [Range(0f, 1f)] public float sfxSpatialBlend = 1f; // 1 = 3D
    public float sfxMinDistance = 1f;
    public float sfxMaxDistance = 20f;
    public AudioRolloffMode sfxRolloff = AudioRolloffMode.Logarithmic;

    [Tooltip("If there’s an ExplosionFX child, reuse its AudioSource first")]
    public bool reuseChildAudioSource = true;
    [Tooltip("Fallback lifetime when we can’t infer from particles (for non-looping fades)")]
    public float sfxDefaultLifetime = 2.0f;

    [Header("Child explosion (recommended)")]
    [Tooltip("Drag the disabled ExplosionFX child under the insect prefab here (it starts disabled by default)")]
    [SerializeField] private GameObject explosionChild;

    [Header("Cookie-on-back (drop the same one)")]
    public bool carryCookie = true;
    public Transform carryPoint;      // Optional: back anchor
    public GameObject cookieOnBack;   // The actual child object in the scene
    public float dropImpulse = 1.2f;
    public float dropTorque = 0.8f;

    [Header("Instant-kill the player on touch")]
    public bool killPlayerOnTouch = true;

    // —— Internal state —— //
    private bool dropped = false;    // Prevent double drop
    private bool exploded = false;    // Prevent double explosion
    private bool isDead = false;      // Prevent multiple death triggers

    

    public GameObject[] gameObjects;

    private void Awake()
    {
        // 注释掉自动查找粒子系统，避免与DissolveSphere的Ember_Particles冲突
        // ResolveExplosionChild();                // Auto-find the disabled particle child
        if (explosionChild && explosionChild.activeSelf)
        {
            Debug.LogWarning($"[InsectDeath] '{explosionChild.name}' is active at start, forced to be disabled to avoid开场播放。");
            explosionChild.SetActive(false);    // Don’t let it play at scene start
        }


    }

    /// <summary> Standard death: explode once at the insect's current position. </summary>
    public void Kill()
    {
        // 防止重复死亡
        if (isDead) return;
        isDead = true;
        
        Debug.Log("[InsectDeath] Kill()");
        if (carryCookie)
        {
            SpwanBugs.Instance.DecreaseMaxPrimaryBugs();
        }
        
        KillAt(transform.position, Vector3.up);
    }

    /// <summary> Death with hit info (but we don't alter FX by that normal/pos anymore). </summary>
    public void KillAt(Vector3 hitPos, Vector3 hitNormal)
    {
        // 隐藏原模型，显示死亡模型
        gameObjects[0].SetActive(false);
        gameObjects[1].SetActive(true);

        // 启动死亡动画
        DissolveSphere[] dissolves = GetComponentsInChildren<DissolveSphere>();
        foreach (DissolveSphere diss in dissolves)
        {
            diss.enabled = true;
            diss.StartDissolve();
        }
        
        //PlayExplosion(hitPos, hitNormal);

        // Drop the same cookie (only once) - 先掉落饼干，再禁用碰撞器
        if (!dropped && carryCookie && cookieOnBack)
        {
            dropped = true;

            if (carryPoint)
            {
                cookieOnBack.transform.position = carryPoint.position;
                cookieOnBack.transform.rotation = carryPoint.rotation;
            }
            // 将饼干从虫子分离出来
            cookieOnBack.transform.SetParent(null);

            var rb = cookieOnBack.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.AddForce(Vector3.up * dropImpulse, ForceMode.Impulse);
                rb.AddTorque(Random.onUnitSphere * dropTorque, ForceMode.Impulse);
            }
            
            // 将掉落饼干移到专门的Layer，避免与粒子系统碰撞
            // 注意：需要在粒子系统的Collision模块中排除"Cookie" Layer
            int cookieLayer = LayerMask.NameToLayer("Cookie");
            if (cookieLayer == -1)
            {
                // 如果"Cookie" Layer不存在，使用"Default" Layer但设置为Trigger
                // 这样粒子系统不会碰撞（因为粒子系统默认不碰撞Trigger）
                Collider[] cookieColliders = cookieOnBack.GetComponentsInChildren<Collider>();
                foreach (Collider col in cookieColliders)
                {
                    col.isTrigger = true;
                }
            }
            else
            {
                // 使用专门的Cookie Layer
                cookieOnBack.layer = cookieLayer;
                // 同时设置所有子物体的Layer
                Transform[] cookieChildren = cookieOnBack.GetComponentsInChildren<Transform>();
                foreach (Transform child in cookieChildren)
                {
                    child.gameObject.layer = cookieLayer;
                }
            }
        }

        // 饼干已经分离，现在禁用虫子身上的所有碰撞器，防止再次被击中
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // 设置刚体为运动学模式，停止物理模拟
        Rigidbody bugRb = GetComponent<Rigidbody>();
        if (bugRb != null)
        {
            bugRb.isKinematic = true;
        }
       
        // 延长销毁时间以完整播放死亡动画（坍塌+消散约3秒）
        Destroy(gameObject, 3.5f);
    }

    private void PlayExplosion(Vector3 pos, Vector3 normal)
    {
        if (exploded) return;
        exploded = true;

        GameObject fxRoot = null;

        // —— Use child FX (most reliable; don’t change its pose) —— //
        if (explosionChild)
        {
            explosionChild.transform.SetParent(null, true);
            explosionChild.SetActive(true);
            fxRoot = explosionChild;

            var systems = explosionChild.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (!ps) continue;
                var main = ps.main;
                main.loop = false;
                ps.Play(true);
            }
        }
        // —— Fallback: instantiate external prefab —— //
        else if (explosionFxPrefab)
        {
            Vector3 spawnPos = transform.position;
            Quaternion spawnRot = explosionFxPrefab.transform.rotation;

            fxRoot = Instantiate(explosionFxPrefab, spawnPos, spawnRot);

            var systems = fxRoot.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (!ps) continue;
                var main = ps.main;
                main.loop = false;
                ps.Play(true);
            }

            if (!fxRoot.TryGetComponent<AutoDestroyParticle>(out _))
                fxRoot.AddComponent<AutoDestroyParticle>();
        }
        else
        {
            Debug.LogWarning("[InsectDeath] 未设置 explosionChild 或 explosionFxPrefab，无法显示爆炸。");
        }

        // —— Audio —— //
        if (explosionSfx)
        {
            if (useAudioSource)
            {
                // Decide host for the AudioSource: prefer the FX root; if none, create an empty
                GameObject host = fxRoot != null ? fxRoot : new GameObject("ExplosionSFX");
                if (host.transform.parent == null && fxRoot == null)
                {
                    host.transform.position = transform.position;
                }

                // Reuse or add a new AudioSource
                AudioSource src = null;
                if (reuseChildAudioSource && fxRoot != null)
                    src = fxRoot.GetComponent<AudioSource>();

                if (!src) src = host.AddComponent<AudioSource>();

                // Configure the source
                src.clip = explosionSfx;
                src.loop = sfxLoop;
                src.pitch = sfxPitch;
                src.spatialBlend = sfxSpatialBlend;
                src.minDistance = sfxMinDistance;
                src.maxDistance = sfxMaxDistance;
                src.rolloffMode = sfxRolloff;
                src.playOnAwake = false;
                src.volume = (sfxFadeIn > 0f) ? 0f : sfxVolume;

                // Play & fade
                src.Play();
                float life = EstimateFxLifetimeSeconds(fxRoot);
                if (!sfxLoop)
                {
                    // Non-looping: fade in -> hold -> fade out -> clean up temp host
                    StartCoroutine(FadeInThenOutAndCleanup(src, sfxVolume, sfxFadeIn, sfxFadeOut, life, fxRoot == null ? host : null));
                }
                else
                {
                    // Looping: fade in only (fade out later when you stop/destroy externally, or add a timer)
                    if (sfxFadeIn > 0f) StartCoroutine(FadeVolume(src, 0f, sfxVolume, sfxFadeIn));
                }
            }
            else
            {
                // Simple one-shot (no fades)
                AudioSource.PlayClipAtPoint(explosionSfx, transform.position, sfxVolume);
            }
        }
    }

    private void OnCollisionEnter(Collision c)
    {
        // 已经死亡，不再杀死玩家
        if (isDead) return;
        if (!killPlayerOnTouch) return;
        if (c.collider.CompareTag("Player"))
        {
            c.collider.GetComponent<PlayerController>()?.Die();
        }
    }

    [ContextMenu("Test Kill")]
    private void _TestKill()
    {
        if (Application.isPlaying)
        {
            Kill();
        }
        else
        {
            Debug.LogWarning("Test Kill 需要在 Play 模式下运行。");
        }
    }

    // —— Key bit: more robust auto-locate of a disabled FX child —— //
    private void ResolveExplosionChild()
    {
        if (explosionChild) return;

        // 1) Prefer a disabled child with AutoDestroyParticle
        var autos = GetComponentsInChildren<AutoDestroyParticle>(true);
        foreach (var a in autos)
        {
            if (!a) continue;
            explosionChild = a.gameObject;
          //  Debug.Log($"[InsectDeath] find AutoDestroyParticle child object: {explosionChild.name}");
            return;
        }

        // 2) Otherwise, look for a disabled child containing ParticleSystem (take the top-most root)
        var pss = GetComponentsInChildren<ParticleSystem>(true);
        if (pss.Length > 0)
        {
            // Use the first particle's top-most ancestor that still has particles as the FX root
            Transform root = pss[0].transform;
            while (root.parent != null && root.parent.GetComponentInChildren<ParticleSystem>(true) != null && root.parent != transform)
                root = root.parent;
            explosionChild = root.gameObject;
          //  Debug.Log($"[InsectDeath] find particle system child object: {explosionChild.name}");
        }
    }

    //Small audio helpers

    // Estimate FX lifetime from particle systems (used to schedule fade-out window)
    float EstimateFxLifetimeSeconds(GameObject fxRoot)
    {
        if (!fxRoot) return Mathf.Max(0.1f, sfxDefaultLifetime);
        float max = 0f;
        var pss = fxRoot.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            var main = ps.main;
            float dur = main.duration;
            float life;
            var lt = main.startLifetime;
            switch (lt.mode)
            {
                case ParticleSystemCurveMode.TwoConstants:
                    life = Mathf.Max(lt.constantMin, lt.constantMax); break;
                case ParticleSystemCurveMode.Constant:
                    life = lt.constant; break;
                default:
                    life = sfxDefaultLifetime * 0.5f; break; // For complex curves, just approximate
            }
            max = Mathf.Max(max, dur + life);
        }
        return (max > 0.05f) ? max : Mathf.Max(0.1f, sfxDefaultLifetime);
    }

    System.Collections.IEnumerator FadeVolume(AudioSource src, float from, float to, float time)
    {
        if (!src) yield break;
        if (time <= 0f) { src.volume = to; yield break; }
        float t = 0f;
        src.volume = from;
        while (t < time && src)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        if (src) src.volume = to;
    }

    // Non-looping: fade in -> hold -> fade out -> clean up temp host
    System.Collections.IEnumerator FadeInThenOutAndCleanup(AudioSource src, float targetVol, float fadeIn, float fadeOut, float life, GameObject tempHostToDestroy)
    {
        if (!src) yield break;

        // Fade in
        if (fadeIn > 0f) yield return FadeVolume(src, 0f, targetVol, fadeIn);
        else src.volume = targetVol;

        // Hold (minus fade-out window)
        float hold = Mathf.Max(0f, life - fadeOut);
        float t = 0f;
        while (t < hold && src)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Fade out
        if (fadeOut > 0f && src) yield return FadeVolume(src, src.volume, 0f, fadeOut);

        // Stop and clean up
        if (src) src.Stop();
        if (tempHostToDestroy) Destroy(tempHostToDestroy);
    }
}
