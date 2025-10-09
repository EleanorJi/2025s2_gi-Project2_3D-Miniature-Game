using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Antventure.Systems.Audio
{
    /// <summary>
    /// Scene BGM Controller - Automatically manages BGM transitions between scenes
    /// This component should be attached to the AudioManager or a persistent GameObject
    /// </summary>
    [System.Serializable]
    public class SceneBGMData
    {
        public string sceneName;
        public AudioClip bgmClip;
        public bool loopBGM = true;
        public float volume = 0.7f;
        public bool useFadeTransition = true;
    }

    public class SceneBGMController : MonoBehaviour
    {
        [Header("Default BGM Settings")]
        [SerializeField] private AudioClip defaultBGM;
        [SerializeField] private bool playDefaultOnStart = true;
        [SerializeField] private float defaultVolume = 0.7f;
        
        [Header("Scene-Specific BGM Configuration")]
        [SerializeField] private List<SceneBGMData> sceneBGMDatabase = new List<SceneBGMData>();
        
        [Header("Transition Settings")]
        [SerializeField] private bool enableAutoTransitions = true;
        [SerializeField] private float transitionDelay = 0.5f; // Delay after scene load before starting BGM
        
        private string currentSceneName;
        private AudioClip currentBGM;
        
        private void Awake()
        {
            // Ensure this persists across scenes
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to scene events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        
        private void Start()
        {
            // Initialize with current scene
            currentSceneName = SceneManager.GetActiveScene().name;
            
            if (playDefaultOnStart && enableAutoTransitions)
            {
                StartCoroutine(DelayedBGMStart());
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from scene events
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!enableAutoTransitions) return;
            
            currentSceneName = scene.name;
            
            // Start BGM after a short delay to ensure scene is fully loaded
            StartCoroutine(DelayedBGMStart());
        }
        
        private void OnSceneUnloaded(Scene scene)
        {
            // Optional: Handle scene unload events if needed
        }
        
        private System.Collections.IEnumerator DelayedBGMStart()
        {
            // Wait for scene to fully initialize
            yield return new WaitForSeconds(transitionDelay);
            
            // Get BGM for current scene
            SceneBGMData sceneBGM = GetSceneBGMData(currentSceneName);
            
            if (sceneBGM != null && sceneBGM.bgmClip != null)
            {
                // Play scene-specific BGM
                PlaySceneBGM(sceneBGM);
            }
            else if (defaultBGM != null && currentBGM != defaultBGM)
            {
                // Fall back to default BGM
                PlayDefaultBGM();
            }
        }
        
        private SceneBGMData GetSceneBGMData(string sceneName)
        {
            foreach (var bgmData in sceneBGMDatabase)
            {
                if (bgmData.sceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return bgmData;
                }
            }
            return null;
        }
        
        private void PlaySceneBGM(SceneBGMData bgmData)
        {
            if (AudioManager.Instance == null || bgmData.bgmClip == currentBGM)
            {
                return;
            }
            
            currentBGM = bgmData.bgmClip;
            
            if (bgmData.useFadeTransition)
            {
                AudioManager.Instance.PlayBGMWithFade(bgmData.bgmClip, bgmData.loopBGM, bgmData.volume);
            }
            else
            {
                AudioManager.Instance.PlayBGM(bgmData.bgmClip, bgmData.loopBGM, bgmData.volume);
            }
            
            Debug.Log($"Playing scene BGM for '{currentSceneName}': {bgmData.bgmClip.name}");
        }
        
        private void PlayDefaultBGM()
        {
            if (AudioManager.Instance == null || defaultBGM == currentBGM)
            {
                return;
            }
            
            currentBGM = defaultBGM;
            AudioManager.Instance.PlayBGMWithFade(defaultBGM, true, defaultVolume);
            
            Debug.Log($"Playing default BGM: {defaultBGM.name}");
        }
        
        /// <summary>
        /// Manually set BGM for a specific scene
        /// </summary>
        public void SetSceneBGM(string sceneName, AudioClip bgmClip, bool loop = true, float volume = 0.7f, bool useFade = true)
        {
            // Check if scene BGM data already exists
            SceneBGMData existingData = GetSceneBGMData(sceneName);
            
            if (existingData != null)
            {
                // Update existing data
                existingData.bgmClip = bgmClip;
                existingData.loopBGM = loop;
                existingData.volume = volume;
                existingData.useFadeTransition = useFade;
            }
            else
            {
                // Add new scene BGM data
                sceneBGMDatabase.Add(new SceneBGMData
                {
                    sceneName = sceneName,
                    bgmClip = bgmClip,
                    loopBGM = loop,
                    volume = volume,
                    useFadeTransition = useFade
                });
            }
            
            // If this is the current scene, play the BGM immediately
            if (currentSceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                PlaySceneBGM(GetSceneBGMData(sceneName));
            }
        }
        
        /// <summary>
        /// Remove BGM mapping for a specific scene
        /// </summary>
        public void RemoveSceneBGM(string sceneName)
        {
            sceneBGMDatabase.RemoveAll(data => data.sceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Enable or disable automatic BGM transitions
        /// </summary>
        public void SetAutoTransitions(bool enabled)
        {
            enableAutoTransitions = enabled;
        }
        
        /// <summary>
        /// Get current scene BGM info
        /// </summary>
        public SceneBGMData GetCurrentSceneBGM()
        {
            return GetSceneBGMData(currentSceneName);
        }
        
        #if UNITY_EDITOR
        [Header("Editor Tools")]
        [SerializeField] private bool showDebugInfo = true;
        
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label($"Current Scene: {currentSceneName}");
            GUILayout.Label($"Current BGM: {(currentBGM != null ? currentBGM.name : "None")}");
            GUILayout.Label($"Auto Transitions: {enableAutoTransitions}");
            GUILayout.Label($"Scene BGM Count: {sceneBGMDatabase.Count}");
            
            if (GUILayout.Button("Play Default BGM"))
            {
                PlayDefaultBGM();
            }
            
            if (GUILayout.Button("Stop BGM"))
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopBGMWithFade();
                    currentBGM = null;
                }
            }
            GUILayout.EndArea();
        }
        #endif
    }
}
