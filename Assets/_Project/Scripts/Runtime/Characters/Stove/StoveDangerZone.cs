using UnityEngine;
using Antventure.UI;

public class StoveDangerZone : MonoBehaviour
{
    [Header("Related Fires")]
    public FireController[] associatedFires;

    [Header("Dangerous Settings")]
    public bool isDangerous = true;
    
    [Header("状态跟踪")]
    private bool wasDangerousLastCheck = true; // 默认假设初始是危险的
    private bool hasCheckedAtLeastOnce = false;

    void Start()
    {
        if (associatedFires == null || associatedFires.Length == 0)
        {
            associatedFires = FindObjectsByType<FireController>(FindObjectsSortMode.None);
        }
        
        // 初始化状态
        wasDangerousLastCheck = IsCurrentlyDangerousInternal();
    }

    // 内部检查方法，不触发状态变化事件
    private bool IsCurrentlyDangerousInternal()
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

    // 公开的检查方法，会触发状态变化事件
    public bool IsCurrentlyDangerous()
    {
        bool isDangerousNow = IsCurrentlyDangerousInternal();
        hasCheckedAtLeastOnce = true;
        
        // 检查状态是否从危险变为安全
        if (wasDangerousLastCheck && !isDangerousNow)
        {
            OnBecameSafe();
        }
        // 检查状态是否从安全变为危险
        else if (!wasDangerousLastCheck && isDangerousNow)
        {
            OnBecameDangerous();
        }
        
        // 更新上次检查的状态
        wasDangerousLastCheck = isDangerousNow;
        
        return isDangerousNow;
    }

    // 当灶台从危险变为安全时调用
    private void OnBecameSafe()
    {
        Debug.Log("灶台已安全 - 从危险状态变为安全状态");
        
        // 显示UI提示
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowStoveSafeHint();
        }
    }

    // 当灶台从安全变为危险时调用（可选）
    private void OnBecameDangerous()
    {
        Debug.Log("灶台变得危险了！");
        // 这里可以添加变为危险时的UI提示或其他逻辑
    }

    // 手动调用检查状态变化（如果需要）
    public void CheckStateChange()
    {
        IsCurrentlyDangerous();
    }

    // 当灶台安全时手动调用（用于按钮等其他触发方式）
    public void OnStoveSafe()
    {
        Debug.Log("灶台已安全");
        
        // 显示UI提示
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowStoveSafeHint();
        }
    }

    public void OnPlayerEnter(PlayerController player)
    {
        // 先检查当前状态
        bool isDangerousNow = IsCurrentlyDangerous();
        
        if (isDangerousNow)
        {
            Debug.Log("玩家碰到危险的灶台！");
            player.Die();
        }
        else
        {
            Debug.Log("灶台现在安全。");
        }
    }

    // 在Update中持续检查状态变化（如果需要实时监测）
    private void Update()
    {
        // 如果需要实时监测状态变化，可以取消注释下面这行
        IsCurrentlyDangerous();
    }
}