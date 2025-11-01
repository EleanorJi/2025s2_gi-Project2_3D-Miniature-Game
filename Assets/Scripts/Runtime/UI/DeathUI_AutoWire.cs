using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Antventure.UI;

public class DeathUI_AutoWire : MonoBehaviour
{
    [Header("Refs (optional – will auto-find)")]
    [SerializeField] private PlayerHealth playerHealth; // 可为空：自动找
    [SerializeField] private GameObject panel;          // 指向你的 final_death 图片所在的 GameObject（或其父容器）
    [SerializeField] private TMP_Text hintText;         // 可为空
    [TextArea][SerializeField] private string hint = "Left click to respawn";

    [Header("Cursor (optional)")]
    [SerializeField] private bool unlockCursorOnDeath = true;

    [Header("Background Transparency")]
    [SerializeField][Range(0f, 1f)] private float backgroundAlpha = 0.8f; // 背景透明度，0=完全透明，1=完全不透明
    [SerializeField][Range(0f, 1f)] private float imageAlpha = 1f; // 死亡图片透明度
    [SerializeField][Range(0f, 1f)] private float textAlpha = 1f; // 提示文字透明度
    [SerializeField] private bool separateImageTextAlpha = false; // 是否分别控制图片和文字透明度

    [Header("Global Controller Integration")]
    [SerializeField] private bool useGlobalController = true;
    [Tooltip("如果启用，将使用GlobalDeathUIController来显示死亡UI")]
    [SerializeField] private Sprite deathSprite; // 死亡图片（用于全局控制器）

    // runtime state
    private bool _isShown;
    private readonly List<MonoBehaviour> _disabledPlayerScripts = new();
    private readonly List<MonoBehaviour> _disabledCameraScripts = new();

    // ---------- Unity 6 / 2023+ safe finder ----------
    static T FindOne<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }

    public int cookie;

    private void Awake()
    {

        cookie = CookiesInventory.Instance.cookies;
        // auto-wire PlayerHealth by tag or by type
        if (!playerHealth)
        {
            // 先按 Tag=Player 找场上玩家
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo) playerHealth = pgo.GetComponent<PlayerHealth>();
            if (!playerHealth) playerHealth = FindOne<PlayerHealth>();
        }

        if (!playerHealth)
        {
            Debug.LogError("[DeathUI] PlayerHealth NOT found in scene. " +
                           "Give your player the 'Player' tag or drag PlayerHealth into the field.");
            enabled = false;
            return;
        }

        // 默认把本物体当作面板（如果你把脚本挂在图片上）
        if (!panel) panel = gameObject;

        // 初始隐藏
        SetCanvasGroupVisible(false);
        if (panel && panel.activeSelf) panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (!playerHealth) return;
        playerHealth.OnDied.AddListener(HandleDied);
        playerHealth.OnRespawned.AddListener(HandleRespawned);
#if UNITY_EDITOR
        Debug.Log($"[DeathUI] Wired to PlayerHealth on '{playerHealth.gameObject.name}'.");
