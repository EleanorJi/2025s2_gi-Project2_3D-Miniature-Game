using UnityEngine;

public class StoveDangerZone : MonoBehaviour
{
    [Header("RelatedFires")]
    // 在Inspector中拖拽所有相关的火焰到这里
    public FireController[] associatedFires;

    [Header("DangerousSettings")]
    public bool isDangerous = true; // 可以在运行时调整

    void Start()
    {
        // 如果没有手动关联，尝试自动查找
        if (associatedFires == null || associatedFires.Length == 0)
        {
            // 方法1：查找场景中所有火焰
            associatedFires = FindObjectsByType<FireController>(FindObjectsSortMode.None);
            
            // 方法2：或者通过标签查找
            // GameObject[] fireObjects = GameObject.FindGameObjectsWithTag("Fire");
            // associatedFires = new FireController[fireObjects.Length];
            // for (int i = 0; i < fireObjects.Length; i++)
            // {
            //     associatedFires[i] = fireObjects[i].GetComponent<FireController>();
            // }
        }
    }

    // 检查灶台当前是否危险
    public bool IsCurrentlyDangerous()
    {
        if (!isDangerous) return false;
        
        // 如果任何关联的火焰还在下降或未完全下降，就是危险的
        foreach (FireController fire in associatedFires)
        {
            if (fire != null && (fire.IsDescending || !fire.IsFullyDescended))
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