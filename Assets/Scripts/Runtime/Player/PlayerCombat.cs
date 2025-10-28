using UnityEngine;

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
    }

    void TrySummonOneMinion()
    {
        // 1) Cookie 检查：需 1 个 cookie
        if (CookiesInventory.Instance == null || !CookiesInventory.Instance.Spend(1))
        {
            // ✨ Cookie 不足：弹出中央UI提示（自动1秒淡出）
            NoCookieUI.ShowCenter("You need at least 1 cookie to summon a minion.");
            Debug.Log("[Summon] Not enough cookies (need 1).");
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
}
