using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Shoot")]
    public Transform firePoint;               // The firing point on the player's body
    public GameObject projectilePrefab;       // Projectile prefab (fireball/poison)
    public float fireCooldown = 0.3f;
    public int playerDamage = 5;              // ★ Player bullet damage (don't set to 0 in Inspector)
    private float lastShotTime;

    [Header("Summon")]
    public GameObject minionPrefab;           // Minion prefab (with MinionAnchor + MinionShooter)
    public int minionCount = 3;
    public float summonSpread = 0.6f;
    public float minionLifetime = 20f;
    public float minionFireInterval = 0.7f;
    public int minionDamage = 1;              // ★ Minion bullet damage = 1

    [Header("Boss")]
    public Transform boss;                    // Can be left empty, will auto-find by Tag at runtime
    public string bossTag = "Boss";

    void Update()
    {
        if (Input.GetMouseButton(0)) TryShoot();

        if (Input.GetKeyDown(KeyCode.K)) SummonMinions();
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
            proj.damage   = playerDamage; // ★ Player damage
            proj.targetTag = "";          // ← Empty means it can hit anything (including Boss)
            proj.logHits  = true;         // ← Force enable log to ensure you can see hit prints
        }
    }


    void SummonMinions()
    {
        EnsureBoss();
        if (!minionPrefab) return;

        for (int i = 0; i < minionCount; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * summonSpread;
            Vector3 spawnPos = transform.position + new Vector3(rnd.x, 0f, rnd.y);
            var m = Instantiate(minionPrefab, spawnPos, Quaternion.identity);

            // 固定在玩家周围
            var anchor = m.GetComponent<MinionAnchor>();
            if (anchor)
            {
                anchor.follow = transform;
                anchor.localOffset = new Vector3(rnd.x, 0f, rnd.y);
            }

            // 自动射击（小兵伤害 = 1）
            var shooter = m.GetComponent<MinionShooter>();
            if (shooter)
            {
                shooter.projectilePrefab = projectilePrefab;
                shooter.firePoint = FindFirePointIn(m.transform);
                shooter.fireEvery = minionFireInterval;
                shooter.minionDamage = minionDamage;      // ← 1
                shooter.targetTag = string.IsNullOrEmpty(bossTag) ? "Boss" : bossTag;
                shooter.lifeTime = minionLifetime;
            }
        }
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
}
