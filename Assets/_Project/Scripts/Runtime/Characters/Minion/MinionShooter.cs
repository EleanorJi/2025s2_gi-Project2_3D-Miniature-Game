using UnityEngine;

public class MinionShooter : MonoBehaviour
{
    [Header("Target")]
    public Transform target;            // player
    public string targetTag = "Boss";   // boss tag

    [Header("Shoot")]
    public Transform firePoint;         // Minion's fire point (a child object named firePoint in the prefab)
    public GameObject projectilePrefab; // Projectile prefab (same as player's)
    public float fireEvery = 0.7f;      // Fire interval
    public int minionDamage = 1;        // Minion damage
    public float rotateSpeed = 10f;     // Rotation speed

    [Header("Life")]
    public float lifeTime = 20f;        // Auto-destroy after N seconds

    private float lastFireTime;

    void Start()
    {
        // Auto find firePoint if not assigned
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
        // Auto find target if lost
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;
        }

        // auto rotate towards target
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

        // Auto fire
        if (Time.time - lastFireTime >= fireEvery && projectilePrefab != null)
        {
            lastFireTime = Time.time;

            var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            var proj = go.GetComponent<PoisonProjectile>();
            if (proj != null)
            {
                proj.damage = minionDamage;   // damage = 1
                proj.targetTag = targetTag;
            }
        }
    }
}
