using UnityEngine;

public class InsectDeath : MonoBehaviour
{
    [Header("可选 FX / SFX")]
    [Tooltip("（兜底）不使用子物体时，实例化这个爆炸预制体")]
    public GameObject explosionFxPrefab;

    [Header("爆炸音效（可淡入/淡出/循环）")]
    public AudioClip explosionSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Tooltip("用 AudioSource 播放（可渐变/循环）；关闭则用 PlayClipAtPoint（无渐变）")]
    public bool useAudioSource = true;
    [Tooltip("循环播放（通常不需要；用于较长炸裂环境音时）")]
    public bool sfxLoop = false;
    [Min(0f)] public float sfxFadeIn = 0f;
    [Min(0f)] public float sfxFadeOut = 0.2f;
    [Range(0.1f, 3f)] public float sfxPitch = 1f;

    [Header("3D 音频参数（仅 useAudioSource 生效）")]
    [Range(0f, 1f)] public float sfxSpatialBlend = 1f; // 1 = 3D
    public float sfxMinDistance = 1f;
    public float sfxMaxDistance = 20f;
    public AudioRolloffMode sfxRolloff = AudioRolloffMode.Logarithmic;

    [Tooltip("若有 ExplosionFX 子物体，优先复用其 AudioSource")]
    public bool reuseChildAudioSource = true;
    [Tooltip("若无法从粒子推断时，音效生命期兜底时长（用于循环=否时决定淡出开窗）")]
    public float sfxDefaultLifetime = 2.0f;

    [Header("子物体爆炸（推荐）")]
    [Tooltip("把虫子预制体下的 ExplosionFX 子物体（默认禁用）拖到这里")]
    [SerializeField] private GameObject explosionChild;

    [Header("背饼干（同一颗从背上掉）")]
    public bool carryCookie = true;
    public Transform carryPoint;      // 可选：背部定位点
    public GameObject cookieOnBack;   // 场景里的这颗子物体
    public float dropImpulse = 1.2f;
    public float dropTorque  = 0.8f;

    [Header("撞到玩家是否秒杀")]
    public bool killPlayerOnTouch = true;

    // —— 内部状态 —— //
    private bool dropped  = false;    // 防二次掉饼干
    private bool exploded = false;    // 防二次爆炸

    private void Awake()
    {
        ResolveExplosionChild();                // 自动寻找禁用的粒子子物体
        if (explosionChild && explosionChild.activeSelf)
        {
            Debug.LogWarning($"[InsectDeath] '{explosionChild.name}' 初始为激活，已强制关闭以避免开场播放。");
            explosionChild.SetActive(false);    // 防止开场播
        }
    }

    /// <summary> 标准死亡：在虫子当前位置爆炸一次。 </summary>
    public void Kill()
    {
        Debug.Log("[InsectDeath] Kill()");
        KillAt(transform.position, Vector3.up);
    }

    /// <summary> 携带命中信息的死亡（但不再用朝向/位置去改特效）。 </summary>
    public void KillAt(Vector3 hitPos, Vector3 hitNormal)
    {
        PlayExplosion(hitPos, hitNormal);

        // 掉同一颗饼干（只做一次）
        if (!dropped && carryCookie && cookieOnBack)
        {
            dropped = true;

            if (carryPoint)
            {
                cookieOnBack.transform.position = carryPoint.position;
                cookieOnBack.transform.rotation = carryPoint.rotation;
            }
            cookieOnBack.transform.SetParent(null);

            var rb = cookieOnBack.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity  = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.AddForce(Vector3.up * dropImpulse, ForceMode.Impulse);
                rb.AddTorque(Random.onUnitSphere * dropTorque, ForceMode.Impulse);
            }
        }

        Destroy(gameObject); // 最后销毁虫子本体
    }

    private void PlayExplosion(Vector3 pos, Vector3 normal)
    {
        if (exploded) return;
        exploded = true;

        GameObject fxRoot = null;

        // —— 子物体爆炸（最稳，不改位置/朝向） —— //
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
        // —— 兜底：实例化外部预制体 —— //
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

        // —— 音效 —— //
        if (explosionSfx)
        {
            if (useAudioSource)
            {
                // 选择挂载音源的宿主：优先 FX 根；若没有就挂到场景空物体
                GameObject host = fxRoot != null ? fxRoot : new GameObject("ExplosionSFX");
                if (host.transform.parent == null && fxRoot == null)
                {
                    host.transform.position = transform.position;
                }

                // 复用或新建 AudioSource
                AudioSource src = null;
                if (reuseChildAudioSource && fxRoot != null)
                    src = fxRoot.GetComponent<AudioSource>();

                if (!src) src = host.AddComponent<AudioSource>();

                // 配置音源
                src.clip = explosionSfx;
                src.loop = sfxLoop;
                src.pitch = sfxPitch;
                src.spatialBlend = sfxSpatialBlend;
                src.minDistance = sfxMinDistance;
                src.maxDistance = sfxMaxDistance;
                src.rolloffMode = sfxRolloff;
                src.playOnAwake = false;
                src.volume = (sfxFadeIn > 0f) ? 0f : sfxVolume;

                // 播放 & 渐变
                src.Play();
                float life = EstimateFxLifetimeSeconds(fxRoot);
                if (!sfxLoop)
                {
                    // 非循环：淡入 -> 等待 -> 淡出 -> 回收
                    StartCoroutine(FadeInThenOutAndCleanup(src, sfxVolume, sfxFadeIn, sfxFadeOut, life, fxRoot == null ? host : null));
                }
                else
                {
                    // 循环：仅淡入（淡出需你外部再停/销毁，或改用定时器）
                    if (sfxFadeIn > 0f) StartCoroutine(FadeVolume(src, 0f, sfxVolume, sfxFadeIn));
                }
            }
            else
            {
                // 简单一次性（无淡入淡出）
                AudioSource.PlayClipAtPoint(explosionSfx, transform.position, sfxVolume);
            }
        }
    }

    private void OnCollisionEnter(Collision c)
    {
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

    // —— 关键：更稳的“自动定位禁用子物体” —— //
    private void ResolveExplosionChild()
    {
        if (explosionChild) return;

        // 1) 优先找挂了 AutoDestroyParticle 的禁用子物体
        var autos = GetComponentsInChildren<AutoDestroyParticle>(true);
        foreach (var a in autos)
        {
            if (!a) continue;
            explosionChild = a.gameObject;
            Debug.Log($"[InsectDeath] 自动找到 AutoDestroyParticle 子物体：{explosionChild.name}");
            return;
        }

        // 2) 其次找包含 ParticleSystem 的禁用子物体（取最上层节点）
        var pss = GetComponentsInChildren<ParticleSystem>(true);
        if (pss.Length > 0)
        {
            // 把第一个粒子的“最上层有粒子的根”作为特效根
            Transform root = pss[0].transform;
            while (root.parent != null && root.parent.GetComponentInChildren<ParticleSystem>(true) != null && root.parent != transform)
                root = root.parent;
            explosionChild = root.gameObject;
            Debug.Log($"[InsectDeath] 自动找到含粒子系统的子物体：{explosionChild.name}");
        }
    }

    // =================== 音频小工具 ===================

    // 根据粒子系统估算 FX 时长（用于自动安排淡出窗口）
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
                    life = sfxDefaultLifetime * 0.5f; break; // 复杂曲线就给个近似
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

    // 非循环：淡入 -> 保持 -> 淡出 -> 清理临时宿主
    System.Collections.IEnumerator FadeInThenOutAndCleanup(AudioSource src, float targetVol, float fadeIn, float fadeOut, float life, GameObject tempHostToDestroy)
    {
        if (!src) yield break;

        // 淡入
        if (fadeIn > 0f) yield return FadeVolume(src, 0f, targetVol, fadeIn);
        else src.volume = targetVol;

        // 等待（减去淡出窗口）
        float hold = Mathf.Max(0f, life - fadeOut);
        float t = 0f;
        while (t < hold && src)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 淡出
        if (fadeOut > 0f && src) yield return FadeVolume(src, src.volume, 0f, fadeOut);

        // 停止并清理
        if (src) src.Stop();
        if (tempHostToDestroy) Destroy(tempHostToDestroy);
    }
}
