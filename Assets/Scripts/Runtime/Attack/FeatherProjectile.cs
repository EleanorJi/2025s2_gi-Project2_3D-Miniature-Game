using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class FeatherProjectile : MonoBehaviour
{
    [Header("Targeting")]
    public string playerTag = "Player";
    public float lagSeconds = 0.5f;              // follow player position with this much delay

    [Header("Motion")]
    public float speed = 12f;                    // horizontal forward speed
    public float turnRateDeg = 180f;             // turning speed around the world's Y axis
    public Rigidbody rb;                         // can be null; use velocity to move if not kinematic
    public float lifeTime = 1.0f;                // <=0 means no self-destruction
    public bool keepStartHeight = true;          // lock launch height

    [Header("Damage")]
    public int damage = 20;

    [Header("Visual Orientation")]
    public Transform visual;                     // ★ feather mesh object
    public bool alignVisualYawToForward = true;  // visual yaw follows projectile forward
    public Vector3 visualFlatLocalEuler = new Vector3(-90f, 0f, 0f);

    // --- internal ---
    Transform _player;
    PlayerPositionRecorder _rec;
    float _dieAt = -1f;
    float _startY;

    void Reset()
    {
        TryGetComponent(out rb);
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) { _player = p.transform; _rec = _player.GetComponent<PlayerPositionRecorder>(); }

        if (!rb) TryGetComponent(out rb);
        if (rb) rb.useGravity = false;

        if (!visual && transform.childCount > 0)
            visual = transform.GetChild(0);

        // When starting up, align the root in a "horizontal" position to avoid the "forward" movement causing pitch.
        Vector3 f = transform.forward; f.y = 0f;
        if (f.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);

        _startY = transform.position.y;
    }

    void Start()
    {
        if (lifeTime > 0f)
        {
            _dieAt = Time.time + lifeTime;
            StartCoroutine(SelfDestructAfter(lifeTime)); // Guaranteeing the bottom line
        }
    }

    void Update()
    {
        // --- Only tracking on the horizontal plane (around the Y-axis) ---
        if (_player && turnRateDeg > 0f)
        {
            Vector3 targetPos = _rec ? _rec.GetPastPosition(lagSeconds) : _player.position;
            Vector3 to = targetPos - transform.position; to.y = 0f;

            Vector3 curF = transform.forward; curF.y = 0f;
            if (to.sqrMagnitude > 1e-6f && curF.sqrMagnitude > 1e-6f)
            {
                Quaternion curYaw = Quaternion.LookRotation(curF.normalized, Vector3.up);
                Quaternion desiredYaw = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(curYaw, desiredYaw, turnRateDeg * Time.deltaTime);
            }
        }

        // --- Progress in Level ---
        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        fwd.Normalize();

        if (rb && !rb.isKinematic) rb.linearVelocity = fwd * speed;
        else transform.position += fwd * (speed * Time.deltaTime);

        if (keepStartHeight)
            transform.position = new Vector3(transform.position.x, _startY, transform.position.z);

        // Visuals: Maintain horizontal position + Follow Yaw (the tip points towards the player)
        if (visual)
        {
            if (alignVisualYawToForward)
            {
                // Just a single "yaw" value, combined with a local Euler correction for "lying flat"
                Quaternion yaw = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up);
                visual.rotation = yaw * Quaternion.Euler(visualFlatLocalEuler);
            }
            else
            {
                // When not following the direction, simply maintain a fixed horizontal angle
                visual.rotation = Quaternion.Euler(visualFlatLocalEuler);
            }
        }

        // Self-destruction
        if (_dieAt > 0f && Time.time >= _dieAt)
            Destroy(gameObject);
    }

    IEnumerator SelfDestructAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (this) Destroy(gameObject);
    }


    void OnTriggerEnter(Collider other)
    {
        if (!other || !other.CompareTag(playerTag))
            return;


        var hp = other.GetComponent<PlayerHealth>();
        if (hp) hp.TakeDamage("Feather", damage);

       

        Destroy(gameObject, 0.3f);
    }

   
}
