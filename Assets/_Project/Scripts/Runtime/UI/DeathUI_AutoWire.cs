using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeathUI_AutoWire : MonoBehaviour
{
    [Header("Refs (optional – will auto-find)")]
    [SerializeField] private PlayerHealth playerHealth; // 可为空：自动找
    [SerializeField] private GameObject panel;          // 指向你的 final_death 图片所在的 GameObject（或其父容器）
    [SerializeField] private TMP_Text hintText;         // 可为空
    [TextArea] [SerializeField] private string hint = "Left click to respawn";

    [Header("Cursor (optional)")]
    [SerializeField] private bool unlockCursorOnDeath = true;

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

    private void Awake()
    {
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
            playerHealth.Respawn();
        }
    }

    // =================== Event Handlers ===================

    private void HandleDied()
    {
        if (hintText) hintText.text = hint;
        ShowPanel(true);
    }

    private void HandleRespawned()
    {
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

        if (show)
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
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
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
