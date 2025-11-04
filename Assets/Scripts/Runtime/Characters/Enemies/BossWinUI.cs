using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class BossWinUI : MonoBehaviour
{
    // ★ Global flag: has the boss already been defeated in this scene?
    public static bool HasWon { get; private set; }

    [Header("Refs")]
    public Health boss;                 // Boss Health
    public GameObject panel;            // Win panel
    public TMP_Text hintText;           // Optional hint text

    [Header("UI to hide on win")]
    public GameObject[] hideOnWin;      // Drag PlayerHealthBar (or other HUD) here

    [Header("Cleanup on Win")]

    public bool destroyAllMinionsOnWin = true;


    public bool destroyAllProjectilesOnWin = true;

    [Header("Copy")]
    [TextArea] public string hint = "You defeated the pigeon!\nLeft click to continue";

    [Header("After Win")]
    public string sceneToLoad = "StartScene"; // 现在走 SceneOrderManager

    [Header("Audio")]
    [Tooltip("Sound effect that plays once when the boss is defeated")]
    [SerializeField] private AudioClip winSfx;   // Drag Success.mp3 here

    // Gameplay gating
    private MonoBehaviour[] _disabledPlayerScripts;
    private MonoBehaviour[] _disabledCameraScripts;
    private bool _shown;

    private void Awake()
    {
        // Reset flag whenever this scene loads, so it doesn't carry over
        HasWon = false;

        if (!panel) panel = gameObject;

        // Find Boss and subscribe to its death event
        if (!boss)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) boss = b.GetComponent<Health>();
        }
        if (boss) boss.OnDeath.AddListener(HandleBossDied);

        // Build lists of scripts to disable/restore
        BuildDisableLists(FindPlayerGO(), Camera.main ? Camera.main.gameObject : null);

        // Initialize panel as hidden
        if (panel.activeSelf) panel.SetActive(false);
        _shown = false;
    }

    private void OnDestroy()
    {
        if (boss) boss.OnDeath.RemoveListener(HandleBossDied);
    }

    private void Update()
    {
        if (_shown && Input.GetMouseButtonDown(0))
        {
            Continue();
        }
    }

    private void HandleBossDied()
    {
        if (HasWon) return;   // Avoid multiple triggers
        HasWon = true;

        // ★ Play victory sound
        PlayWinSfx();

        // 0) Clean up minions and projectiles so they stop attacking the player
        CleanupOnWin();

        // 1) Force clear cookies (no longer depends on any bool flag)
        if (CookiesInventory.Instance != null)
        {
            CookiesInventory.Instance.Clear();   // cookies = 0; OnChanged(0)
        }

        // 2) Hide HUD (player health bar, etc.)
        if (hideOnWin != null)
        {
            foreach (var go in hideOnWin)
            {
                if (go) go.SetActive(false);
            }
        }

        // 3) Show win panel & pause the game
        ShowPanel(true);
    }

    private void Continue()
    {
        // Double-check: clear cookies again before leaving the level
        if (CookiesInventory.Instance != null)
        {
            CookiesInventory.Instance.Clear();
        }

        // Restore time and controls, then change scene
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        SetScriptsEnabled(_disabledPlayerScripts, true);
        SetScriptsEnabled(_disabledCameraScripts, true);

        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneOrderManager.Instance.LoadNextScene();
        else
            ShowPanel(false);
    }

    private void ShowPanel(bool show)
    {
        _shown = show;
        if (panel) panel.SetActive(show);
        if (hintText) hintText.text = hint;

        if (show)
        {
            // Real pause
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Disable movement/attack/camera
            SetScriptsEnabled(_disabledPlayerScripts, false);
            SetScriptsEnabled(_disabledCameraScripts, false);

            // Extra safety: disable all FeatherShooter so no new feathers are spawned
            var shooters = FindObjectsOfType<FeatherShooter>();
            foreach (var s in shooters)
            {
                if (s) s.enabled = false;
            }
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            SetScriptsEnabled(_disabledPlayerScripts, true);
            SetScriptsEnabled(_disabledCameraScripts, true);
        }
    }

    /// <summary>
    /// Victory cleanup: destroy minions and projectiles so they stop hitting the player / stop playing hit sounds.
    /// </summary>
    private void CleanupOnWin()
    {
        if (destroyAllMinionsOnWin)
        {
            var minions = FindObjectsOfType<MinionAnchor>();
            foreach (var m in minions)
            {
                if (m != null) Destroy(m.gameObject);
            }
        }

        if (destroyAllProjectilesOnWin)
        {
            // Feather projectiles
            var feathers = FindObjectsOfType<FeatherProjectile>();
            foreach (var f in feathers)
            {
                if (f != null) Destroy(f.gameObject);
            }

            // Other projectiles (poison etc.), if they exist
            var poison = FindObjectsOfType<PoisonProjectile>();
            foreach (var p in poison)
            {
                if (p != null) Destroy(p.gameObject);
            }
        }
    }

    /// <summary>
    /// Play the victory sound once at the camera position.
    /// </summary>
    private void PlayWinSfx()
    {
        if (winSfx == null) return;

        if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(winSfx, Camera.main.transform.position);
        }
        else
        {
            AudioSource.PlayClipAtPoint(winSfx, Vector3.zero);
        }
    }

    private void BuildDisableLists(GameObject player, GameObject camGO)
    {
        _disabledPlayerScripts = player ? player.GetComponents<MonoBehaviour>() : new MonoBehaviour[0];
        _disabledCameraScripts = camGO ? camGO.GetComponents<MonoBehaviour>() : new MonoBehaviour[0];
    }

    private void SetScriptsEnabled(MonoBehaviour[] list, bool enabled)
    {
        if (list == null) return;
        foreach (var m in list)
        {
            if (!m || m == this) continue;
            m.enabled = enabled;
        }
    }

    private GameObject FindPlayerGO()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        return p ? p : null;
    }
}
