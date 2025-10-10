using UnityEngine;

namespace Antventure.UI.Menus
{
    /// <summary>
    /// Ultimate Pause Setup - Creates the complete pause system with unified cursor management
    /// This will solve all cursor conflicts once and for all
    /// </summary>
    public class UltimatePauseSetup : MonoBehaviour
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
                CreateUltimatePauseSystem();
            }
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Create Ultimate Pause System")]
        private void CreateUltimatePauseSystem()
        {
            Debug.Log("🚀 Creating Ultimate Pause System...");
            
            // Step 1: Create UnifiedCursorManager
            CreateUnifiedCursorManager();
            
            // Step 2: Create GamePauseSystem
            CreateGamePauseSystem();
            
            // Step 3: Disable conflicting systems
            DisableConflictingSystems();
            
            Debug.Log("✅ Ultimate Pause System created successfully!");
            Debug.Log("🎯 This system will:");
            Debug.Log("   - Override ALL other cursor management systems");
            Debug.Log("   - Hide cursor in gameplay scenes");
            Debug.Log("   - Show cursor in pause menu with proper button interactions");
            Debug.Log("   - Maintain your custom cursor textures");
            Debug.Log("   - Prevent all cursor conflicts");
            
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
        
        private void CreateUnifiedCursorManager()
        {
            if (UnifiedCursorManager.Instance != null)
            {
                Debug.Log("✅ UnifiedCursorManager already exists");
                return;
            }
            
            GameObject cursorManagerGO = new GameObject("UnifiedCursorManager");
            UnifiedCursorManager cursorManager = cursorManagerGO.AddComponent<UnifiedCursorManager>();
            
            // Assign cursor textures if provided
            if (defaultCursor != null || hoverCursor != null)
            {
                // Use reflection to set private fields
                var cursorManagerType = typeof(UnifiedCursorManager);
                
                if (defaultCursor != null)
                {
                    var defaultCursorField = cursorManagerType.GetField("defaultCursor", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    defaultCursorField?.SetValue(cursorManager, defaultCursor);
                }
                
                if (hoverCursor != null)
                {
                    var hoverCursorField = cursorManagerType.GetField("hoverCursor", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    hoverCursorField?.SetValue(cursorManager, hoverCursor);
                }
            }
            
            Debug.Log("✅ UnifiedCursorManager created");
        }
        
        private void CreateGamePauseSystem()
        {
            if (GamePauseSystem.Instance != null)
            {
                Debug.Log("✅ GamePauseSystem already exists");
                return;
            }
            
            GameObject pauseSystemGO = new GameObject("GamePauseSystem");
            GamePauseSystem pauseSystem = pauseSystemGO.AddComponent<GamePauseSystem>();
            
            Debug.Log("✅ GamePauseSystem created");
        }
        
        private void DisableConflictingSystems()
        {
            int disabledCount = 0;
            
            // Disable CursorManager
            CursorManager[] cursorManagers = FindObjectsOfType<CursorManager>(true);
            foreach (var manager in cursorManagers)
            {
                manager.enabled = false;
                disabledCount++;
                Debug.Log($"🔇 Disabled CursorManager on {manager.gameObject.name}");
            }
            
            // Disable GameplayCursorController - but don't disable them, just set them to non-aggressive
            GameplayCursorController[] gameplayCursors = FindObjectsOfType<GameplayCursorController>(true);
            foreach (var cursor in gameplayCursors)
            {
                // Use reflection to set aggressiveMode to false
                var cursorType = typeof(GameplayCursorController);
                var aggressiveModeField = cursorType.GetField("aggressiveMode", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (aggressiveModeField != null)
                {
                    aggressiveModeField.SetValue(cursor, false);
                    Debug.Log($"🔧 Set GameplayCursorController to non-aggressive on {cursor.gameObject.name}");
                }
                else
                {
                    cursor.enabled = false;
                    Debug.Log($"🔇 Disabled GameplayCursorController on {cursor.gameObject.name}");
                }
                disabledCount++;
            }
            
            // Disable old PauseMenuController
            PauseMenuController[] pauseControllers = FindObjectsOfType<PauseMenuController>(true);
            foreach (var controller in pauseControllers)
            {
                controller.enabled = false;
                disabledCount++;
                Debug.Log($"🔇 Disabled PauseMenuController on {controller.gameObject.name}");
            }
            
            Debug.Log($"✅ Modified/Disabled {disabledCount} conflicting systems");
        }
        
        [ContextMenu("Test Ultimate System")]
        private void TestUltimateSystem()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Test can only be run in play mode");
                return;
            }
            
            Debug.Log("🧪 Testing Ultimate Pause System...");
            
            if (UnifiedCursorManager.Instance != null)
            {
                Debug.Log("✅ UnifiedCursorManager: Active");
                Debug.Log($"   Current State: {UnifiedCursorManager.Instance.GetCurrentState()}");
                Debug.Log($"   Pause Menu Open: {UnifiedCursorManager.Instance.IsPauseMenuOpen()}");
            }
            else
            {
                Debug.LogError("❌ UnifiedCursorManager: Missing");
            }
            
            if (GamePauseSystem.Instance != null)
            {
                Debug.Log("✅ GamePauseSystem: Active");
                Debug.Log($"   Is Paused: {GamePauseSystem.Instance.IsPaused}");
                
                // Test toggle pause
                GamePauseSystem.Instance.TogglePause();
                Debug.Log("🎮 Toggled pause state for testing");
            }
            else
            {
                Debug.LogError("❌ GamePauseSystem: Missing");
            }
        }
        
        [ContextMenu("Check System Status")]
        private void CheckSystemStatus()
        {
            Debug.Log("=== Ultimate Pause System Status ===");
            
            // Check UnifiedCursorManager
            if (UnifiedCursorManager.Instance != null)
            {
                Debug.Log("✅ UnifiedCursorManager: Found");
                if (Application.isPlaying)
                {
                    Debug.Log($"   - Current State: {UnifiedCursorManager.Instance.GetCurrentState()}");
                    Debug.Log($"   - Pause Menu Open: {UnifiedCursorManager.Instance.IsPauseMenuOpen()}");
                }
            }
            else
            {
                Debug.Log("❌ UnifiedCursorManager: Not Found");
            }
            
            // Check GamePauseSystem
            if (GamePauseSystem.Instance != null)
            {
                Debug.Log("✅ GamePauseSystem: Found");
                if (Application.isPlaying)
                {
                    Debug.Log($"   - Is Paused: {GamePauseSystem.Instance.IsPaused}");
                }
            }
            else
            {
                Debug.Log("❌ GamePauseSystem: Not Found");
            }
            
            // Check system state
            Debug.Log($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Debug.Log($"Time Scale: {Time.timeScale}");
            Debug.Log($"Cursor Visible: {Cursor.visible}");
            Debug.Log($"Cursor Lock State: {Cursor.lockState}");
            
            // Check conflicting systems
            int conflictCount = 0;
            
            CursorManager[] cursorManagers = FindObjectsOfType<CursorManager>();
            foreach (var manager in cursorManagers)
            {
                if (manager.enabled)
                {
                    Debug.LogWarning($"⚠️ Active CursorManager found on {manager.gameObject.name}");
                    conflictCount++;
                }
            }
            
            GameplayCursorController[] gameplayCursors = FindObjectsOfType<GameplayCursorController>();
            foreach (var cursor in gameplayCursors)
            {
                if (cursor.enabled)
                {
                    Debug.LogWarning($"⚠️ Active GameplayCursorController found on {cursor.gameObject.name}");
                    conflictCount++;
                }
            }
            
            if (conflictCount == 0)
            {
                Debug.Log("✅ No conflicting cursor systems detected");
            }
            else
            {
                Debug.LogWarning($"⚠️ Found {conflictCount} potentially conflicting systems");
            }
        }
        
        [ContextMenu("Force Fix All Conflicts")]
        private void ForceFixAllConflicts()
        {
            Debug.Log("🔧 Force fixing all cursor conflicts...");
            
            DisableConflictingSystems();
            
            if (Application.isPlaying)
            {
                // Fix pause panel issues
                if (GamePauseSystem.Instance != null)
                {
                    // Force resume if paused
                    if (GamePauseSystem.Instance.IsPaused)
                    {
                        GamePauseSystem.Instance.ResumeGame();
                        Debug.Log("✅ Forced resume game");
                    }
                }
                
                // Fix cursor state
                if (UnifiedCursorManager.Instance != null)
                {
                    string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (sceneName == "StartScene" || sceneName == "HomeScene")
                    {
                        UnifiedCursorManager.Instance.ForceShowCursor();
                        Debug.Log("✅ Forced cursor to show for menu scene");
                    }
                    else
                    {
                        UnifiedCursorManager.Instance.ForceHideCursor();
                        Debug.Log("✅ Forced cursor to hide for gameplay scene");
                    }
                }
                
                // Reset time scale
                Time.timeScale = 1f;
                Debug.Log("✅ Reset time scale to 1");
            }
            
            Debug.Log("✅ Force fix completed");
        }
        
        [ContextMenu("Fix Exit Panel Issue")]
        private void FixExitPanelIssue()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("This fix can only be run in play mode");
                return;
            }
            
            Debug.Log("🔧 Fixing exit panel issue...");
            
            // Find and destroy any lingering pause panels
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            int destroyedCount = 0;
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("PausePanel") || obj.name.Contains("Pause Panel"))
                {
                    Debug.Log($"🗑️ Destroying lingering pause panel: {obj.name}");
                    Destroy(obj);
                    destroyedCount++;
                }
            }
            
            // Reset pause system state
            if (GamePauseSystem.Instance != null)
            {
                GamePauseSystem.Instance.ResumeGame();
                Debug.Log("✅ Reset GamePauseSystem state");
            }
            
            // Reset cursor manager state
            if (UnifiedCursorManager.Instance != null)
            {
                UnifiedCursorManager.Instance.ForceShowCursor();
                Debug.Log("✅ Reset UnifiedCursorManager state");
            }
            
            // Reset time scale
            Time.timeScale = 1f;
            
            Debug.Log($"✅ Fixed exit panel issue - destroyed {destroyedCount} lingering panels");
        }
        #endif
    }
}
