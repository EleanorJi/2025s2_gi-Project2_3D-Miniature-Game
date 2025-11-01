using UnityEngine;

public class BugDestory : MonoBehaviour
{
    // 死后需要生成的物体（掉落物、饼干等）
    public GameObject dropPrefab;

    // 防止重复死亡（一个虫子只会掉落一次）
    private bool isDying = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            Die();
        }
    }

    // 判断死后是否应该掉落
    bool ShouldDropOnDeath()
    {
        // 查找名字为 "Cookie" 的子对象
        Transform cookie = transform.Find("Cookie");
        if (cookie != null)
        {
            // 只有当 Cookie 子对象处于启用状态时才触发掉落
            return cookie.gameObject.activeInHierarchy;
        }

        // 如果没有名为 Cookie 的子对象，保留原有的掉落行为
        return true;
    }

    // 统一的死亡处理，先生成掉落物再销毁自身
    void Die()
    {
        if (isDying) return;
        isDying = true;

        // 按条件决定是否掉落
        if (ShouldDropOnDeath() && dropPrefab != null)
        {
            Instantiate(dropPrefab, transform.position, Quaternion.identity);
        }

        // 销毁虫子对象
        Destroy(gameObject);
    }
}
