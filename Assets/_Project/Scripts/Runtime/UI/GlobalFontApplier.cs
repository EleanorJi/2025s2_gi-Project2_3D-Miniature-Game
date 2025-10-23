using UnityEngine;
using UnityEngine.SceneManagement;

namespace Antventure.UI
{
    /// <summary>
    /// Global Font Applier - Automatically applies unified fonts when scenes load
    /// </summary>
    public class GlobalFontApplier : MonoBehaviour
    {
        [Header("Settings")]
        public bool applyOnSceneLoad = true;
        public bool applyOnStart = true;
        public float delayBeforeApply = 0.5f; // Delay to ensure all UI is loaded
        
        private static GlobalFontApplier _instance;
        
        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (applyOnSceneLoad)
                {
                    SceneManager.sceneLoaded += OnSceneLoaded;
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (applyOnStart)
            {
                Invoke(nameof(ApplyFontsDelayed), delayBeforeApply);
            }
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GlobalFontApplier] Scene loaded: {scene.name}");
            Invoke(nameof(ApplyFontsDelayed), delayBeforeApply);
        }
        
        private void ApplyFontsDelayed()
        {
            if (FontManager.Instance != null)
            {
                FontManager.Instance.ApplyFontsToScene();
            }
            else
            {
                Debug.LogWarning("[GlobalFontApplier] FontManager instance not found!");
            }
        }
        
        [ContextMenu("Apply Fonts to Current Scene")]
        public void ApplyFontsToCurrentScene()
        {
            ApplyFontsDelayed();
        }
        
        private void OnDestroy()
        {
            if (applyOnSceneLoad)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
    }
}

