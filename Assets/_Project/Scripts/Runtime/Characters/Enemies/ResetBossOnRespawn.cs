using UnityEngine;

[DisallowMultipleComponent]
public class ResetBossOnRespawn : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth player;     // 可留空按 Tag=Player 自动找
    public Health bossHealth;       // 可留空按 Tag=Boss 自动找
    public BossHealthBarUI bossBar; // 可选：强制刷新 UI

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.GetComponent<PlayerHealth>();
        }
        if (!bossHealth)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) bossHealth = b.GetComponent<Health>();
        }
        if (!bossBar) bossBar = FindObjectOfType<BossHealthBarUI>(true);
    }

    void OnEnable()
    {
        if (player) player.OnRespawned.AddListener(ResetNow);
    }

    void OnDisable()
    {
        if (player) player.OnRespawned.RemoveListener(ResetNow);
    }

    public void ResetNow()
    {
        if (!bossHealth) return;

        bossHealth.ResetToFull();     // 这会触发 OnHealthChanged( max, max )

        // 保险起见，强制 UI 刷新一次（避免序列化/启用顺序导致 UI 没收到第一帧事件）
        if (bossBar) bossBar.RefreshNow();

        Debug.Log("[ResetBossOnRespawn] Boss reset to full and UI refreshed.");
    }
}
