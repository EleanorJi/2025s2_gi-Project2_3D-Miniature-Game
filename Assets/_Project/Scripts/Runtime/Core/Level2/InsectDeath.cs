using UnityEngine;

public class InsectDeath : MonoBehaviour
{
    [Header("可选 FX")]
    public GameObject explosionFxPrefab;

    [Header("背饼干（同一颗从背上掉）")]
    public bool carryCookie = true;
    public Transform carryPoint;        // 可选：背部定位点
    public GameObject cookieOnBack;     // 场景里这颗子物体
    public float dropImpulse = 1.2f;
    public float dropTorque  = 0.8f;

    [Header("撞到玩家是否秒杀")]
    public bool killPlayerOnTouch = true;

    private bool dropped = false;       // 防二次执行

    public void Kill()
    {
        // FX
        if (explosionFxPrefab)
        {
            var fx = Instantiate(explosionFxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        // 掉落同一颗饼干（只做一次）
        if (!dropped && carryCookie && cookieOnBack)
        {
            dropped = true;

            // 对齐到背部定位点（如果有）
            if (carryPoint)
            {
                cookieOnBack.transform.position = carryPoint.position;
                cookieOnBack.transform.rotation = carryPoint.rotation;
            }

            // 解绑
            cookieOnBack.transform.SetParent(null);

            // 只改刚体，不改碰撞器（你的两个 Collider 设置保持不变）
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

        // 销毁虫子
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision c)
    {
        if (!killPlayerOnTouch) return;
        if (c.collider.CompareTag("Player"))
            c.collider.GetComponent<PlayerController>()?.Die();
    }
}
