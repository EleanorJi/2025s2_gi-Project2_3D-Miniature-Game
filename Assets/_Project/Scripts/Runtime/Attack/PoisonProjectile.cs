using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Collider))]
public class PoisonProjectile : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 8f;
    public float lifeTime = 3f;

    [Header("Damage")]
    public int damage = 1;
    public string targetTag = "Boss";

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

        // The movement of the trigger is driven by us
        if (selfCol) selfCol.isTrigger = true;
        if (rb) { rb.isKinematic = true; rb.useGravity = false; }

        // Get the sphere radius (if using a SphereCollider)
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
        // First, calculate the next frame position
        Vector3 nextPos = transform.position + transform.forward * speed * Time.fixedDeltaTime;

        // Sweep between lastPos → nextPos, including Trigger
        Vector3 dir = nextPos - lastPos;
        float dist = dir.magnitude;
        if (dist > 0f)
        {
            var hits = Physics.SphereCastAll(lastPos, sphereRadius, dir.normalized, dist, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider == selfCol) continue;

                
                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null)
                {
                    if (!string.IsNullOrEmpty(targetTag))
                    {
                        
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

        // If not hit, just move forward
        if (rb && rb.isKinematic) rb.MovePosition(nextPos);
        else transform.position = nextPos;

        lastPos = transform.position;
    }
}
