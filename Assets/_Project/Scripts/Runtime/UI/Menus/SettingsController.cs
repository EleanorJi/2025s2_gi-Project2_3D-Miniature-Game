using UnityEngine;
using UnityEngine.UI;
using Antventure.Systems.Audio;

namespace Antventure.UI.Menus
{
    public class SettingsController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider soundEffectVolumeSlider;
        [SerializeField] private Button closeButton;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioClip buttonClickSound;
        
        private void Start()
        {
            // Initialize UI
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            
            // Setup event listeners
            SetupEventListeners();
            
            // Load saved settings
            LoadSettings();
        }
        
        private void SetupEventListeners()
        {
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
            if (soundEffectVolumeSlider != null)
                soundEffectVolumeSlider.onValueChanged.AddListener(OnSoundEffectVolumeChanged);
            
            // Ensure close button is properly connected
            if (closeButton != null)
            {
                // Clear any existing listeners first
                closeButton.onClick.RemoveAllListeners();
                // Add our close function
                closeButton.onClick.AddListener(CloseSettings);
                Debug.Log("Close button listener added successfully!");
            }
            else
            {
                Debug.LogWarning("Close button is not assigned in SettingsController!");
            }
        }
        
        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                PlayButtonSound();
                Debug.Log("Settings panel opened");
            }
        }
        
        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                PlayButtonSound();
                Debug.Log("Settings panel closed");
            }
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.MusicVolume = value;
                AudioManager.Instance.SaveAudioSettings();
            }
        }
        
        private void OnSoundEffectVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SoundEffectVolume = value;
                AudioManager.Instance.SaveAudioSettings();
            }
        }
        
        private void LoadSettings()
        {
            if (AudioManager.Instance != null)
            {
                // Load music volume
                if (musicVolumeSlider != null)
                    musicVolumeSlider.value = AudioManager.Instance.MusicVolume;
                
                // Load sound effect volume
                if (soundEffectVolumeSlider != null)
                    soundEffectVolumeSlider.value = AudioManager.Instance.SoundEffectVolume;
            }
        }
        
        private void PlayButtonSound()
        {
            if (AudioManager.Instance != null && buttonClickSound != null)
            {
                AudioManager.Instance.PlaySoundEffect(buttonClickSound);
            }
        }
        
        // Public method for testing - you can call this from Inspector
        public void TestCloseButton()
        {
            Debug.Log("Test close button clicked!");
            CloseSettings();
        }
    }
}