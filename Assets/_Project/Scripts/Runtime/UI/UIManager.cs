using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Antventure.UI.Menus;
using Antventure.UI.HUD;

namespace Antventure.UI
{
    /// <summary>
    /// Global UI Manager
    /// Uses singleton pattern to manage all UI-related functionality
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject loadingScreen;

        [Header("提示文字")]
        [SerializeField] private GameObject hintTextPanel; // 新增：提示文字面板
        [SerializeField] private Text hintText; // 新增：提示文字组件

        [TextArea(3, 6)]
        [SerializeField] private string stoveSaveHintText = "Stove now is save";

        [Header("UI Controllers")]
        [SerializeField] private GameSettingsManager gameSettingsManager;
        [SerializeField] private PauseMenuController pauseMenuController;
        [SerializeField] private GameHUDController gameHUDController;

        [Header("Audio")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip menuOpenSound;
        [SerializeField] private AudioClip menuCloseSound;

        // UI state
        public bool IsOptionsMenuOpen { get; private set; }
        public bool IsPauseMenuOpen { get; private set; }
        public bool IsLoadingScreenActive { get; private set; }

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Initialize UI state
            InitializeUI();
        }

        private void Start()
        {
            // Ensure all panels are initially closed
            CloseAllPanels();
        }

