using UnityEngine;

public class MinionShooter : MonoBehaviour
{
    [Header("Target")]
    public Transform target;            // 优先使用这个
    public string targetTag = "Boss";   // 为空时按 Tag 查找

    [Header("Shoot")]
    public Transform firePoint;         // 小兵的开火点（Prefab 里放一个名为 firePoint 的子物体）
    public GameObject projectilePrefab; // 与玩家同一颗子弹
    public float fireEvery = 0.7f;      // 开火间隔
    public int minionDamage = 1;        // 小兵造成的伤害
    public float rotateSpeed = 10f;     // 朝向转身速度

    [Header("Life")]
    public float lifeTime = 20f;        // N 秒后自动销毁

    private float lastFireTime;

    void Start()
    {
        // 自动找 firePoint（名称里含 fire 的 Transform）
        if (firePoint == null)
        {
            var t = transform.Find("firePoint");
            if (t != null) firePoint = t;
            else
            {
                foreach (var tf in GetComponentsInChildren<Transform>())
                {
                    if (tf.name.ToLower().Contains("fire"))
                    {
                        firePoint = tf;
                        break;
                    }
                }
            }
        }
        if (firePoint == null) firePoint = transform;

        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;
        }

        if (lifeTime > 0) Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 持续尝试获取目标
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;
        }

        // 朝向目标：小兵本体水平转，firePoint 对准三维方向
        if (target != null)
        {
            Vector3 flat = target.position - transform.position; flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                var desired = Quaternion.LookRotation(flat, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotateSpeed * Time.deltaTime);
            }

            Vector3 aim = target.position - firePoint.position;
            if (aim.sqrMagnitude > 0.0001f)
                firePoint.rotation = Quaternion.LookRotation(aim.normalized, Vector3.up);
        }

        // 自动开火
        if (Time.time - lastFireTime >= fireEvery && projectilePrefab != null)
        {
            lastFireTime = Time.time;

            var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            var proj = go.GetComponent<PoisonProjectile>();
            if (proj != null)
            {
                proj.damage = minionDamage;   // 小兵伤害 = 1
                proj.targetTag = targetTag;
            }
        }
    }
}
