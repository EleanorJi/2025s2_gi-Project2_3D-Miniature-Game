using UnityEngine;
using Antventure.Systems.Audio;

namespace Antventure.Systems.Audio
{
    /// <summary>
    /// BGM Setup Helper - Utility script to quickly configure BGM for your project
    /// Attach this to any GameObject and use the context menu options to set up BGM
    /// </summary>
    public class BGMSetupHelper : MonoBehaviour
    {
        [Header("Quick Setup")]
        [SerializeField] private AudioClip mainMenuBGM;
        [SerializeField] private AudioClip level1BGM;
        [SerializeField] private AudioClip tutorialBGM;
        [SerializeField] private AudioClip defaultBGM;
        
        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 0.7f;
        
        [Header("Scene Names (must match exactly)")]
        [SerializeField] private string homeSceneName = "StartScene";
        [SerializeField] private string level1SceneName = "Level1";
        [SerializeField] private string level1_2SceneName = "Level1-2";
        [SerializeField] private string tutorialSceneName = "Tutorial";
        
        #if UNITY_EDITOR
        [ContextMenu("Setup All Scene BGM")]
        private void SetupAllSceneBGM()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("AudioManager not found! Make sure AudioManager exists in the scene.");
                return;
            }
            
            // Set default BGM
            if (defaultBGM != null)
            {
                Debug.Log($"Setting default BGM: {defaultBGM.name}");
            }
            
            // Setup scene-specific BGM
            if (mainMenuBGM != null)
            {
                AudioManager.Instance.SetSceneBGM(homeSceneName, mainMenuBGM, true, bgmVolume);
                Debug.Log($"Set BGM for {homeSceneName}: {mainMenuBGM.name}");
            }
            
            if (level1BGM != null)
            {
                AudioManager.Instance.SetSceneBGM(level1SceneName, level1BGM, true, bgmVolume);
                AudioManager.Instance.SetSceneBGM(level1_2SceneName, level1BGM, true, bgmVolume); // Same BGM for both Level1 variants
                Debug.Log($"Set BGM for {level1SceneName} and {level1_2SceneName}: {level1BGM.name}");
            }
            
            if (tutorialBGM != null)
            {
                AudioManager.Instance.SetSceneBGM(tutorialSceneName, tutorialBGM, true, bgmVolume);
                Debug.Log($"Set BGM for {tutorialSceneName}: {tutorialBGM.name}");
            }
            
            Debug.Log("BGM setup completed!");
        }
        
        [ContextMenu("Test Play Main Menu BGM")]
        private void TestPlayMainMenuBGM()
        {
            if (AudioManager.Instance != null && mainMenuBGM != null)
            {
                AudioManager.Instance.PlayBGMWithFade(mainMenuBGM, true, bgmVolume);
                Debug.Log($"Testing Main Menu BGM: {mainMenuBGM.name}");
            }
            else
            {
                Debug.LogWarning("AudioManager or Main Menu BGM not found!");
            }
        }
        
        [ContextMenu("Test Play Level1 BGM")]
        private void TestPlayLevel1BGM()
        {
            if (AudioManager.Instance != null && level1BGM != null)
            {
                AudioManager.Instance.PlayBGMWithFade(level1BGM, true, bgmVolume);
                Debug.Log($"Testing Level1 BGM: {level1BGM.name}");
            }
            else
            {
                Debug.LogWarning("AudioManager or Level1 BGM not found!");
            }
        }
        
        [ContextMenu("Test Play Tutorial BGM")]
        private void TestPlayTutorialBGM()
        {
            if (AudioManager.Instance != null && tutorialBGM != null)
            {
                AudioManager.Instance.PlayBGMWithFade(tutorialBGM, true, bgmVolume);
                Debug.Log($"Testing Tutorial BGM: {tutorialBGM.name}");
            }
            else
            {
                Debug.LogWarning("AudioManager or Tutorial BGM not found!");
            }
        }
        
        [ContextMenu("Stop All BGM")]
        private void StopAllBGM()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGMWithFade();
                Debug.Log("Stopped all BGM");
            }
        }
        
        [ContextMenu("Create AudioManager GameObject")]
        private void CreateAudioManager()
        {
            // Check if AudioManager already exists
            if (AudioManager.Instance != null)
            {
                Debug.LogWarning("AudioManager already exists in the scene!");
                return;
            }
            
            // Create new AudioManager GameObject
            GameObject audioManagerGO = new GameObject("AudioManager");
            AudioManager audioManager = audioManagerGO.AddComponent<AudioManager>();
            
            // Add AudioSources
            AudioSource musicSource = audioManagerGO.AddComponent<AudioSource>();
            AudioSource sfxSource = audioManagerGO.AddComponent<AudioSource>();
            
            // Configure AudioSources
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = bgmVolume;
            
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.7f;
            
            Debug.Log("Created AudioManager GameObject with AudioSources");
            
            // Select the created object in hierarchy
            UnityEditor.Selection.activeGameObject = audioManagerGO;
        }
        
        private void OnValidate()
        {
            // Clamp volume to valid range
            bgmVolume = Mathf.Clamp01(bgmVolume);
        }
        #endif
        
        /// <summary>
        /// Runtime method to setup BGM (can be called from other scripts)
        /// </summary>
        public void SetupBGMRuntime()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("AudioManager not found!");
                return;
            }
            
            // Setup scene-specific BGM at runtime
            if (mainMenuBGM != null)
                AudioManager.Instance.SetSceneBGM(homeSceneName, mainMenuBGM, true, bgmVolume);
            
            if (level1BGM != null)
            {
                AudioManager.Instance.SetSceneBGM(level1SceneName, level1BGM, true, bgmVolume);
                AudioManager.Instance.SetSceneBGM(level1_2SceneName, level1BGM, true, bgmVolume);
            }
            
            if (tutorialBGM != null)
                AudioManager.Instance.SetSceneBGM(tutorialSceneName, tutorialBGM, true, bgmVolume);
        }
        
        /// <summary>
        /// Get the BGM clip for a specific scene
        /// </summary>
        public AudioClip GetBGMForScene(string sceneName)
        {
            switch (sceneName)
            {
                case "StartScene":
                    return mainMenuBGM;
                case "Level1":
                case "Level1-2":
                    return level1BGM;
                case "Tutorial":
                    return tutorialBGM;
                default:
                    return defaultBGM;
            }
        }
    }
}
