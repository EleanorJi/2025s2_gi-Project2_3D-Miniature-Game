using UnityEngine;
using UnityEngine.Audio;

public class PlayerCombat : MonoBehaviour
{
    [Header("Shoot")]
    public Transform firePoint;               // 开火点
    public GameObject projectilePrefab;       // 子弹预制体
    public float fireCooldown = 0.3f;
    public int playerDamage = 5;              // 玩家子弹伤害
    private float lastShotTime;

    [Header("Summon (1 cookie per minion)")]
    public GameObject minionPrefab;           // 小兵预制体（包含 MinionAnchor + MinionShooter）
    public float summonSpread = 0.6f;         // 生成在玩家周围的半径
    public float minionLifetime = 20f;        // 小兵寿命
    public float minionFireInterval = 0.7f;   // 小兵射击间隔
    public int minionDamage = 1;              // 小兵子弹伤害

    [Header("Boss")]
    public Transform boss;                    // Boss（可留空，运行时按 Tag 寻找）
    public string bossTag = "Boss";

    #region Audio
    [Header("Audio (drop your clips here)")]
    [Tooltip("射击音效。可放多条，随机播放其一。")]
    public AudioClip[] shootClips;
    [Range(0f, 1f)] public float shootVolume = 0.8f;

    [Tooltip("召唤成功音效。可放多条，随机播放其一。")]
    public AudioClip[] summonClips;
    [Range(0f, 1f)] public float summonVolume = 0.9f;

    [Tooltip("Cookie 不足时提示音效（可选）。")]
    public AudioClip[] errorClips;
    [Range(0f, 1f)] public float errorVolume = 0.8f;

    [Tooltip("将所有音效输出到指定的 Mixer Group（可选）。")]
    public AudioMixerGroup outputMixerGroup;

    [Header("Audio Tweaks")]
    [Tooltip("是否对每次播放做轻微音高随机，避免重复听感。")]
    public bool enablePitchJitter = true;
    [Tooltip("音高随机范围，例如 0.95~1.05。")]
    public Vector2 pitchRange = new Vector2(0.97f, 1.03f);

    private AudioSource sfxSource;
    #endregion

    void Awake()
    {
        // 安全创建/复用一个 AudioSource，用来 PlayOneShot
        sfxSource = GetComponent<AudioSource>();
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // UI/玩家开火一般用 2D 声音；想做3D可调成1并合理设置Rolloff
        if (outputMixerGroup) sfxSource.outputAudioMixerGroup = outputMixerGroup;
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) TryShoot();

        // 按 K：每次召唤 1 个，需消耗 1 个 Cookie
        if (Input.GetKeyDown(KeyCode.K)) TrySummonOneMinion();
    }

    void TryShoot()
    {
        if (!projectilePrefab || !firePoint) return;
        if (Time.time - lastShotTime < fireCooldown) return;

        lastShotTime = Time.time;

        var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        var proj = go.GetComponent<PoisonProjectile>();
        if (proj != null)
        {
            proj.damage    = playerDamage; // 玩家伤害
            proj.targetTag = "";           // 空串：可命中任意（含 Boss）
            proj.logHits   = true;         // 打印命中日志便于调试
        }

        // 播放射击音效
        PlaySFX(shootClips, shootVolume);
    }

    void TrySummonOneMinion()
    {
        // 1) Cookie 检查：需 1 个 cookie
        if (CookiesInventory.Instance == null || !CookiesInventory.Instance.Spend(1))
        {
            // ✨ Cookie 不足：弹出中央UI提示（自动1秒淡出）
            NoCookieUI.ShowCenter("You need at least 1 cookie to summon a minion.");
            Debug.Log("[Summon] Not enough cookies (need 1).");

            // 播放错误提示音（可选）
            PlaySFX(errorClips, errorVolume);
            return;
        }

        // 2) 找 Boss 引用
        EnsureBoss();
        if (!minionPrefab) return;

        // 3) 在玩家周围位置随机生成（不重叠）
        Vector2 rnd = Random.insideUnitCircle * summonSpread;
        Vector3 spawnPos = transform.position + new Vector3(rnd.x, 0f, rnd.y);

        var m = Instantiate(minionPrefab, spawnPos, Quaternion.identity);

        // 4) 绑定 Anchor，使其跟随玩家并贴地
        var anchor = m.GetComponent<MinionAnchor>();
        if (anchor)
        {
            anchor.follow = transform;
            anchor.localOffset = new Vector3(rnd.x, 0f, rnd.y);
        }

        // 5) 配置射击逻辑
        var shooter = m.GetComponent<MinionShooter>();
        if (shooter)
        {
            shooter.projectilePrefab = projectilePrefab;
            shooter.firePoint = FindFirePointIn(m.transform);
            shooter.fireEvery = minionFireInterval;
            shooter.minionDamage = minionDamage;
            shooter.targetTag = string.IsNullOrEmpty(bossTag) ? "Boss" : bossTag;
            shooter.lifeTime = minionLifetime;
        }

        // 6) 禁用小兵与玩家、与其他小兵的碰撞
        DisableCollisions(m);

        // 播放召唤音效
        PlaySFX(summonClips, summonVolume);
    }

    Transform FindFirePointIn(Transform root)
    {
        var t = root.Find("firePoint");
        if (t) return t;
        foreach (Transform c in root.GetComponentsInChildren<Transform>(true))
            if (c.name.ToLower().Contains("fire")) return c;
        return root;
    }

    void EnsureBoss()
    {
        if (boss) return;
        var go = GameObject.FindGameObjectWithTag(string.IsNullOrEmpty(bossTag) ? "Boss" : bossTag);
        if (go) boss = go.transform;
    }

    /// <summary>
    /// 让新召唤的小兵不与玩家和既有小兵发生物理碰撞。
    /// </summary>
    void DisableCollisions(GameObject newMinion)
    {
        var newCols = newMinion.GetComponentsInChildren<Collider>(includeInactive: true);
        var playerCol = GetComponent<Collider>();

        // 不与玩家碰撞
        if (playerCol)
        {
            foreach (var c in newCols)
                if (c) Physics.IgnoreCollision(c, playerCol, true);
        }

        // 不与已存在的小兵碰撞
        var allMinions = FindObjectsOfType<MinionAnchor>();
        foreach (var other in allMinions)
        {
            if (!other || other.gameObject == newMinion) continue;

            var otherCols = other.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (var c1 in newCols)
            {
                foreach (var c2 in otherCols)
                {
                    if (c1 && c2)
                        Physics.IgnoreCollision(c1, c2, true);
                }
            }
        }
    }

    // === 音效播放工具 ===
    void PlaySFX(AudioClip[] clips, float volume)
    {
        if (sfxSource == null || clips == null || clips.Length == 0) return;

        int idx = (clips.Length == 1) ? 0 : Random.Range(0, clips.Length);
        var clip = clips[idx];
        if (!clip) return;

        float originalPitch = sfxSource.pitch;

        if (enablePitchJitter && pitchRange.x > 0f && pitchRange.y > 0f)
        {
            float minP = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxP = Mathf.Max(pitchRange.x, pitchRange.y);
            sfxSource.pitch = Random.Range(minP, maxP);
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));

        // 恢复音高，避免影响后续播放
        if (enablePitchJitter) sfxSource.pitch = originalPitch;
    }
}
