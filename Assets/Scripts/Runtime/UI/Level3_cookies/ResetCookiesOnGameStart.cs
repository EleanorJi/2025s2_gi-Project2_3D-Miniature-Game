using UnityEngine;

public class ResetCookiesOnGameStart : MonoBehaviour
{

    public bool resetOnAwake = true;

    private void Awake()
    {
        if (resetOnAwake && CookiesInventory.Instance != null)
        {
            CookiesInventory.Instance.Clear();
            Debug.Log("[ResetCookiesOnGameStart] Cookies cleared at game start.");
        }
    }


    public void ResetCookiesNow()
    {
        if (CookiesInventory.Instance != null)
        {
            CookiesInventory.Instance.Clear();
            Debug.Log("[ResetCookiesOnGameStart] Cookies cleared via button.");
        }
    }
}
