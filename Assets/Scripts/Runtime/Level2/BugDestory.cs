using UnityEngine;

public class BugDestory : MonoBehaviour
{
    // 可以被销毁的物体（例如饼干、糖果等）
    public GameObject dropPrefab;

    // 防止重复销毁，确保一个对象只销毁一次
    private bool isDying = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
        // 检测与PlayerPoisonShooter粒子的碰撞
        else if (other.CompareTag("PoisonParticle"))
        {
          
            
            Die();
        }
    }

    // 判断敌人是否应该掉落物品
    bool ShouldDropOnDeath()
    {
        // 查找名为"Cookie"的子对象
        Transform cookie = transform.Find("Cookie");
        if (cookie != null)
        {
            // 只有当Cookie子对象处于激活状态时才掉落物品
            return cookie.gameObject.activeInHierarchy;
        }

        // 如果没有名为Cookie的子对象，默认掉落物品
        return true;
    }

    // 统一的销毁处理方法，用于生成掉落物品等
    public void Die()
    {
        if (isDying) return;
        isDying = true;

        // 检查是否应该掉落物品
        if (ShouldDropOnDeath() && dropPrefab != null)
        {
            Instantiate(dropPrefab, transform.position, Quaternion.identity);
        }

        // 销毁对象
        Destroy(gameObject);
    }
}