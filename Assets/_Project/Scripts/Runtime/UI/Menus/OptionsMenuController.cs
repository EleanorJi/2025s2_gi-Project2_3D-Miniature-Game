using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace Antventure.UI.Menus
{
    /// <summary>
    /// Options Menu Controller
    /// Manages game settings options including audio, graphics, controls, etc.
    /// </summary>
    public class OptionsMenuController : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Graphics Settings")]
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Dropdown resolutionDropdown;

        [Header("Control Settings")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Toggle invertYAxisToggle;

        [Header("UI Elements")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetToDefaultButton;
        [SerializeField] private Button applyButton;

        // Settings key constants
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";
        private const string QUALITY_KEY = "QualityLevel";
        private const string FULLSCREEN_KEY = "Fullscreen";
        private const string RESOLUTION_KEY = "Resolution";
        private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";
        private const string INVERT_Y_KEY = "InvertY";

        // Available resolutions list
        private Resolution[] availableResolutions;

        private void Start()
        {
            InitializeSettings();
            SetupEventListeners();
            LoadSettings();
        }

        /// <summary>
        /// Initialize settings interface
        /// </summary>
        private void InitializeSettings()
        {
            // Initialize resolution options
            InitializeResolutions();
            
            // Initialize quality options
            InitializeQualitySettings();
        }

        /// <summary>
        /// Initialize resolution options
        /// </summary>
        private void InitializeResolutions()
        {
            if (resolutionDropdown != null)
            {
                availableResolutions = Screen.resolutions;
                resolutionDropdown.ClearOptions();

                System.Collections.Generic.List<string> resolutionOptions = new System.Collections.Generic.List<string>();
                int currentResolutionIndex = 0;

                for (int i = 0; i < availableResolutions.Length; i++)
                {
                    string option = availableResolutions[i].width + " x " + availableResolutions[i].height;
                    resolutionOptions.Add(option);

                    // Find current resolution
                    if (availableResolutions[i].width == Screen.currentResolution.width &&
                        availableResolutions[i].height == Screen.currentResolution.height)
                    {
                        currentResolutionIndex = i;
                    }
                }

                resolutionDropdown.AddOptions(resolutionOptions);
                resolutionDropdown.value = currentResolutionIndex;
            }
        }

        /// <summary>
        /// Initialize quality settings options
        /// </summary>
        private void InitializeQualitySettings()
        {
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                
                System.Collections.Generic.List<string> qualityOptions = new System.Collections.Generic.List<string>();
                string[] qualityNames = QualitySettings.names;
                
                for (int i = 0; i < qualityNames.Length; i++)
                {
                    qualityOptions.Add(qualityNames[i]);
                }
                
                qualityDropdown.AddOptions(qualityOptions);
                qualityDropdown.value = QualitySettings.GetQualityLevel();
            }
        }

        /// <summary>
        /// Setup event listeners
        /// </summary>
        private void SetupEventListeners()
        {
            // Audio slider events
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

            // Graphics settings events
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(SetQuality);
            
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(SetResolution);

            // Control settings events
            if (mouseSensitivitySlider != null)
                mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
            
            if (invertYAxisToggle != null)
                invertYAxisToggle.onValueChanged.AddListener(SetInvertY);

            // Button events
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseOptionsMenu);
            
            if (resetToDefaultButton != null)
                resetToDefaultButton.onClick.AddListener(ResetToDefault);
            
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplySettings);
        }

        #region Audio Settings

        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            if (audioMixer != null)
            {
                // Convert 0-1 value to -80 to 0 decibel value
                float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                audioMixer.SetFloat("MasterVolume", dbValue);
            }
            
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        }

        /// <summary>
        /// Set music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            if (audioMixer != null)
            {
                float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                audioMixer.SetFloat("MusicVolume", dbValue);
            }
            
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        }

        /// <summary>
        /// Set SFX volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            if (audioMixer != null)
            {
                float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                audioMixer.SetFloat("SFXVolume", dbValue);
            }
            
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        }

        #endregion

        #region Graphics Settings

        /// <summary>
        /// Set quality level
        /// </summary>
        public void SetQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
            PlayerPrefs.SetInt(QUALITY_KEY, qualityIndex);
        }

        /// <summary>
        /// Set fullscreen mode
        /// </summary>
        public void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        }

        /// <summary>
        /// Set resolution
        /// </summary>
        public void SetResolution(int resolutionIndex)
        {
            if (availableResolutions != null && resolutionIndex < availableResolutions.Length)
            {
                Resolution resolution = availableResolutions[resolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
                PlayerPrefs.SetInt(RESOLUTION_KEY, resolutionIndex);
            }
        }

        #endregion

        #region Control Settings

        /// <summary>
        /// Set mouse sensitivity
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, sensitivity);
            // Here you can notify other systems to update mouse sensitivity
        }

        /// <summary>
        /// Set Y-axis inversion
        /// </summary>
        public void SetInvertY(bool invert)
        {
            PlayerPrefs.SetInt(INVERT_Y_KEY, invert ? 1 : 0);
            // Here you can notify other systems to update Y-axis settings
        }

        #endregion

        #region Menu Actions

        /// <summary>
        /// Close options menu
        /// </summary>
        public void CloseOptionsMenu()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlayButtonClickSound();
                UIManager.Instance.CloseOptionsMenu();
            }
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefault()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlayButtonClickSound();
            }

            // Reset audio settings
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = 0.8f;
                SetMasterVolume(0.8f);
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = 0.7f;
                SetMusicVolume(0.7f);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = 0.8f;
                SetSFXVolume(0.8f);
            }

            // Reset graphics settings
            if (qualityDropdown != null)
            {
                int defaultQuality = QualitySettings.names.Length - 1; // Highest quality
                qualityDropdown.value = defaultQuality;
                SetQuality(defaultQuality);
            }
            
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = true;
                SetFullscreen(true);
            }

            // Reset control settings
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = 1.0f;
                SetMouseSensitivity(1.0f);
            }
            
            if (invertYAxisToggle != null)
            {
                invertYAxisToggle.isOn = false;
                SetInvertY(false);
            }
        }

        /// <summary>
        /// Apply settings
        /// </summary>
        public void ApplySettings()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlayButtonClickSound();
            }

            // Save all settings
            PlayerPrefs.Save();
            
            UnityEngine.Debug.Log("Settings applied and saved!");
        }

        #endregion

        #region Settings Persistence

        /// <summary>
        /// Load saved settings
        /// </summary>
        private void LoadSettings()
        {
            // Load audio settings
            if (masterVolumeSlider != null)
            {
                float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 0.8f);
                masterVolumeSlider.value = masterVolume;
                SetMasterVolume(masterVolume);
            }
            
            if (musicVolumeSlider != null)
            {
                float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.7f);
                musicVolumeSlider.value = musicVolume;
                SetMusicVolume(musicVolume);
            }
            
            if (sfxVolumeSlider != null)
            {
                float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);
                sfxVolumeSlider.value = sfxVolume;
                SetSFXVolume(sfxVolume);
            }

            // Load graphics settings
            if (qualityDropdown != null)
            {
                int quality = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
                qualityDropdown.value = quality;
                SetQuality(quality);
            }
            
            if (fullscreenToggle != null)
            {
                bool fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;
                fullscreenToggle.isOn = fullscreen;
                SetFullscreen(fullscreen);
            }
            
            if (resolutionDropdown != null)
            {
                int resolution = PlayerPrefs.GetInt(RESOLUTION_KEY, availableResolutions.Length - 1);
                if (resolution < availableResolutions.Length)
                {
                    resolutionDropdown.value = resolution;
                    SetResolution(resolution);
                }
            }

            // Load control settings
            if (mouseSensitivitySlider != null)
            {
                float sensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, 1.0f);
                mouseSensitivitySlider.value = sensitivity;
                SetMouseSensitivity(sensitivity);
            }
            
            if (invertYAxisToggle != null)
            {
                bool invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1;
                invertYAxisToggle.isOn = invertY;
                SetInvertY(invertY);
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Remove event listeners
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveAllListeners();
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();

            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.RemoveAllListeners();
            
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveAllListeners();
            
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveAllListeners();

            if (mouseSensitivitySlider != null)
                mouseSensitivitySlider.onValueChanged.RemoveAllListeners();
            
            if (invertYAxisToggle != null)
                invertYAxisToggle.onValueChanged.RemoveAllListeners();

            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();
            
            if (resetToDefaultButton != null)
                resetToDefaultButton.onClick.RemoveAllListeners();
            
            if (applyButton != null)
                applyButton.onClick.RemoveAllListeners();
        }
    }
}

