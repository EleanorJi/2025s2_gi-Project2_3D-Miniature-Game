using UnityEngine;

namespace Antventure.UI.Menus
{
    /// <summary>
    /// Pause System Setup Helper - Quick setup for the new pause system
    /// </summary>
    public class PauseSystemSetup : MonoBehaviour
    {
        [Header("Setup Options")]
        [SerializeField] private bool createOnStart = false;
        [SerializeField] private bool removeAfterSetup = true;
        
        [Header("Cursor Textures (Optional)")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D hoverCursor;
        
        private void Start()
        {
            if (createOnStart)
            {
                CreatePauseSystem();
            }
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Create Pause System")]
        private void CreatePauseSystem()
        {
            // Check if already exists
            if (GamePauseSystem.Instance != null)
            {
                Debug.LogWarning("GamePauseSystem already exists!");
                return;
            }
            
            // Create GameObject
            GameObject pauseSystemGO = new GameObject("GamePauseSystem");
            
            // Add component
            GamePauseSystem pauseSystem = pauseSystemGO.AddComponent<GamePauseSystem>();
            
            // Assign cursor textures if provided
            if (defaultCursor != null || hoverCursor != null)
            {
                // Note: You'll need to assign these manually in the inspector
                // as the fields are private in GamePauseSystem
                Debug.Log("Remember to assign cursor textures in the GamePauseSystem inspector if desired");
            }
            
            Debug.Log("✅ GamePauseSystem created successfully!");
            Debug.Log("🎯 Features:");
            Debug.Log("   - ESC key pausing in gameplay scenes");
            Debug.Log("   - Proper cursor management (hidden in game, visible in pause)");
            Debug.Log("   - Button hover effects");
            Debug.Log("   - Music and SFX volume controls");
            Debug.Log("   - Exit to main menu");
            Debug.Log("   - Cross-scene persistence");
            
            // Remove setup helper
            if (removeAfterSetup)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }
        
        [ContextMenu("Test Pause System")]
        private void TestPauseSystem()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Test can only be run in play mode");
                return;
            }
            
            if (GamePauseSystem.Instance != null)
            {
                GamePauseSystem.Instance.TogglePause();
                Debug.Log("Toggled pause state");
            }
            else
            {
                Debug.LogError("GamePauseSystem not found! Create it first.");
            }
        }
        
        [ContextMenu("Check System Status")]
        private void CheckSystemStatus()
        {
            Debug.Log("=== Pause System Status ===");
            
            if (GamePauseSystem.Instance != null)
            {
                Debug.Log("✅ GamePauseSystem: Found");
                Debug.Log($"   - Is Paused: {GamePauseSystem.Instance.IsPaused}");
            }
            else
            {
                Debug.Log("❌ GamePauseSystem: Not Found");
            }
            
            Debug.Log($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Debug.Log($"Time Scale: {Time.timeScale}");
            Debug.Log($"Cursor Visible: {Cursor.visible}");
            Debug.Log($"Cursor Lock State: {Cursor.lockState}");
            
            // Check AudioManager
            if (Antventure.Systems.Audio.AudioManager.Instance != null)
            {
                Debug.Log("✅ AudioManager: Found");
            }
            else
            {
                Debug.Log("⚠️ AudioManager: Not Found");
            }
            
            // Check EventSystem
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                Debug.Log("✅ EventSystem: Found");
            }
            else
            {
                Debug.Log("⚠️ EventSystem: Not Found");
            }
        }
        #endif
    }
}

