using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class FeatherProjectile : MonoBehaviour
{
    [Header("Targeting")]
    public string playerTag = "Player";
    public float lagSeconds = 0.5f;              // 追踪玩家过去的位置（没有 recorder 也能跑）

    [Header("Motion")]
    public float speed = 12f;                    // 水平前进速度
    public float turnRateDeg = 180f;             // 只绕世界Y轴的转向速度
    public Rigidbody rb;                         // 可为空；非Kinematic则用velocity推进
    public float lifeTime = 1.0f;                // <=0 不自毁
    public bool keepStartHeight = true;          // 锁定发射高度

    [Header("Damage")]
    public int damage = 20;

    [Header("Visual Orientation")]
    public Transform visual;                     // ★ 羽毛网格子物体（不要拖根）
    public bool alignVisualYawToForward = true;  // ★ 视觉跟随根的“Yaw”
    public Vector3 visualFlatLocalEuler = new Vector3(-90f, 0f, 0f);
    // ↑ 让网格“躺平”的欧拉角。若尖端方向不对，改成 (90,0,0) 或 (0,0,90) 等测试。

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

        // 启动时把根的朝向“水平化”，避免 forward 带俯仰
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
            StartCoroutine(SelfDestructAfter(lifeTime)); // 兜底
        }
    }

    void Update()
    {
        // --- 只在水平面追踪（绕Y轴） ---
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

        // --- 水平前进 ---
        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        fwd.Normalize();

        if (rb && !rb.isKinematic) rb.linearVelocity = fwd * speed;
        else transform.position += fwd * (speed * Time.deltaTime);

        if (keepStartHeight)
            transform.position = new Vector3(transform.position.x, _startY, transform.position.z);

        // --- 视觉：保持水平 + 跟随Yaw（尖端对着玩家） ---
        if (visual)
        {
            if (alignVisualYawToForward)
            {
                // 只拿根的 Yaw，叠加一个“躺平”的本地欧拉修正
                Quaternion yaw = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up);
                visual.rotation = yaw * Quaternion.Euler(visualFlatLocalEuler);
            }
            else
            {
                // 不跟随朝向时，纯粹保持固定水平角
                visual.rotation = Quaternion.Euler(visualFlatLocalEuler);
            }
        }

        // --- 自毁 ---
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
