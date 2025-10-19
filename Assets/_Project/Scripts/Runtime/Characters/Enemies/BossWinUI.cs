using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class BossWinUI : MonoBehaviour
{
    [Header("Refs")]
    public Health boss;                 // Boss Health (会自动用 Tag=Boss 查找)
    public GameObject panel;            // Win 面板（不填则用本物体）
    public TMP_Text hintText;           // 可选的提示文案

    [Header("UI to hide on win")]
    public GameObject[] hideOnWin;      // 把 PlayerHealthBar（或其它HUD）拖进来

    [Header("Copy")]
    [TextArea] public string hint = "You defeated the pigeon!\nLeft click to continue";

    [Header("After Win")]
    public string sceneToLoad = "StartScene"; // 左键时要回到的场景

    // gameplay gating
    private MonoBehaviour[] _disabledPlayerScripts;
    private MonoBehaviour[] _disabledCameraScripts;
    private bool _shown;

    private void Awake()
    {
        if (!panel) panel = gameObject;

        // 找 Boss 并订阅
        if (!boss)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) boss = b.GetComponent<Health>();
        }
        if (boss) boss.OnDeath.AddListener(HandleBossDied);

        // 需要禁用/恢复的脚本列表
        BuildDisableLists(FindPlayerGO(), Camera.main ? Camera.main.gameObject : null);

        // 初始化隐藏面板
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
        // 隐藏 HUD（如玩家血条）
        if (hideOnWin != null)
        {
            foreach (var go in hideOnWin)
            {
                if (go) go.SetActive(false);
            }
        }

        ShowPanel(true);
    }

    private void Continue()
    {
        // 恢复时间与控制，再切场景
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        SetScriptsEnabled(_disabledPlayerScripts, true);
        SetScriptsEnabled(_disabledCameraScripts, true);

        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
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

            // 禁用移动/攻击/镜头
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
