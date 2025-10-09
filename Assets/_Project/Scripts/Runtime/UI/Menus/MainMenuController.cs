using UnityEngine;
using UnityEngine.SceneManagement;

namespace Antventure.UI.Menus
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private string  firstLevelSceneName = "Level1";
        [SerializeField] private string tutorialSceneName = "Tutorial";

        [Header("Audio Settings")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip hoverSound;
        
        [Header("Settings")]
        [SerializeField] private GameSettingsManager gameSettingsManager;

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
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        // Play Button Clicks
        public void OnPlayClicked()
        {
            PlayClickSound();
            SceneManager.LoadScene(firstLevelSceneName);
        }

        // Tutorial Button Clicks
        public void OnTutorialClicked()
        {
            PlayClickSound();
            SceneManager.LoadScene(tutorialSceneName);
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