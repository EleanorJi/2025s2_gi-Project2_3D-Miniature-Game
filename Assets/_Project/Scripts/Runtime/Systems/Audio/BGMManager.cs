using UnityEngine;
using UnityEngine.SceneManagement;

namespace Antventure.Systems.Audio
{
    /// <summary>
    /// BGM Manager - Handles automatic background music management for scenes
    /// This component can be placed in scenes to automatically set BGM when the scene loads
    /// </summary>
    public class BGMManager : MonoBehaviour
    {
        [Header("Scene BGM Settings")]
        [SerializeField] private AudioClip sceneBGM;
        [SerializeField] private bool loopBGM = true;
        [SerializeField] private float bgmVolume = 0.7f;
        [SerializeField] private bool useFadeTransition = true;
        [SerializeField] private bool playOnStart = true;
        
        [Header("Advanced Settings")]
        [SerializeField] private bool overrideGlobalBGM = false;
        [SerializeField] private bool stopBGMOnSceneExit = false;
        
        private void Start()
        {
            if (playOnStart && sceneBGM != null)
            {
                PlaySceneBGM();
            }
        }
        
        private void OnDestroy()
        {
            if (stopBGMOnSceneExit && AudioManager.Instance != null)
            {
                if (useFadeTransition)
                {
                    AudioManager.Instance.StopBGMWithFade();
                }
                else
                {
                    AudioManager.Instance.StopMusic();
                }
            }
        }
        
        /// <summary>
        /// Play the scene's BGM
        /// </summary>
        public void PlaySceneBGM()
        {
            if (AudioManager.Instance == null || sceneBGM == null)
            {
                Debug.LogWarning("AudioManager instance not found or no BGM assigned!");
                return;
            }
            
            if (useFadeTransition)
            {
                AudioManager.Instance.PlayBGMWithFade(sceneBGM, loopBGM, bgmVolume);
            }
            else
            {
                AudioManager.Instance.PlayBGM(sceneBGM, loopBGM, bgmVolume);
            }
            
            // Register this BGM with the current scene if override is enabled
            if (overrideGlobalBGM)
            {
                string currentSceneName = SceneManager.GetActiveScene().name;
                AudioManager.Instance.SetSceneBGM(currentSceneName, sceneBGM, loopBGM, bgmVolume);
            }
        }
        
        /// <summary>
        /// Stop the current BGM
        /// </summary>
        public void StopSceneBGM()
        {
            if (AudioManager.Instance == null) return;
            
            if (useFadeTransition)
            {
                AudioManager.Instance.StopBGMWithFade();
            }
            else
            {
                AudioManager.Instance.StopMusic();
            }
        }
        
        /// <summary>
        /// Change the scene BGM at runtime
        /// </summary>
        public void ChangeSceneBGM(AudioClip newBGM, bool loop = true, float volume = 0.7f)
        {
            sceneBGM = newBGM;
            loopBGM = loop;
            bgmVolume = volume;
            
            if (newBGM != null)
            {
                PlaySceneBGM();
            }
        }
        
        /// <summary>
        /// Pause/Resume BGM
        /// </summary>
        public void PauseBGM()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PauseBGM();
            }
        }
        
        public void ResumeBGM()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResumeBGM();
            }
        }
        
        #if UNITY_EDITOR
        [Header("Editor Tools")]
        [SerializeField] private bool testBGMInEditor = false;
        
        private void OnValidate()
        {
            // Clamp volume to valid range
            bgmVolume = Mathf.Clamp01(bgmVolume);
        }
        
        [ContextMenu("Test Play BGM")]
        private void TestPlayBGM()
        {
            if (Application.isPlaying && sceneBGM != null)
            {
                PlaySceneBGM();
            }
        }
        
        [ContextMenu("Test Stop BGM")]
        private void TestStopBGM()
        {
            if (Application.isPlaying)
            {
                StopSceneBGM();
            }
        }
        #endif
    }
}
