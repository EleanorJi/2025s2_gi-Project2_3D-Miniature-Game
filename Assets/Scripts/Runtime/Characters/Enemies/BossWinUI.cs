using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class BossWinUI : MonoBehaviour
{
    [Header("Refs")]
    public Health boss;                 // Boss Health
    public GameObject panel;            // Win panel
    public TMP_Text hintText;           // Optional hint text

    [Header("UI to hide on win")]
    public GameObject[] hideOnWin;      // Drag PlayerHealthBar (or other HUD) here

    [Header("Cookies")]
    [Tooltip("通关时是否把 Cookie 清零并刷新 UI")]
    public bool resetCookiesOnWin = true;

    [Header("Copy")]
    [TextArea] public string hint = "You defeated the pigeon!\nLeft click to continue";

    [Header("After Win")]
    public string sceneToLoad = "StartScene"; // Scene to load on left click

    // gameplay gating
    private MonoBehaviour[] _disabledPlayerScripts;
    private MonoBehaviour[] _disabledCameraScripts;
    private bool _shown;

    private void Awake()
    {
        if (!panel) panel = gameObject;

        // Find Boss and subscribe
        if (!boss)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) boss = b.GetComponent<Health>();
        }
        if (boss) boss.OnDeath.AddListener(HandleBossDied);

        // Build lists of scripts to disable/restore
        BuildDisableLists(FindPlayerGO(), Camera.main ? Camera.main.gameObject : null);

        // Initialize hidden panel
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
        // 1) 先处理 Cookie —— 会自动触发 Level3CookieUI.Refresh
        if (resetCookiesOnWin && CookiesInventory.Instance != null)
        {
            CookiesInventory.Instance.Clear();   // cookies = 0; OnChanged(0)
        }

        // 2) 隐藏 HUD（玩家血条等）
        if (hideOnWin != null)
        {
            foreach (var go in hideOnWin)
            {
                if (go) go.SetActive(false);
            }
        }

        // 3) 打开胜利面板 & 冻结游戏
        ShowPanel(true);
    }

    private void Continue()
    {
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
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Disable movement/attack/camera
            SetScriptsEnabled(_disabledPlayerScripts, false);
            SetScriptsEnabled(_disabledCameraScripts, false);
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
