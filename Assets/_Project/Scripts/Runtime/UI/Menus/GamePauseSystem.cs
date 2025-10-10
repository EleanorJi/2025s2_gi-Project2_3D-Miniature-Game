using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using Antventure.Systems.Audio;

namespace Antventure.UI.Menus
{
    /// <summary>
    /// Game Pause System - Complete pause functionality with proper cursor management
    /// Handles all pause-related functionality including cursor visibility and UI interactions
    /// </summary>
    public class GamePauseSystem : MonoBehaviour
    {
        public static GamePauseSystem Instance { get; private set; }
        
        [Header("Pause UI")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Canvas pauseCanvas;
        
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button exitButton;
        
        [Header("Audio Controls")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text musicVolumeText;
        [SerializeField] private Text sfxVolumeText;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip pauseSound;
        [SerializeField] private AudioClip resumeSound;
        
        [Header("Cursor Settings")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D hoverCursor;
        [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
        
        // State management
        private bool isPaused = false;
        private bool wasGameplayScene = false;
        
        // Constants
        private const string MAIN_MENU_SCENE = "StartScene";
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePauseSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            CreatePauseUI();
        }
        
        private void Update()
        {
            // Handle ESC key only in gameplay scenes
            if (IsGameplayScene() && Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
            
            // Force cleanup pause panel in menu scenes
            if (!IsGameplayScene() && pausePanel != null && pausePanel.activeInHierarchy)
            {
                Debug.LogWarning("[PAUSE SYSTEM] Pause panel active in menu scene, forcing cleanup");
                pausePanel.SetActive(false);
                isPaused = false;
                Time.timeScale = 1f;
                
                if (UnifiedCursorManager.Instance != null)
                {
                    UnifiedCursorManager.Instance.ForceShowCursor();
                }
            }
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Time.timeScale = 1f;
        }
        
        /// <summary>
        /// Initialize the pause system
        /// </summary>
        private void InitializePauseSystem()
        {
            // Load cursor textures if not assigned
            if (defaultCursor == null)
            {
                // Use system default cursor
            }
            
            if (hoverCursor == null)
            {
                // Use system default cursor for hover too
            }
        }
        
        /// <summary>
        /// Create the pause UI
        /// </summary>
        private void CreatePauseUI()
        {
            // Create canvas
            GameObject canvasGO = new GameObject("PauseCanvas");
            canvasGO.transform.SetParent(transform);
            
            pauseCanvas = canvasGO.AddComponent<Canvas>();
            pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseCanvas.sortingOrder = 1000;
            pauseCanvas.overrideSorting = true;
            
            // Add canvas scaler
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Add graphic raycaster
            GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();
            
            // Ensure EventSystem exists
            EnsureEventSystem();
            
            // Create pause panel
            CreatePausePanel();
            
            // Setup initial state
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Ensure EventSystem exists for UI interaction
        /// </summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(eventSystemGO);
            }
        }
        
        /// <summary>
        /// Create the pause panel and all UI elements
        /// </summary>
        private void CreatePausePanel()
        {
            // Main panel background
            GameObject panel = new GameObject("PausePanel");
            panel.transform.SetParent(pauseCanvas.transform, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // Semi-transparent background
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.8f);
            
            // Content container
            GameObject content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(600, 500);
            
            // Content background
            Image contentBg = content.AddComponent<Image>();
            contentBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            
            pausePanel = panel;
            
            // Create UI elements
            CreateUIElements(content);
            SetupEventListeners();
        }
        
        /// <summary>
        /// Create all UI elements
        /// </summary>
        private void CreateUIElements(GameObject parent)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            // Title
            CreateText(parent, "GAME PAUSED", new Vector2(0, 180), new Vector2(400, 60), 32, defaultFont, Color.white);
            
            // Music Volume Section
            CreateText(parent, "Music Volume", new Vector2(-150, 100), new Vector2(200, 30), 18, defaultFont, Color.white);
            musicVolumeSlider = CreateSlider(parent, new Vector2(50, 100), new Vector2(200, 30));
            musicVolumeText = CreateText(parent, "70%", new Vector2(180, 100), new Vector2(80, 30), 16, defaultFont, Color.white);
            
            // SFX Volume Section
            CreateText(parent, "Sound Effects Volume", new Vector2(-150, 50), new Vector2(200, 30), 18, defaultFont, Color.white);
            sfxVolumeSlider = CreateSlider(parent, new Vector2(50, 50), new Vector2(200, 30));
            sfxVolumeText = CreateText(parent, "70%", new Vector2(180, 50), new Vector2(80, 30), 16, defaultFont, Color.white);
            
            // Buttons
            resumeButton = CreateButton(parent, "Resume Game", new Vector2(-100, -50), new Vector2(180, 50), defaultFont);
            exitButton = CreateButton(parent, "Exit to Menu", new Vector2(100, -50), new Vector2(180, 50), defaultFont);
            
            // Add hover effects to buttons
            AddButtonHoverEffect(resumeButton);
            AddButtonHoverEffect(exitButton);
        }
        
        /// <summary>
        /// Create a text element
        /// </summary>
        private Text CreateText(GameObject parent, string text, Vector2 position, Vector2 size, int fontSize, Font font, Color color)
        {
            GameObject textGO = new GameObject("Text_" + text.Replace(" ", ""));
            textGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Text textComp = textGO.AddComponent<Text>();
            textComp.text = text;
            textComp.font = font;
            textComp.fontSize = fontSize;
            textComp.color = color;
            textComp.alignment = TextAnchor.MiddleCenter;
            
            return textComp;
        }
        
        /// <summary>
        /// Create a slider element
        /// </summary>
        private Slider CreateSlider(GameObject parent, Vector2 position, Vector2 size)
        {
            GameObject sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = sliderGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.7f;
            
            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderGO.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGO.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f, 1f);
            
            // Handle Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGO.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;
            
            // Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(20, 20);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            
            // Assign references
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            
            return slider;
        }
        
        /// <summary>
        /// Create a button element
        /// </summary>
        private Button CreateButton(GameObject parent, string text, Vector2 position, Vector2 size, Font font)
        {
            GameObject buttonGO = new GameObject("Button_" + text.Replace(" ", ""));
            buttonGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            
            // Button text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text textComp = textGO.AddComponent<Text>();
            textComp.text = text;
            textComp.font = font;
            textComp.fontSize = 18;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            
            return button;
        }
        
        /// <summary>
        /// Add hover effect to button
        /// </summary>
        private void AddButtonHoverEffect(Button button)
        {
            ButtonHoverHandler hoverHandler = button.gameObject.AddComponent<ButtonHoverHandler>();
            hoverHandler.Initialize(this);
        }
        
        /// <summary>
        /// Setup event listeners
        /// </summary>
        private void SetupEventListeners()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(ResumeGame);
            }
            
            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(ExitToMenu);
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }
        }
        
        /// <summary>
        /// Handle scene loaded event
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Force cleanup if we're in a paused state
            if (isPaused)
            {
                ResumeGame();
            }
            
            // Clean up pause panel if we're entering a menu scene
            if (!IsGameplayScene())
            {
                if (pausePanel != null)
                {
                    pausePanel.SetActive(false);
                    Destroy(pausePanel);
                    pausePanel = null;
                }
                ShowMenuCursor();
            }
            else
            {
                // In gameplay scene, ensure pause panel is created
                if (pausePanel == null)
                {
                    CreatePauseUI();
                }
                HideGameplayCursor();
            }
        }
        
        /// <summary>
        /// Check if current scene is a gameplay scene
        /// </summary>
        private bool IsGameplayScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return sceneName != "StartScene" && sceneName != "HomeScene";
        }
        
        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
        
        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (isPaused || !IsGameplayScene()) return;
            
            isPaused = true;
            Time.timeScale = 0f;
            
            // Show pause panel
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
            
            // Show cursor for UI interaction
            ShowPauseCursor();
            
            // Load current audio settings
            LoadAudioSettings();
            
            // Play pause sound
            PlaySound(pauseSound);
            
            Debug.Log("Game Paused");
        }
        
        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (!isPaused) return;
            
            isPaused = false;
            Time.timeScale = 1f;
            
            // Hide pause panel
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
            
            // Save audio settings
            SaveAudioSettings();
            
            // Hide cursor for gameplay
            HideGameplayCursor();
            
            // Play resume sound
            PlaySound(resumeSound);
            
            Debug.Log("Game Resumed");
        }
        
        /// <summary>
        /// Exit to main menu
        /// </summary>
        public void ExitToMenu()
        {
            PlaySound(buttonClickSound);
            
            // Properly hide pause menu first
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
            
            // Reset cursor state
            HideGameplayCursor();
            
            // Reset game state
            isPaused = false;
            Time.timeScale = 1f;
            
            // Save settings before leaving
            SaveAudioSettings();
            
            // Clean up pause system for menu scene
            CleanupForSceneTransition();
            
            // Load main menu
            SceneManager.LoadScene(MAIN_MENU_SCENE);
        }
        
        /// <summary>
        /// Clean up pause system before scene transition
        /// </summary>
        private void CleanupForSceneTransition()
        {
            // Destroy pause panel if it exists
            if (pausePanel != null)
            {
                Destroy(pausePanel);
                pausePanel = null;
            }
            
            // Reset all states
            isPaused = false;
            
            // Ensure cursor is in correct state for menu
            if (UnifiedCursorManager.Instance != null)
            {
                UnifiedCursorManager.Instance.ForceShowCursor();
            }
            else
            {
                ShowMenuCursor();
            }
        }
        
        /// <summary>
        /// Show cursor for pause menu interaction
        /// </summary>
        private void ShowPauseCursor()
        {
            if (UnifiedCursorManager.Instance != null)
            {
                UnifiedCursorManager.Instance.ShowPauseMenuCursor();
            }
            else
            {
                // Fallback
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        
        /// <summary>
        /// Hide cursor for gameplay
        /// </summary>
        private void HideGameplayCursor()
        {
            if (UnifiedCursorManager.Instance != null)
            {
                UnifiedCursorManager.Instance.HidePauseMenuCursor();
            }
            else
            {
                // Fallback
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        
        /// <summary>
        /// Show cursor for menu scenes
        /// </summary>
        private void ShowMenuCursor()
        {
            if (UnifiedCursorManager.Instance != null)
            {
                UnifiedCursorManager.Instance.ForceShowCursor();
            }
            else
            {
                // Fallback
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        
        /// <summary>
        /// Set hover cursor
        /// </summary>
        public void SetHoverCursor()
        {
            if (UnifiedCursorManager.Instance != null)
            {
                UnifiedCursorManager.Instance.SetHoverCursor();
            }
        }
        
        /// <summary>
        /// Set default cursor
        /// </summary>
        public void SetDefaultCursor()
        {
            if (UnifiedCursorManager.Instance != null)
            {
                UnifiedCursorManager.Instance.SetDefaultCursor();
            }
        }
        
        /// <summary>
        /// Handle music volume changes
        /// </summary>
        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.MusicVolume = value;
            }
            
            if (musicVolumeText != null)
            {
                musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }
        
        /// <summary>
        /// Handle SFX volume changes
        /// </summary>
        private void OnSfxVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SoundEffectVolume = value;
            }
            
            if (sfxVolumeText != null)
            {
                sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }
        
        /// <summary>
        /// Load audio settings from AudioManager
        /// </summary>
        private void LoadAudioSettings()
        {
            if (AudioManager.Instance != null)
            {
                if (musicVolumeSlider != null)
                {
                    musicVolumeSlider.value = AudioManager.Instance.MusicVolume;
                }
                
                if (sfxVolumeSlider != null)
                {
                    sfxVolumeSlider.value = AudioManager.Instance.SoundEffectVolume;
                }
            }
        }
        
        /// <summary>
        /// Save audio settings
        /// </summary>
        private void SaveAudioSettings()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SaveAudioSettings();
            }
        }
        
        /// <summary>
        /// Play sound effect
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (AudioManager.Instance != null && clip != null)
            {
                AudioManager.Instance.PlaySoundEffect(clip);
            }
        }
        
        /// <summary>
        /// Get pause state
        /// </summary>
        public bool IsPaused => isPaused;
    }
    
    /// <summary>
    /// Button Hover Handler - Manages cursor changes on button hover
    /// </summary>
    public class ButtonHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private GamePauseSystem pauseSystem;
        
        public void Initialize(GamePauseSystem system)
        {
            pauseSystem = system;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (pauseSystem != null)
            {
                pauseSystem.SetHoverCursor();
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (pauseSystem != null)
            {
                pauseSystem.SetDefaultCursor();
            }
        }
    }
}