#endif
    }

    private void OnDisable()
    {
        if (!playerHealth) return;
        playerHealth.OnDied.RemoveListener(HandleDied);
        playerHealth.OnRespawned.RemoveListener(HandleRespawned);
#if UNITY_EDITOR
        Debug.Log("[DeathUI] Unsubscribed.");
#endif
    }

    private void Update()
    {
        // 死亡界面显示中，左键=复活
        if (_isShown && Input.GetMouseButtonDown(0) && playerHealth != null)
        {
            //bug修改:把上个场景中获取的饼干数量给到临时变量，在死后把临时变量重新分配到已保存的值，然后刷新一下ui
            CookiesInventory.Instance.cookies = cookie;
            cookie = CookiesInventory.Instance ? CookiesInventory.Instance.cookies : 0;
            Level3CookieUI.Instance.Refresh(cookie);
            playerHealth.Respawn();
        }
    }

    // =================== Event Handlers ===================

    private void HandleDied()
    {
        // 检查当前场景是否为第三关boss场景，如果是则强制使用本地UI
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isLevel3Boss = currentSceneName == "Level3_boss";
        
        // 如果启用全局控制器且存在，并且不是第三关，则使用全局控制器
        if (useGlobalController && GlobalDeathUIController.Instance != null && !isLevel3Boss)
        {
            GlobalDeathUIController.Instance.ShowDeathUI(hint, deathSprite, 4f);

            // 仍然需要处理游戏暂停和脚本禁用
            HandleGamePause(true);
            return;
        }

        // 否则使用本地UI（包括第三关强制使用本地UI的情况）
        if (hintText) hintText.text = hint;
        ShowPanel(true);
        ApplyTransparencySettings();
    }

    private void HandleRespawned()
    {
        // 检查当前场景是否为第三关boss场景，如果是则强制使用本地UI
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isLevel3Boss = currentSceneName == "Level3_boss";
        
        // 如果使用全局控制器且不是第三关，隐藏全局UI
        if (useGlobalController && GlobalDeathUIController.Instance != null && !isLevel3Boss)
        {
            GlobalDeathUIController.Instance.HideDeathUI();
            HandleGamePause(false);
            return;
        }

        // 否则使用本地UI（包括第三关强制使用本地UI的情况）
        ShowPanel(false);
    }

    // =================== UI control & gameplay gating ===================

    private void ShowPanel(bool show)
    {
        _isShown = show;

        if (panel)
        {
            // 若脚本挂在图片本体上，优先使用 CanvasGroup 控制透明与交互
            if (ReferenceEquals(panel, gameObject))
                SetCanvasGroupVisible(show);
            else
                panel.SetActive(show);
        }

        HandleGamePause(show);
    }

    /// <summary>
    /// 处理游戏暂停相关逻辑（分离出来供全局控制器使用）
    /// </summary>
    private void HandleGamePause(bool pause)
    {
        if (pause)
        {
            // 暂停时间 & 显示鼠标
            Time.timeScale = 0f;
            if (unlockCursorOnDeath)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // 禁用玩家/相机所有可疑脚本（除 PlayerHealth 自己与本脚本）
            BuildDisableLists(playerHealth.gameObject, Camera.main ? Camera.main.gameObject : null);
            SetScriptsEnabled(_disabledPlayerScripts, false);
            SetScriptsEnabled(_disabledCameraScripts, false);
        }
        else
        {
            // 恢复
            Time.timeScale = 1f;

            // 收起面板后恢复脚本
            SetScriptsEnabled(_disabledPlayerScripts, true);
            SetScriptsEnabled(_disabledCameraScripts, true);
            _disabledPlayerScripts.Clear();
            _disabledCameraScripts.Clear();

            // 隐藏/锁定鼠标（交回你的原始游玩状态）
            if (unlockCursorOnDeath)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void SetCanvasGroupVisible(bool visible)
    {
        // 使用 CanvasGroup 在同一对象上做优雅显隐（不破坏激活链）
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = visible ? backgroundAlpha : 0f; // 使用可调节的背景透明度
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    /// <summary>
    /// 应用透明度设置到各个UI元素
    /// </summary>
    private void ApplyTransparencySettings()
    {
        if (!separateImageTextAlpha) return;

        // 对图片组件应用透明度
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img != null)
            {
                Color color = img.color;
                color.a = imageAlpha;
                img.color = color;
            }
        }

        // 对文字组件应用透明度
        if (hintText != null)
        {
            Color textColor = hintText.color;
            textColor.a = textAlpha;
            hintText.color = textColor;
        }

        // 对所有Text组件应用透明度
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text != null)
            {
                Color color = text.color;
                color.a = textAlpha;
                text.color = color;
            }
        }

        // 对所有TMP_Text组件应用透明度
        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text tmpText in tmpTexts)
        {
            if (tmpText != null)
            {
                Color color = tmpText.color;
                color.a = textAlpha;
                tmpText.color = color;
            }
        }
    }

    private void BuildDisableLists(GameObject playerGO, GameObject cameraGO)
    {
        _disabledPlayerScripts.Clear();
        _disabledCameraScripts.Clear();

        if (playerGO)
        {
            foreach (var mb in playerGO.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!mb) continue;
                if (!mb.enabled) continue;

                // 不要禁用 PlayerHealth 自己；也不要把本 UI 自己禁了
                if (mb is PlayerHealth) continue;
                if (mb == this) continue;

                _disabledPlayerScripts.Add(mb);
            }
        }

        if (cameraGO)
        {
            foreach (var mb in cameraGO.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!mb) continue;
                if (!mb.enabled) continue;
                _disabledCameraScripts.Add(mb);
            }
        }
    }

    private static void SetScriptsEnabled(List<MonoBehaviour> list, bool enabled)
    {
        // 逐一开关，遇到可能已销毁/被移除的脚本也安全
        for (int i = 0; i < list.Count; i++)
        {
            var mb = list[i];
            if (!mb) continue;
            mb.enabled = enabled;
        }
    }
}
