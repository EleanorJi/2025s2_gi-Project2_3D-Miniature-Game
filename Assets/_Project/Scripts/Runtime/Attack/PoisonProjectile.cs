using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Collider))]
public class PoisonProjectile : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 8f;
    public float lifeTime = 3f;

    [Header("Damage")]
    public int damage = 1;
    public string targetTag = "Boss"; // 可留空；留着兼容 PlayerCombat/MinionShooter 的赋值

    [Header("Debug")]
    public bool logHits = false;

    Rigidbody rb;
    Collider selfCol;
    float sphereRadius = 0.1f;
    Vector3 lastPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        selfCol = GetComponent<Collider>();

        // 触发器 + 运动由我们驱动
        if (selfCol) selfCol.isTrigger = true;
        if (rb) { rb.isKinematic = true; rb.useGravity = false; }

        // 取球半径（如果用的是 SphereCollider）
        var sc = GetComponent<SphereCollider>();
        if (sc != null) sphereRadius = Mathf.Max(0.01f, sc.radius * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z)));
    }

    void OnEnable()
    {
        lastPos = transform.position;
        if (lifeTime > 0) Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        // 先计算下一帧位置
        Vector3 nextPos = transform.position + transform.forward * speed * Time.fixedDeltaTime;

        // 在 lastPos → nextPos 之间做扫掠，包含 Trigger
        Vector3 dir = nextPos - lastPos;
        float dist = dir.magnitude;
        if (dist > 0f)
        {
            var hits = Physics.SphereCastAll(lastPos, sphereRadius, dir.normalized, dist, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider == selfCol) continue;

                // 先找父层级的 Health（命中 Boss 的任意子碰撞体都能扣血）
                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null)
                {
                    if (!string.IsNullOrEmpty(targetTag))
                    {
                        // 如果你坚持按 Tag 过滤，就要求 Health 节点或其父节点有这个 Tag
                        var root = h.gameObject;
                        if (!root.CompareTag(targetTag) && !(root.transform.parent && root.transform.parent.CompareTag(targetTag)))
                        {
                            if (logHits) Debug.Log($"Hit {hit.collider.name} (has Health but tag mismatch), ignored");
                            continue;
                        }
                    }

                    if (logHits) Debug.Log($"Projectile hit {hit.collider.name} → {h.gameObject.name}, dmg={damage}");
                    h.TakeDamage(damage);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // 没命中就推进
        if (rb && rb.isKinematic) rb.MovePosition(nextPos);
        else transform.position = nextPos;

        lastPos = transform.position;
    }
}
