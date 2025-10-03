using UnityEngine;

public class CookiePickup : MonoBehaviour
{
    public int amount = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CookiesInventory.Instance?.Add(amount);
        // 即使事件没连接，也手动刷新一次（安全兜底）
        if (CookiesInventory.Instance != null)
            SkillChargeUI.Instance?.Refresh(CookiesInventory.Instance.cookies);
            
        Debug.Log($"[Cookie] Picked, total={CookiesInventory.Instance?.cookies}");

        Destroy(gameObject);
        Debug.Log($"[Cookie] Picked, total={(CookiesInventory.Instance?CookiesInventory.Instance.cookies:-1)}");

    }
}
