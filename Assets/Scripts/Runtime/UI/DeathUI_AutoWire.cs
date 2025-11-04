using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Antventure.UI;

public class DeathUI_AutoWire : MonoBehaviour
{
    [Header("Refs (optional – will auto-find)")]
    [SerializeField] private PlayerHealth playerHealth; 
    [SerializeField] private GameObject panel;          
    [SerializeField] private TMP_Text hintText;         // Optional hint text
    [TextArea][SerializeField] private string hint = "Left click to respawn";

    [Header("Cursor (optional)")]
    [SerializeField] private bool unlockCursorOnDeath = true;

    [Header("Background Transparency")]
    [SerializeField][Range(0f, 1f)] private float backgroundAlpha = 0.8f; // Background transparency, 0=fully transparent, 1=fully opaque
    [SerializeField][Range(0f, 1f)] private float imageAlpha = 1f;        // Death image transparency
    [SerializeField][Range(0f, 1f)] private float textAlpha = 1f;         // Hint text transparency
    [SerializeField] private bool separateImageTextAlpha = false;         // Whether to control image and text transparency separately

    [Header("Global Controller Integration")]
    [SerializeField] private bool useGlobalController = true;
    [SerializeField] private Sprite deathSprite; // Optional death image for global controller

    // === Audio ===
    [Header("Audio (optional)")]
    [SerializeField] private AudioClip deathSfx;          // Played when death UI is shown
    [SerializeField] private AudioClip respawnSfx;        // Played on respawn (optional)
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 0.9f; // Global volume for both clips

    private AudioSource _audioSource; // Local SFX source for this UI

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
        // Cache cookies for respawn
        cookie = CookiesInventory.Instance ? CookiesInventory.Instance.cookies : 0;

        // Auto-wire PlayerHealth by tag or by type
        FindAndWirePlayerHealth();

        // Auto-wire panel if not assigned
        if (!panel) panel = gameObject;

        // Setup local AudioSource (2D UI-style sound)
        _audioSource = GetComponent<AudioSource>();
        if (!_audioSource) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f; // 2D sound (not affected by position)
        _audioSource.volume = sfxVolume;

        // Initially hide
        SetCanvasGroupVisible(false);
        if (panel && panel.activeSelf) panel.SetActive(false);
    }
    
    /// <summary>
    /// 查找并连接到PlayerHealth组件
    /// </summary>
    private void FindAndWirePlayerHealth()
    {
        if (!playerHealth)
        {
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
        
        Debug.Log($"[DeathUI] Found PlayerHealth on '{playerHealth.gameObject.name}'");
    }

    private void Start()
    {
        // Recheck the PlayerHealth connection in Start to ensure that it is properly connected after the scene loading is complete.
        if (!playerHealth)
        {
            Debug.LogWarning("[DeathUI] PlayerHealth not found in Awake, retrying in Start...");
            FindAndWirePlayerHealth();
        }
    }

    private void OnEnable()
    {
        // If the PlayerHealth reference is missing, try to re-search for it.
        if (!playerHealth)
        {
            Debug.LogWarning("[DeathUI] PlayerHealth reference lost, attempting to reconnect...");
            FindAndWirePlayerHealth();
        }
        
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
        if (_isShown && Input.GetMouseButtonDown(0) && playerHealth != null)
        {

            if (CookiesInventory.Instance != null)
            {
                CookiesInventory.Instance.cookies = cookie;
                cookie = CookiesInventory.Instance.cookies;

                if (Level3CookieUI.Instance != null)
                    Level3CookieUI.Instance.Refresh(cookie);
            }


            playerHealth.Respawn();
        }
    }

    // =================== Event Handlers ===================

    private void HandleDied()
    {
        // 1) At the moment of death, all minions are instantly wiped out.
        ClearAllMinions();

        // 2) Play the death sound effect
        PlaySfx(deathSfx);

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isLevel3Boss = currentSceneName == "Level3_boss";
        
        if (useGlobalController && GlobalDeathUIController.Instance != null && !isLevel3Boss)
        {
            try
            {
                GlobalDeathUIController.Instance.ShowDeathUI(hint, deathSprite, 4f);
                HandleGamePause(true);
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DeathUI] Failed to use GlobalDeathUIController: {ex.Message}. Falling back to local UI.");
                // Fall through to use local UI as backup
            }
        }

        if (hintText) hintText.text = hint;
        ShowPanel(true);
        ApplyTransparencySettings();
    }

    private void HandleRespawned()
    {
        // Optional: play a respawn sound when reviving
        PlaySfx(respawnSfx);

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isLevel3Boss = currentSceneName == "Level3_boss";
        
        if (useGlobalController && GlobalDeathUIController.Instance != null && !isLevel3Boss)
        {
            try
            {
                GlobalDeathUIController.Instance.HideDeathUI();
                HandleGamePause(false);
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DeathUI] Failed to hide GlobalDeathUIController: {ex.Message}. Using local UI.");
                // Fall through to use local UI as backup
            }
        }

        ShowPanel(false);
    }

    // =================== UI control & gameplay gating ===================

    private void ShowPanel(bool show)
    {
        _isShown = show;

        if (panel)
        {
            if (ReferenceEquals(panel, gameObject))
                SetCanvasGroupVisible(show);
            else
                panel.SetActive(show);
        }

        HandleGamePause(show);
    }

    private void HandleGamePause(bool pause)
    {
        if (pause)
        {
            Time.timeScale = 0f;
            if (unlockCursorOnDeath)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            BuildDisableLists(playerHealth.gameObject, Camera.main ? Camera.main.gameObject : null);
            SetScriptsEnabled(_disabledPlayerScripts, false);
            SetScriptsEnabled(_disabledCameraScripts, false);
        }
        else
        {
            Time.timeScale = 1f;

            SetScriptsEnabled(_disabledPlayerScripts, true);
            SetScriptsEnabled(_disabledCameraScripts, true);
            _disabledPlayerScripts.Clear();
            _disabledCameraScripts.Clear();

            if (unlockCursorOnDeath)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void SetCanvasGroupVisible(bool visible)
    {
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = visible ? backgroundAlpha : 0f; // Use configurable background transparency
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    private void ApplyTransparencySettings()
    {
        if (!separateImageTextAlpha) return;

        // Images
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

        // UI Text (legacy)
        if (hintText != null)
        {
            Color textColor = hintText.color;
            textColor.a = textAlpha;
            hintText.color = textColor;
        }

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

        // TMP Texts
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
        for (int i = 0; i < list.Count; i++)
        {
            var mb = list[i];
            if (!mb) continue;
            mb.enabled = enabled;
        }
    }

    // ========= Audio helper =========
    /// <summary>
    /// Plays a one-shot SFX using the local AudioSource.
    /// Safe to call with null clips.
    /// </summary>
    private void PlaySfx(AudioClip clip)
    {
        if (!clip) return;
        if (!_audioSource)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
        }

        _audioSource.volume = sfxVolume;
        _audioSource.PlayOneShot(clip);
    }

    // ========= Minion clear =========
    private void ClearAllMinions()
    {
        var minions = FindObjectsOfType<MinionAnchor>();
        foreach (var m in minions)
        {
            if (m != null)
            {
                Destroy(m.gameObject);
            }
        }
    }
}
