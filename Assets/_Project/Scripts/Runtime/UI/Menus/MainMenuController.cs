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
            Debug.Log("Options menu opened.");
            // TODO: Open Options menu
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