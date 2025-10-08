using UnityEngine;

public class CookiePickup : MonoBehaviour
{
    public int amount = 1;
    [Range(0f,1f)] public float sfxVolumeOverride = -1f; // -1 表示使用 GlobalSfx 的默认音量
    public bool sfxAs2D = true; // 想要3D就勾掉

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CookiesInventory.Instance?.Add(amount);
        // 兜底刷新
        if (CookiesInventory.Instance != null)
            SkillChargeUI.Instance?.Refresh(CookiesInventory.Instance.cookies);

        // 播放拾取音效（走全局管理器）
        if (GlobalSfx.Instance != null)
        {
            float? vol = (sfxVolumeOverride >= 0f) ? (float?)sfxVolumeOverride : null;
            GlobalSfx.PlayCookieSfx(transform.position, vol, sfxAs2D);
        }

        Debug.Log($"[Cookie] Picked, total={CookiesInventory.Instance?.cookies}");

        gameObject.SetActive(false); // 保持可被 ResetCrumbs() 重新启用
    }
}