        private void Update()
        {
            // Handle ESC key input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeKey();
            }
        }

        /// <summary>
        /// Initialize UI system
        /// </summary>
        private void InitializeUI()
        {
            // If no AudioSource exists, try to get or create one
            if (uiAudioSource == null)
            {
                uiAudioSource = GetComponent<AudioSource>();
                if (uiAudioSource == null)
                {
                    uiAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Configure AudioSource
            uiAudioSource.playOnAwake = false;
            uiAudioSource.volume = 0.7f;
        }

        /// <summary>
        /// Handle ESC key logic
        /// </summary>
        private void HandleEscapeKey()
        {
            // If options menu is open, close options menu
            if (IsOptionsMenuOpen)
            {
                CloseOptionsMenu();
            }
            // If pause menu is open, close pause menu
            else if (IsPauseMenuOpen)
            {
                ClosePauseMenu();
            }
            // If in game scene, open pause menu
            else if (IsInGameScene())
            {
                OpenPauseMenu();
            }
        }

        /// <summary>
        /// Check if currently in a game scene
        /// </summary>
        private bool IsInGameScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            return currentScene == "Level1" || currentScene == "Level1-2" || currentScene == "Tutorial";
        }
        
        /// <summary>
        /// 显示提示文字
        /// </summary>
        public void ShowHintText(string text)
        {
            if (hintTextPanel != null && hintText != null)
            {
                hintText.text = text;
                hintTextPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 隐藏提示文字
        /// </summary>
        public void HideHintText()
        {
            if (hintTextPanel != null)
            {
                hintTextPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 显示灶台安全提示
        /// </summary>
        public void ShowStoveSafeHint()
        {
            ShowHintText(stoveSaveHintText);
            
            // 5秒后自动隐藏提示
            StartCoroutine(HideHintAfterDelay(5f));
        }

        private IEnumerator HideHintAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideHintText();
        }

        #region Options Menu Methods

        /// <summary>
        /// Open options menu
        /// </summary>
        public void OpenOptionsMenu()
        {
            if (optionsPanel != null && !IsOptionsMenuOpen)
            {
                optionsPanel.SetActive(true);
                IsOptionsMenuOpen = true;
                PlayMenuSound(menuOpenSound);

                // If in game, pause the game
                if (IsInGameScene())
                {
                    Time.timeScale = 0f;
                }
            }

            if (gameSettingsManager != null)
            {
                // Additional initialization logic can be added here
            }
        }

        /// <summary>
        /// Close options menu
        /// </summary>
        public void CloseOptionsMenu()
        {
            if (optionsPanel != null && IsOptionsMenuOpen)
            {
                optionsPanel.SetActive(false);
                IsOptionsMenuOpen = false;
                PlayMenuSound(menuCloseSound);
                
                // If in game and no other menus are open, resume game
                if (IsInGameScene() && !IsPauseMenuOpen)
                {
                    Time.timeScale = 1f;
                }
            }
        }

        #endregion

        #region Pause Menu Methods

        /// <summary>
        /// Open pause menu
        /// </summary>
        public void OpenPauseMenu()
        {
            if (pausePanel != null && !IsPauseMenuOpen && IsInGameScene())
            {
                pausePanel.SetActive(true);
                IsPauseMenuOpen = true;
                Time.timeScale = 0f;
                PlayMenuSound(menuOpenSound);
            }
            
            if (pauseMenuController != null)
            {
                pauseMenuController.PauseGame();
            }
        }

        /// <summary>
        /// Close pause menu
        /// </summary>
        public void ClosePauseMenu()
        {
            if (pausePanel != null && IsPauseMenuOpen)
            {
                pausePanel.SetActive(false);
                IsPauseMenuOpen = false;
                PlayMenuSound(menuCloseSound);
                
                // If options menu is not open, resume game
                if (!IsOptionsMenuOpen)
                {
                    Time.timeScale = 1f;
                }
            }
            
            if (pauseMenuController != null)
            {
                pauseMenuController.ResumeGame();
            }
        }

        /// <summary>
        /// Get game HUD controller
        /// </summary>
        public GameHUDController GetGameHUD()
        {
            return gameHUDController;
        }

        /// <summary>
        /// Resume game
        /// </summary>
        public void ResumeGame()
        {
            ClosePauseMenu();
        }

        /// <summary>
        /// Restart current level
        /// </summary>
        public void RestartLevel()
        {
            Time.timeScale = 1f;
            string currentScene = SceneManager.GetActiveScene().name;
            LoadScene(currentScene);
        }

        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            LoadScene("StartScene");
        }

        #endregion

        #region Loading Screen Methods

        /// <summary>
        /// Show loading screen
        /// </summary>
        public void ShowLoadingScreen()
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
                IsLoadingScreenActive = true;
            }
        }

        /// <summary>
        /// Hide loading screen
        /// </summary>
        public void HideLoadingScreen()
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
                IsLoadingScreenActive = false;
            }
        }

        #endregion

        #region Scene Management

        /// <summary>
        /// Load scene (with loading screen)
        /// </summary>
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// Load scene asynchronously
        /// </summary>
        private System.Collections.IEnumerator LoadSceneAsync(string sceneName)
        {
            ShowLoadingScreen();
            
            // Wait one frame to ensure loading screen is displayed
            yield return null;
            
            // Start async scene loading
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            // Wait for scene loading to complete
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            // Brief delay to ensure scene is fully initialized
            yield return new WaitForSeconds(0.5f);
            
            HideLoadingScreen();
        }

        #endregion

        #region Audio Methods

        /// <summary>
        /// Play button click sound effect
        /// </summary>
        public void PlayButtonClickSound()
        {
            PlayUISound(buttonClickSound);
        }

        /// <summary>
        /// Play button hover sound effect
        /// </summary>
        public void PlayButtonHoverSound()
        {
            PlayUISound(buttonHoverSound);
        }

        /// <summary>
        /// Play UI sound effect
        /// </summary>
        private void PlayUISound(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
            {
                uiAudioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Play menu sound effect
        /// </summary>
        private void PlayMenuSound(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
            {
                uiAudioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Close all UI panels
        /// </summary>
        private void CloseAllPanels()
        {
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
            
            if (pausePanel != null)
                pausePanel.SetActive(false);
            
            if (loadingScreen != null)
                loadingScreen.SetActive(false);
            if (hintTextPanel != null) // 新增
                hintTextPanel.SetActive(false);

            IsOptionsMenuOpen = false;
            IsPauseMenuOpen = false;
            IsLoadingScreenActive = false;
        }

        /// <summary>
        /// Quit game
        /// </summary>
        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        #endregion

        private void OnDestroy()
        {
            // Ensure time scale returns to normal
            Time.timeScale = 1f;
        }
    }
}