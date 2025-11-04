using UnityEngine;

public class ResetCookiesOnGameStart : MonoBehaviour
{
    [Tooltip("是否在 Awake 时立即清空 Cookies。")]
    public bool resetOnAwake = true;

    private void Awake()
    {
        if (resetOnAwake && CookiesInventory.Instance != null)
        {
            CookiesInventory.Instance.Clear();
            Debug.Log("[ResetCookiesOnGameStart] Cookies cleared at game start.");
        }
    }

    // 如果你的 StartScene 有“New Game”按钮，可以在按钮点击里手动调用：
    public void ResetCookiesNow()
    {
        if (CookiesInventory.Instance != null)
        {
            CookiesInventory.Instance.Clear();
            Debug.Log("[ResetCookiesOnGameStart] Cookies cleared via button.");
        }
    }
}
