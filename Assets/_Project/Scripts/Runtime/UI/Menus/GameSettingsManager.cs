using UnityEngine;
using UnityEngine.UI;
using Antventure.Systems.Audio;

namespace Antventure.UI.Menus
{
    /// <summary>
    /// Simple Game Settings Manager
    /// Handles all game settings including audio, graphics, and controls
    /// </summary>
    public class GameSettingsManager : MonoBehaviour
    {
        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button openSettingsButton;
        [SerializeField] private Button closeSettingsButton;
        
        [Header("Audio Settings")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider soundEffectVolumeSlider;
        [SerializeField] private Text musicVolumeText;
        [SerializeField] private Text soundEffectVolumeText;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip buttonHoverSound;
        
        // Default values
        private const float DEFAULT_MUSIC_VOLUME = 0.7f;
        private const float DEFAULT_SFX_VOLUME = 0.7f;
        
        private void Start()
        {
            InitializeSettings();
            SetupEventListeners();
            LoadSettings();
        }
        
        /// <summary>
        /// Initialize the settings system
        /// </summary>
        private void InitializeSettings()
        {
            // Ensure settings panel starts closed
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            
            // Set up slider ranges
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
            }
            
            if (soundEffectVolumeSlider != null)
            {
                soundEffectVolumeSlider.minValue = 0f;
                soundEffectVolumeSlider.maxValue = 1f;
            }
        }
        
        /// <summary>
        /// Set up all button and slider event listeners
        /// </summary>
        private void SetupEventListeners()
        {
            // Button listeners
            if (openSettingsButton != null)
            {
                openSettingsButton.onClick.RemoveAllListeners();
                openSettingsButton.onClick.AddListener(OpenSettings);
            }
            
            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.RemoveAllListeners();
                closeSettingsButton.onClick.AddListener(CloseSettings);
            }
            
            // Slider listeners
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            
            if (soundEffectVolumeSlider != null)
            {
                soundEffectVolumeSlider.onValueChanged.RemoveAllListeners();
                soundEffectVolumeSlider.onValueChanged.AddListener(OnSoundEffectVolumeChanged);
            }
        }
        
        /// <summary>
        /// Open the settings panel
        /// </summary>
        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                PlayButtonSound();
                Debug.Log("Settings opened");
            }
        }
        
        /// <summary>
        /// Close the settings panel
        /// </summary>
        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                PlayButtonSound();
                SaveSettings();
                Debug.Log("Settings closed and saved");
            }
        }
        
        /// <summary>
        /// Handle music volume changes
        /// </summary>
        private void OnMusicVolumeChanged(float value)
        {
            // Update AudioManager
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.MusicVolume = value;
            }
            
            // Update display text
            if (musicVolumeText != null)
            {
                musicVolumeText.text = $"Music: {Mathf.RoundToInt(value * 100)}%";
            }
        }
        
        /// <summary>
        /// Handle sound effect volume changes
        /// </summary>
        private void OnSoundEffectVolumeChanged(float value)
        {
            // Update AudioManager
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SoundEffectVolume = value;
            }
            
            // Update display text
            if (soundEffectVolumeText != null)
            {
                soundEffectVolumeText.text = $"Sound Effects: {Mathf.RoundToInt(value * 100)}%";
            }
        }
        
        /// <summary>
        /// Load saved settings
        /// </summary>
        private void LoadSettings()
        {
            // Load music volume
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", DEFAULT_MUSIC_VOLUME);
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = musicVolume;
            }
            
            // Load sound effect volume
            float sfxVolume = PlayerPrefs.GetFloat("SoundEffectVolume", DEFAULT_SFX_VOLUME);
            if (soundEffectVolumeSlider != null)
            {
                soundEffectVolumeSlider.value = sfxVolume;
            }
            
            // Apply to AudioManager
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.MusicVolume = musicVolume;
                AudioManager.Instance.SoundEffectVolume = sfxVolume;
            }
            
            Debug.Log($"Settings loaded - Music: {musicVolume:F2}, SFX: {sfxVolume:F2}");
        }
        
        /// <summary>
        /// Save current settings
        /// </summary>
        private void SaveSettings()
        {
            if (AudioManager.Instance != null)
            {
                PlayerPrefs.SetFloat("MusicVolume", AudioManager.Instance.MusicVolume);
                PlayerPrefs.SetFloat("SoundEffectVolume", AudioManager.Instance.SoundEffectVolume);
                PlayerPrefs.Save();
                
                Debug.Log("Settings saved successfully");
            }
        }
        
        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = DEFAULT_MUSIC_VOLUME;
            }
            
            if (soundEffectVolumeSlider != null)
            {
                soundEffectVolumeSlider.value = DEFAULT_SFX_VOLUME;
            }
            
            PlayButtonSound();
            Debug.Log("Settings reset to defaults");
        }
        
        /// <summary>
        /// Play button click sound
        /// </summary>
        private void PlayButtonSound()
        {
            if (AudioManager.Instance != null && buttonClickSound != null)
            {
                AudioManager.Instance.PlaySoundEffect(buttonClickSound);
            }
        }
        
        /// <summary>
        /// Play button hover sound
        /// </summary>
        public void PlayHoverSound()
        {
            if (AudioManager.Instance != null && buttonHoverSound != null)
            {
                AudioManager.Instance.PlaySoundEffect(buttonHoverSound);
            }
        }
        
        #if UNITY_EDITOR
        [Header("Editor Testing")]
        [SerializeField] private bool showDebugInfo = false;
        
        [ContextMenu("Test Open Settings")]
        private void TestOpenSettings()
        {
            OpenSettings();
        }
        
        [ContextMenu("Test Close Settings")]
        private void TestCloseSettings()
        {
            CloseSettings();
        }
        
        [ContextMenu("Test Reset Settings")]
        private void TestResetSettings()
        {
            ResetToDefaults();
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 250, 120));
            GUILayout.Label("Settings Debug Info:");
            
            if (AudioManager.Instance != null)
            {
                GUILayout.Label($"Music Volume: {AudioManager.Instance.MusicVolume:F2}");
                GUILayout.Label($"SFX Volume: {AudioManager.Instance.SoundEffectVolume:F2}");
            }
            
            GUILayout.Label($"Panel Active: {(settingsPanel != null ? settingsPanel.activeInHierarchy : false)}");
            
            if (GUILayout.Button("Toggle Settings"))
            {
                if (settingsPanel != null)
                {
                    if (settingsPanel.activeInHierarchy)
                        CloseSettings();
                    else
                        OpenSettings();
                }
            }
            
            GUILayout.EndArea();
        }
        #endif
    }
}
