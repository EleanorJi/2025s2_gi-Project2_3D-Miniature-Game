using UnityEngine;
using Antventure.UI.Menus;

namespace Antventure.UI
{
    /// <summary>
    /// Aggressive cursor controller for gameplay scenes
    /// Ensures cursor stays hidden no matter what
    /// </summary>
    public class GameplayCursorController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool aggressiveMode = true;
        [SerializeField] private bool logWarnings = true;
        
        private int framesSinceLastHide = 0;
        
        private void Start()
        {
            // Immediately hide cursor
            HideCursorCompletely();
            Debug.Log("[GAMEPLAY CURSOR] GameplayCursorController started - cursor hidden");
        }
        
        void Update()
        {
            // Check if UnifiedCursorManager exists and pause menu is open
            if (UnifiedCursorManager.Instance != null && UnifiedCursorManager.Instance.IsPauseMenuOpen())
            {
                // Don't interfere when pause menu is open
                return;
            }
            
            // Check if GamePauseSystem exists and is paused
            if (GamePauseSystem.Instance != null && GamePauseSystem.Instance.IsPaused)
            {
                // Don't interfere when game is paused
                return;
            }
            
            if (aggressiveMode)
            {
                // Check every frame if cursor is visible
                if (Cursor.visible)
                {
                    if (logWarnings)
                    {
                        Debug.LogWarning($"[GAMEPLAY CURSOR] Frame {Time.frameCount}: Cursor became visible! Hiding immediately");
                    }
                    HideCursorCompletely();
                    framesSinceLastHide = 0;
                }
                else
                {
                    framesSinceLastHide++;
                }
                
                // Also check lock state
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    if (logWarnings)
                    {
                        Debug.LogWarning($"[GAMEPLAY CURSOR] Frame {Time.frameCount}: Cursor lock state changed to {Cursor.lockState}! Forcing lock");
                    }
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
        
        private void LateUpdate()
        {
            // Check if UnifiedCursorManager exists and pause menu is open
            if (UnifiedCursorManager.Instance != null && UnifiedCursorManager.Instance.IsPauseMenuOpen())
            {
                // Don't interfere when pause menu is open
                return;
            }
            
            // Check if GamePauseSystem exists and is paused
            if (GamePauseSystem.Instance != null && GamePauseSystem.Instance.IsPaused)
            {
                // Don't interfere when game is paused
                return;
            }
            
            // Final check at end of frame
            if (aggressiveMode && Cursor.visible)
            {
                if (logWarnings)
                {
                    Debug.LogWarning("[GAMEPLAY CURSOR] LateUpdate: Cursor still visible, forcing hide");
                }
                HideCursorCompletely();
            }
        }
        
        private void HideCursorCompletely()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.SetCursor(null, Vector2.zero, GetCursorMode());
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
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                // Re-hide cursor when window regains focus
                HideCursorCompletely();
                Debug.Log("[GAMEPLAY CURSOR] Application regained focus - cursor hidden");
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                // Re-hide cursor when unpausing
                HideCursorCompletely();
                Debug.Log("[GAMEPLAY CURSOR] Application unpaused - cursor hidden");
            }
        }
        
        public void ForceHideCursor()
        {
            HideCursorCompletely();
            Debug.Log("[GAMEPLAY CURSOR] ForceHideCursor called");
        }
        
        public void SetAggressiveMode(bool aggressive)
        {
            aggressiveMode = aggressive;
            Debug.Log($"[GAMEPLAY CURSOR] Aggressive mode set to: {aggressive}");
            
            if (aggressive)
            {
                HideCursorCompletely();
            }
        }
        
        [ContextMenu("Force Hide Cursor")]
        public void ContextMenuForceHide()
        {
            ForceHideCursor();
        }
        
        [ContextMenu("Show Debug Info")]
        public void ShowDebugInfo()
        {
            Debug.Log($"[GAMEPLAY CURSOR] Debug Info:");
            Debug.Log($"  Cursor.visible: {Cursor.visible}");
            Debug.Log($"  Cursor.lockState: {Cursor.lockState}");
            Debug.Log($"  aggressiveMode: {aggressiveMode}");
            Debug.Log($"  framesSinceLastHide: {framesSinceLastHide}");
        }
    }
}
