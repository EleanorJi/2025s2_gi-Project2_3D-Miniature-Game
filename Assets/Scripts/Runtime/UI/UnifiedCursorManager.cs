using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Antventure.UI
{
    /// <summary>
    /// Unified Cursor Manager - The ultimate cursor management system
    /// This system has the highest priority and overrides all other cursor management
    /// </summary>
    public class UnifiedCursorManager : MonoBehaviour
    {
        public static UnifiedCursorManager Instance { get; private set; }
        
        [Header("Cursor Textures")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D hoverCursor;
        [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
        
        [Header("Settings")]
        [SerializeField] private bool useCustomCursors = true;
        [SerializeField] private bool debugMode = true;
        
        // Cursor states
        public enum CursorState
        {
            Hidden,      // Completely hidden (gameplay)
            Default,     // Normal arrow cursor (menus)
            Hover        // Hover/hand cursor (buttons)
        }
        
        // Current state
        private CursorState currentState = CursorState.Default;
        private bool isPauseMenuOpen = false;
        private bool forceOverride = false;
        
        // Coroutine for continuous enforcement
        private Coroutine enforcementCoroutine;
        
        private void Awake()
        {
            // Singleton with highest priority
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Disable all other cursor managers
                DisableOtherCursorManagers();
                
                InitializeCursors();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Subscribe to scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Start enforcement coroutine
            enforcementCoroutine = StartCoroutine(EnforceCursorState());
            
            // Set initial state based on current scene
            UpdateCursorForCurrentScene();
        }
        
        private void Update()
        {
            // Super aggressive enforcement when pause menu is open
            if (isPauseMenuOpen)
            {
                // Force cursor to be visible and unlocked every frame
                if (!Cursor.visible)
                {
                    Cursor.visible = true;
                    if (debugMode) Debug.Log("[UNIFIED CURSOR] Update: Forced cursor visible");
                }
                
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    if (debugMode) Debug.Log("[UNIFIED CURSOR] Update: Forced cursor unlocked");
                }
            }
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            if (enforcementCoroutine != null)
            {
                StopCoroutine(enforcementCoroutine);
            }
        }
        
        /// <summary>
        /// Get appropriate cursor mode based on platform
        /// ForceSoftware for WebGL to avoid DPI scaling issues
        /// </summary>
        private CursorMode GetCursorMode()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            return CursorMode.ForceSoftware;
            #else
            return CursorMode.Auto;
            #endif
        }
        
        /// <summary>
        /// Initialize cursor textures
        /// </summary>
        private void InitializeCursors()
        {
            // Try to get cursors from existing CursorManager if available
            if (CursorManager.Instance != null)
            {
                var cursorManagerType = typeof(CursorManager);
                
                try
                {
                    var handCursorField = cursorManagerType.GetField("handCursor", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var defaultCursorField = cursorManagerType.GetField("defaultCursor", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (handCursorField != null && hoverCursor == null)
                    {
                        hoverCursor = handCursorField.GetValue(CursorManager.Instance) as Texture2D;
                    }
                    
                    if (defaultCursorField != null && defaultCursor == null)
                    {
                        defaultCursor = defaultCursorField.GetValue(CursorManager.Instance) as Texture2D;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[UNIFIED CURSOR] Could not access CursorManager fields: {e.Message}");
                }
            }
            
            if (debugMode)
            {
                Debug.Log($"[UNIFIED CURSOR] Initialized - Default: {(defaultCursor != null ? defaultCursor.name : "null")}, Hover: {(hoverCursor != null ? hoverCursor.name : "null")}");
            }
        }
        
        /// <summary>
        /// Disable all other cursor management systems
        /// </summary>
        private void DisableOtherCursorManagers()
        {
            // Disable CursorManager
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.enabled = false;
                if (debugMode) Debug.Log("[UNIFIED CURSOR] Disabled CursorManager");
            }
            
            // Disable GameplayCursorController
            GameplayCursorController[] gameplayCursors = FindObjectsOfType<GameplayCursorController>();
            foreach (var cursor in gameplayCursors)
            {
                cursor.enabled = false;
                if (debugMode) Debug.Log("[UNIFIED CURSOR] Disabled GameplayCursorController");
            }
        }
        
        /// <summary>
        /// Continuous enforcement of cursor state
        /// </summary>
        private IEnumerator EnforceCursorState()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.05f); // Check every 50ms for more aggressive enforcement
                
                // Always enforce when pause menu is open
                if (isPauseMenuOpen)
                {
                    EnforceCursorStateImmediate();
                }
                else if (forceOverride)
                {
                    EnforceCursorStateImmediate();
                }
            }
        }
        
        /// <summary>
        /// Immediately enforce the current cursor state
        /// </summary>
        private void EnforceCursorStateImmediate()
        {
            switch (currentState)
            {
                case CursorState.Hidden:
                {
                    if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                        if (debugMode) Debug.Log("[UNIFIED CURSOR] Enforced Hidden state");
                    }
                    break;
                }
                    
                case CursorState.Default:
                {
                    if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;
                        
                        CursorMode cursorMode = GetCursorMode();
                        if (useCustomCursors && defaultCursor != null)
                        {
                            Cursor.SetCursor(defaultCursor, cursorHotspot, cursorMode);
                        }
                        else
                        {
                            Cursor.SetCursor(null, Vector2.zero, cursorMode);
                        }
                        
                        if (debugMode) Debug.Log("[UNIFIED CURSOR] Enforced Default state");
                    }
                    break;
                }
                    
                case CursorState.Hover:
                {
                    if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;
                    }
                    
                    CursorMode cursorMode = GetCursorMode();
                    if (useCustomCursors && hoverCursor != null)
                    {
                        Cursor.SetCursor(hoverCursor, cursorHotspot, cursorMode);
                    }
                    else
                    {
                        Cursor.SetCursor(null, Vector2.zero, cursorMode);
                    }
                    
                    if (debugMode) Debug.Log("[UNIFIED CURSOR] Enforced Hover state");
                    break;
                }
            }
        }
        
        /// <summary>
        /// Handle scene loaded event
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reset pause menu state when changing scenes
            isPauseMenuOpen = false;
            forceOverride = false;
            
            // Re-disable other cursor managers in new scene
            StartCoroutine(DisableOtherCursorManagersDelayed());
            
            // Update cursor for new scene
            UpdateCursorForCurrentScene();
            
            if (debugMode)
            {
                Debug.Log($"[UNIFIED CURSOR] Scene loaded: {scene.name}, reset pause menu state");
            }
        }
        
        /// <summary>
        /// Disable other cursor managers with delay
        /// </summary>
        private IEnumerator DisableOtherCursorManagersDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            DisableOtherCursorManagers();
        }
        
        /// <summary>
        /// Update cursor state based on current scene
        /// </summary>
        private void UpdateCursorForCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            
            if (isPauseMenuOpen)
            {
                // Pause menu is open, keep current state
                return;
            }
            
            if (IsGameplayScene(sceneName))
            {
                SetCursorState(CursorState.Hidden);
            }
            else
            {
                SetCursorState(CursorState.Default);
            }
        }
        
        /// <summary>
        /// Check if scene is a gameplay scene
        /// </summary>
        private bool IsGameplayScene(string sceneName)
        {
            return sceneName != "StartScene" && sceneName != "HomeScene";
        }
        
        /// <summary>
        /// Set cursor state with immediate enforcement
        /// </summary>
        public void SetCursorState(CursorState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                EnforceCursorStateImmediate();
                
                if (debugMode)
                {
                    Debug.Log($"[UNIFIED CURSOR] State changed to: {newState}");
                }
            }
        }
        
        /// <summary>
        /// Show cursor for pause menu
        /// </summary>
        public void ShowPauseMenuCursor()
        {
            isPauseMenuOpen = true;
            forceOverride = true;
            SetCursorState(CursorState.Default);
            
            if (debugMode)
            {
                Debug.Log("[UNIFIED CURSOR] Pause menu opened - showing cursor");
            }
        }
        
        /// <summary>
        /// Hide cursor after pause menu closes
        /// </summary>
        public void HidePauseMenuCursor()
        {
            isPauseMenuOpen = false;
            forceOverride = false;
            
            // Return to scene-appropriate state
            UpdateCursorForCurrentScene();
            
            if (debugMode)
            {
                Debug.Log("[UNIFIED CURSOR] Pause menu closed - updating cursor for scene");
            }
        }
        
        /// <summary>
        /// Set hover cursor (for button interactions)
        /// </summary>
        public void SetHoverCursor()
        {
            if (isPauseMenuOpen)
            {
                SetCursorState(CursorState.Hover);
            }
        }
        
        /// <summary>
        /// Set default cursor (when leaving buttons)
        /// </summary>
        public void SetDefaultCursor()
        {
            if (isPauseMenuOpen)
            {
                SetCursorState(CursorState.Default);
            }
        }
        
        /// <summary>
        /// Force hide cursor (for gameplay)
        /// </summary>
        public void ForceHideCursor()
        {
            isPauseMenuOpen = false;
            forceOverride = false;
            SetCursorState(CursorState.Hidden);
        }
        
        /// <summary>
        /// Force show cursor (for menus)
        /// </summary>
        public void ForceShowCursor()
        {
            SetCursorState(CursorState.Default);
        }
        
        /// <summary>
        /// Get current cursor state
        /// </summary>
        public CursorState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// Check if pause menu is open
        /// </summary>
        public bool IsPauseMenuOpen()
        {
            return isPauseMenuOpen;
        }
        
        #if UNITY_EDITOR
        [Header("Debug Info")]
        [SerializeField] private bool showDebugGUI = true;
        
        private void OnGUI()
        {
            if (!showDebugGUI || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box("Unified Cursor Manager Debug");
            
            GUILayout.Label($"Current State: {currentState}");
            GUILayout.Label($"Pause Menu Open: {isPauseMenuOpen}");
            GUILayout.Label($"Force Override: {forceOverride}");
            GUILayout.Label($"Cursor Visible: {Cursor.visible}");
            GUILayout.Label($"Lock State: {Cursor.lockState}");
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");
            
            if (GUILayout.Button("Force Hidden"))
            {
                ForceHideCursor();
            }
            
            if (GUILayout.Button("Force Show"))
            {
                ForceShowCursor();
            }
            
            if (GUILayout.Button("Show Pause Cursor"))
            {
                ShowPauseMenuCursor();
            }
            
            if (GUILayout.Button("Hide Pause Cursor"))
            {
                HidePauseMenuCursor();
            }
            
            GUILayout.EndArea();
        }
        #endif
    }
}
