using UnityEngine;

[DisallowMultipleComponent]
public class ResetBossOnRespawn : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth player;     
    public Health bossHealth;       
    public BossHealthBarUI bossBar; 

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

        bossHealth.ResetToFull();    


        if (bossBar) bossBar.RefreshNow();

        Debug.Log("[ResetBossOnRespawn] Boss reset to full and UI refreshed.");
    }
}
