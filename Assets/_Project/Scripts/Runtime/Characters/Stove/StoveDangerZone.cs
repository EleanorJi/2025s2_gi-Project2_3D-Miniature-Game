using UnityEngine;

public class StoveDangerZone : MonoBehaviour
{
    [Header("Related Fires")]
    // 在Inspector中拖拽所有相关的火焰到这里
    public FireController[] associatedFires;

    [Header("Dangerous Settings")]
    public bool isDangerous = true; // 可以在运行时调整

    void Start()
    {
        // 如果没有手动关联，尝试自动查找
        if (associatedFires == null || associatedFires.Length == 0)
        {
            // 查找场景中所有火焰控制器
            associatedFires = FindObjectsByType<FireController>(FindObjectsSortMode.None);
        }
    }

    // 检查灶台当前是否危险
    public bool IsCurrentlyDangerous()
    {
        if (!isDangerous) return false;
        
        // 如果任何关联的火焰还在缩小或未完全缩小，就是危险的
        foreach (FireController fire in associatedFires)
        {
            if (fire != null && (fire.IsShrinking || !fire.IsFullyShrunk))
            {
                return true;
            }
        }
        
        return false;
    }

    // 当玩家进入灶台区域时调用
    public void OnPlayerEnter(PlayerController player)
    {
        if (IsCurrentlyDangerous())
        {
            Debug.Log("玩家碰到危险的灶台！");
            player.Die(); // 调用玩家的死亡方法
        }
        else
        {
            Debug.Log("灶台现在安全。");
        }
    }
}