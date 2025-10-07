using UnityEngine;

public class InsectDeath : MonoBehaviour
{
    [Header("可选 FX / SFX")]
    [Tooltip("（兜底）不使用子物体时，实例化这个爆炸预制体")]
    public GameObject explosionFxPrefab;
    public AudioClip explosionSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

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

        // —— 子物体爆炸（最稳，不改位置/朝向） —— //
        if (explosionChild)
        {
            // 保留世界变换
            explosionChild.transform.SetParent(null, true);

            // 激活并统一播放；强制关闭循环
            explosionChild.SetActive(true);
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

            var fx = Instantiate(explosionFxPrefab, spawnPos, spawnRot);

            var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (!ps) continue;
                var main = ps.main;
                main.loop = false;
                ps.Play(true);
            }

            if (!fx.TryGetComponent<AutoDestroyParticle>(out _))
                fx.AddComponent<AutoDestroyParticle>();
        }
        else
        {
            Debug.LogWarning("[InsectDeath] 未设置 explosionChild 或 explosionFxPrefab，无法显示爆炸。");
        }

        if (explosionSfx)
            AudioSource.PlayClipAtPoint(explosionSfx, transform.position, sfxVolume);
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
}
