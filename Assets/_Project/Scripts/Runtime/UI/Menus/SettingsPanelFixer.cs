using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Antventure.UI.Menus
{
    /// <summary>
    /// Settings Panel Fixer - Adds missing functionality to settings panel buttons
    /// Attach this to your SettingsPanel to automatically fix button issues
    /// </summary>
    public class SettingsPanelFixer : MonoBehaviour
    {
        [Header("Settings References")]
        [SerializeField] private GameSettingsManager gameSettingsManager;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;
        
        [Header("Auto-Find Settings")]
        [SerializeField] private bool autoFindComponents = true;
        
        private void Start()
        {
            if (autoFindComponents)
            {
                AutoFindComponents();
            }
            
            SetupButtonEvents();
            AddCursorHandlers();
        }
        
        /// <summary>
        /// Automatically find components in the settings panel
        /// </summary>
        private void AutoFindComponents()
        {
            // Find GameSettingsManager if not assigned
            if (gameSettingsManager == null)
            {
                gameSettingsManager = FindObjectOfType<GameSettingsManager>();
            }
            
            // Find buttons by name
            Button[] buttons = GetComponentsInChildren<Button>();
            
            foreach (Button button in buttons)
            {
                if (button.name.ToLower().Contains("reset") && resetButton == null)
                {
                    resetButton = button;
                    Debug.Log($"Found Reset Button: {button.name}");
                }
                else if (button.name.ToLower().Contains("close") && closeButton == null)
                {
                    closeButton = button;
                    Debug.Log($"Found Close Button: {button.name}");
                }
            }
        }
        
        /// <summary>
        /// Setup button click events
        /// </summary>
        private void SetupButtonEvents()
        {
            if (resetButton != null && gameSettingsManager != null)
            {
                // Clear existing listeners and add reset function
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(() => {
                    gameSettingsManager.ResetToDefaults();
                    Debug.Log("Reset button clicked - settings reset to defaults");
                });
                
                Debug.Log("Reset button event connected successfully");
            }
            else
            {
                Debug.LogWarning($"Cannot setup reset button - resetButton: {resetButton != null}, gameSettingsManager: {gameSettingsManager != null}");
            }
            
            if (closeButton != null && gameSettingsManager != null)
            {
                // Clear existing listeners and add close function
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => {
                    gameSettingsManager.CloseSettings();
                    Debug.Log("Close button clicked - settings panel closed");
                });
                
                Debug.Log("Close button event connected successfully");
            }
            else
            {
                Debug.LogWarning($"Cannot setup close button - closeButton: {closeButton != null}, gameSettingsManager: {gameSettingsManager != null}");
            }
        }
        
        /// <summary>
        /// Add cursor hover handlers to all buttons
        /// </summary>
        private void AddCursorHandlers()
        {
            Button[] allButtons = GetComponentsInChildren<Button>();
            
            foreach (Button button in allButtons)
            {
                // Check if button already has cursor handler
                if (button.GetComponent<ButtonCursorHandler>() == null)
                {
                    button.gameObject.AddComponent<ButtonCursorHandler>();
                    Debug.Log($"Added cursor handler to button: {button.name}");
                }
            }
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Fix All Button Issues")]
        private void FixAllButtonIssues()
        {
            AutoFindComponents();
            SetupButtonEvents();
            AddCursorHandlers();
            Debug.Log("All button issues fixed!");
        }
        
        [ContextMenu("Test Reset Button")]
        private void TestResetButton()
        {
            if (gameSettingsManager != null)
            {
                gameSettingsManager.ResetToDefaults();
                Debug.Log("Reset button test completed");
            }
            else
            {
                Debug.LogError("GameSettingsManager not found!");
            }
        }
        #endif
    }
    
    /// <summary>
    /// Button Cursor Handler - Handles cursor changes for individual buttons
    /// </summary>
    public class ButtonCursorHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Change cursor to hand/pointer
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetHoverCursor();
            }
            else
            {
                // Fallback: Use system hand cursor
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            // Reset cursor to default
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetDefaultCursor();
            }
            else
            {
                // Fallback: Use system default cursor
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
    }
}
