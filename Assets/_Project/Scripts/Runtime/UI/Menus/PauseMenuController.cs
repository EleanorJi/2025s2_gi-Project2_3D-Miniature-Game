using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Antventure.UI.Menus
{
    /// <summary>
    /// Pause Menu Controller
    /// Manages game pause state and related UI interactions
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("Audio")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip pauseSound;
        [SerializeField] private AudioClip resumeSound;

        // Pause state
        private bool isPaused = false;
        
        // Main menu scene name
        private const string MAIN_MENU_SCENE = "HomeScene";

        private void Start()
        {
            SetupEventListeners();
            
            // Ensure pause menu is initially hidden
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Listen for ESC key to toggle pause state
            if (Input.GetKeyDown(KeyCode.Escape))
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
        }

        /// <summary>
        /// Setup event listeners
        /// </summary>
        private void SetupEventListeners()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            
            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptionsClicked);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        #region Public Methods

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (isPaused) return;

            isPaused = true;
            Time.timeScale = 0f; // Pause game time
            
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }

            // Play pause sound effect
            PlaySound(pauseSound);

            // Set cursor state
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("Game Paused");
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = 1f; // Resume game time
            
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            // Play resume sound effect
            PlaySound(resumeSound);

            // Restore cursor state (adjust according to game needs)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("Game Resumed");
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
        /// Get current pause state
        /// </summary>
        public bool IsPaused => isPaused;

        #endregion

        #region Button Event Handlers

        /// <summary>
        /// Resume game button click event
        /// </summary>
        public void OnResumeClicked()
        {
            PlayButtonClickSound();
            ResumeGame();
        }

        /// <summary>
        /// Options button click event
        /// </summary>
        public void OnOptionsClicked()
        {
            PlayButtonClickSound();
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOptionsMenu();
            }
            else
            {
                Debug.LogWarning("UIManager instance not found!");
            }
        }

        /// <summary>
        /// Main menu button click event
        /// </summary>
        public void OnMainMenuClicked()
        {
            PlayButtonClickSound();
            
            // Restore time scale
            Time.timeScale = 1f;
            
            // Show loading screen and load main menu scene
            if (UIManager.Instance != null)
            {
                UIManager.Instance.LoadSceneAsync(MAIN_MENU_SCENE);
            }
            else
            {
                // Fallback: load scene directly
                SceneManager.LoadScene(MAIN_MENU_SCENE);
            }
        }

        /// <summary>
        /// Quit game button click event
        /// </summary>
        public void OnQuitClicked()
        {
            PlayButtonClickSound();
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.QuitGame();
            }
            else
            {
                // Fallback: quit directly
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
        }

        #endregion

        #region Audio Methods

        /// <summary>
        /// Play button click sound effect
        /// </summary>
        private void PlayButtonClickSound()
        {
            PlaySound(buttonClickSound);
        }

        /// <summary>
        /// Play sound effect
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
            {
                uiAudioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // Ensure game time returns to normal
            Time.timeScale = 1f;
            
            // Remove event listeners
            if (resumeButton != null)
                resumeButton.onClick.RemoveAllListeners();
            
            if (optionsButton != null)
                optionsButton.onClick.RemoveAllListeners();
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveAllListeners();
            
            if (quitButton != null)
                quitButton.onClick.RemoveAllListeners();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Automatically pause game when application loses focus (optional feature)
            if (!hasFocus && !isPaused)
            {
                PauseGame();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Automatically pause game when application is paused (mobile platforms)
            if (pauseStatus && !isPaused)
            {
                PauseGame();
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Static method: Find and pause game
        /// </summary>
        public static void PauseGameStatic()
        {
            PauseMenuController pauseController = FindObjectOfType<PauseMenuController>();
            if (pauseController != null)
            {
                pauseController.PauseGame();
            }
        }

        /// <summary>
        /// Static method: Find and resume game
        /// </summary>
        public static void ResumeGameStatic()
        {
            PauseMenuController pauseController = FindObjectOfType<PauseMenuController>();
            if (pauseController != null)
            {
                pauseController.ResumeGame();
            }
        }

        #endregion
    }
}