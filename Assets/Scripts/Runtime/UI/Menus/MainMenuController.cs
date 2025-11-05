using UnityEngine;
using UnityEngine.SceneManagement;

namespace Antventure.UI.Menus
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private string  firstLevelSceneName = "Level1";
        [SerializeField] private string secondLevelSceneName = "Level1-2";
        [SerializeField] private string tutorialSceneName = "Tutorial";

        [Header("Audio Settings")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip hoverSound;
        
        [Header("Settings")]
        [SerializeField] private GameSettingsManager gameSettingsManager;
        
        [Header("Team Panel")]
        [SerializeField] private TeamPanelController teamPanelController;

        private void Start()
        {
            // Use CursorManager if available, otherwise fallback to direct cursor control
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetDefaultCursor();
            }
            else
            {
                // Fallback: Show system default cursor in main menu
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(null, Vector2.zero, GetCursorMode());
            }
        }

        /// <summary>
        /// Get appropriate cursor mode based on platform
        /// ForceSoftware for WebGL to avoid DPI scaling issues
        /// </summary>
        private CursorMode GetCursorMode()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            return CursorMode.ForceSoftware;
            #else
            return CursorMode.Auto;
            #endif
        }

        // Play Button Clicks
        public void OnPlayClicked()
        {
            PlayClickSound();
            SceneOrderManager.Instance.LoadNextScene(); // Start from the main menu and make sequential jumps.
        }

        // Team Button Clicks (formerly Tutorial Button)
        public void OnTeamClicked()
        {
            PlayClickSound();
            if (teamPanelController != null)
            {
                teamPanelController.OpenTeamPanel();
            }
            else
            {
                Debug.LogWarning("Team Panel Controller not assigned!");
            }
        }

        public void OnOptionsClicked()
        {
            PlayClickSound();
            if (gameSettingsManager != null)
            {
                gameSettingsManager.OpenSettings();
            }
            else
            {
                Debug.LogWarning("Game Settings Manager not assigned!");
            }
        }

        // Hover Sound
        public void OnHover()
        {
            if (uiAudioSource != null && hoverSound != null)
            {
                uiAudioSource.PlayOneShot(hoverSound);
            }
        }

        // Play Click Sound
        private void PlayClickSound()
        {
            if (uiAudioSource != null && clickSound != null)
            {
                uiAudioSource.PlayOneShot(clickSound);
            }
        }
    }
}