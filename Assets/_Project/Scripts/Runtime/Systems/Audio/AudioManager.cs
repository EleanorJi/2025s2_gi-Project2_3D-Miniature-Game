using UnityEngine;

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
            }
            else
            {
                Destroy(gameObject);
            }
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
            }
        }
        
        public void StopMusic()
        {
            if (musicAudioSource != null)
            {
                musicAudioSource.Stop();
            }
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