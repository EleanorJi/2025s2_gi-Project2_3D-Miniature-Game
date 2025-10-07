using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Antventure.Systems.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicAudioSource;
        [SerializeField] private AudioSource soundEffectAudioSource;
        
        [Header("Default Volumes")]
        [SerializeField] private float defaultMusicVolume = 0.7f;
        [SerializeField] private float defaultSoundEffectVolume = 0.7f;
        
        [Header("BGM Settings")]
        [SerializeField] private float fadeTransitionTime = 1.0f;
        [SerializeField] private AudioClip defaultBGM;
        [SerializeField] private bool playBGMOnStart = true;
        
        [Header("Scene-Specific BGM")]
        [SerializeField] private List<SceneBGMMapping> sceneBGMList = new List<SceneBGMMapping>();
        
        // Current BGM state
        private AudioClip currentBGM;
        private bool isFading = false;
        
        [System.Serializable]
        public class SceneBGMMapping
        {
            public string sceneName;
            public AudioClip bgmClip;
            public bool loopBGM = true;
            public float volume = 0.7f;
        }
        
        public float MusicVolume 
        { 
            get => musicAudioSource != null ? musicAudioSource.volume : 0f;
            set 
            {
                if (musicAudioSource != null)
                    musicAudioSource.volume = Mathf.Clamp01(value);
            }
        }
        
        public float SoundEffectVolume 
        { 
            get => soundEffectAudioSource != null ? soundEffectAudioSource.volume : 0f;
            set 
            {
                if (soundEffectAudioSource != null)
                    soundEffectAudioSource.volume = Mathf.Clamp01(value);
            }
        }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadAudioSettings();
                InitializeBGM();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Subscribe to scene change events
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Start default BGM if enabled
            if (playBGMOnStart && defaultBGM != null)
            {
                PlayBGM(defaultBGM, true);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from scene change events
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void InitializeBGM()
        {
            // Ensure music audio source is properly configured for BGM
            if (musicAudioSource != null)
            {
                musicAudioSource.loop = true;
                musicAudioSource.playOnAwake = false;
            }
        }
        
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Check if there's a specific BGM for this scene
            SceneBGMMapping sceneBGM = GetSceneBGM(scene.name);
            if (sceneBGM != null && sceneBGM.bgmClip != null)
            {
                PlayBGMWithFade(sceneBGM.bgmClip, sceneBGM.loopBGM, sceneBGM.volume);
            }
            else if (defaultBGM != null && currentBGM != defaultBGM)
            {
                // Fall back to default BGM if no scene-specific BGM is found
                PlayBGMWithFade(defaultBGM, true);
            }
        }
        
        private SceneBGMMapping GetSceneBGM(string sceneName)
        {
            foreach (var mapping in sceneBGMList)
            {
                if (mapping.sceneName == sceneName)
                {
                    return mapping;
                }
            }
            return null;
        }
        
        private void LoadAudioSettings()
        {
            MusicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
            SoundEffectVolume = PlayerPrefs.GetFloat("SoundEffectVolume", defaultSoundEffectVolume);
        }
        
        public void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
            PlayerPrefs.SetFloat("SoundEffectVolume", SoundEffectVolume);
            PlayerPrefs.Save();
        }
        
        public void PlaySoundEffect(AudioClip clip)
        {
            if (soundEffectAudioSource != null && clip != null)
            {
                soundEffectAudioSource.PlayOneShot(clip);
            }
        }
        
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicAudioSource != null && clip != null)
            {
                musicAudioSource.clip = clip;
                musicAudioSource.loop = loop;
                musicAudioSource.Play();
                currentBGM = clip;
            }
        }
        
        public void StopMusic()
        {
            if (musicAudioSource != null)
            {
                musicAudioSource.Stop();
                currentBGM = null;
            }
        }
        
        /// <summary>
        /// Play BGM immediately without fade effect
        /// </summary>
        public void PlayBGM(AudioClip clip, bool loop = true, float volume = -1f)
        {
            if (musicAudioSource != null && clip != null)
            {
                // Stop current music
                musicAudioSource.Stop();
                
                // Set new clip
                musicAudioSource.clip = clip;
                musicAudioSource.loop = loop;
                
                // Set volume (use default if not specified)
                if (volume >= 0f)
                {
                    musicAudioSource.volume = Mathf.Clamp01(volume);
                }
                else
                {
                    musicAudioSource.volume = defaultMusicVolume;
                }
                
                // Play new music
                musicAudioSource.Play();
                currentBGM = clip;
                
                Debug.Log($"Playing BGM: {clip.name}");
            }
        }
        
        /// <summary>
        /// Play BGM with fade transition
        /// </summary>
        public void PlayBGMWithFade(AudioClip clip, bool loop = true, float targetVolume = -1f)
        {
            if (clip == currentBGM)
            {
                return; // Already playing this BGM
            }
            
            if (targetVolume < 0f)
            {
                targetVolume = defaultMusicVolume;
            }
            
            StartCoroutine(FadeBGMCoroutine(clip, loop, targetVolume));
        }
        
        /// <summary>
        /// Stop BGM with fade out effect
        /// </summary>
        public void StopBGMWithFade()
        {
            if (musicAudioSource != null && musicAudioSource.isPlaying)
            {
                StartCoroutine(FadeOutBGMCoroutine());
            }
        }
        
        /// <summary>
        /// Pause/Resume BGM
        /// </summary>
        public void PauseBGM()
        {
            if (musicAudioSource != null && musicAudioSource.isPlaying)
            {
                musicAudioSource.Pause();
            }
        }
        
        public void ResumeBGM()
        {
            if (musicAudioSource != null && !musicAudioSource.isPlaying && currentBGM != null)
            {
                musicAudioSource.UnPause();
            }
        }
        
        /// <summary>
        /// Set BGM for specific scene
        /// </summary>
        public void SetSceneBGM(string sceneName, AudioClip bgmClip, bool loop = true, float volume = 0.7f)
        {
            // Check if mapping already exists
            SceneBGMMapping existingMapping = GetSceneBGM(sceneName);
            if (existingMapping != null)
            {
                // Update existing mapping
                existingMapping.bgmClip = bgmClip;
                existingMapping.loopBGM = loop;
                existingMapping.volume = volume;
            }
            else
            {
                // Add new mapping
                sceneBGMList.Add(new SceneBGMMapping
                {
                    sceneName = sceneName,
                    bgmClip = bgmClip,
                    loopBGM = loop,
                    volume = volume
                });
            }
        }
        
        /// <summary>
        /// Coroutine for fading BGM transition
        /// </summary>
        private IEnumerator FadeBGMCoroutine(AudioClip newClip, bool loop, float targetVolume)
        {
            if (isFading) yield break;
            isFading = true;
            
            float originalVolume = musicAudioSource != null ? musicAudioSource.volume : 0f;
            
            // Fade out current music
            if (musicAudioSource != null && musicAudioSource.isPlaying)
            {
                float fadeOutTime = fadeTransitionTime * 0.5f;
                for (float t = 0; t < fadeOutTime; t += Time.deltaTime)
                {
                    float normalizedTime = t / fadeOutTime;
                    musicAudioSource.volume = Mathf.Lerp(originalVolume, 0f, normalizedTime);
                    yield return null;
                }
                musicAudioSource.volume = 0f;
                musicAudioSource.Stop();
            }
            
            // Set new clip
            if (newClip != null)
            {
                musicAudioSource.clip = newClip;
                musicAudioSource.loop = loop;
                musicAudioSource.volume = 0f;
                musicAudioSource.Play();
                currentBGM = newClip;
                
                // Fade in new music
                float fadeInTime = fadeTransitionTime * 0.5f;
                for (float t = 0; t < fadeInTime; t += Time.deltaTime)
                {
                    float normalizedTime = t / fadeInTime;
                    musicAudioSource.volume = Mathf.Lerp(0f, targetVolume, normalizedTime);
                    yield return null;
                }
                musicAudioSource.volume = targetVolume;
                
                Debug.Log($"Faded to BGM: {newClip.name}");
            }
            
            isFading = false;
        }
        
        /// <summary>
        /// Coroutine for fading out BGM
        /// </summary>
        private IEnumerator FadeOutBGMCoroutine()
        {
            if (isFading || musicAudioSource == null) yield break;
            isFading = true;
            
            float originalVolume = musicAudioSource.volume;
            
            for (float t = 0; t < fadeTransitionTime; t += Time.deltaTime)
            {
                float normalizedTime = t / fadeTransitionTime;
                musicAudioSource.volume = Mathf.Lerp(originalVolume, 0f, normalizedTime);
                yield return null;
            }
            
            musicAudioSource.volume = 0f;
            musicAudioSource.Stop();
            currentBGM = null;
            isFading = false;
            
            Debug.Log("BGM faded out and stopped");
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveAudioSettings();
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SaveAudioSettings();
        }
    }
}