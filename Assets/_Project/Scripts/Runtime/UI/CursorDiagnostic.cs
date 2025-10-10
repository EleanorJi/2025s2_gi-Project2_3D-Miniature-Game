using UnityEngine;
using UnityEngine.UI;
using Antventure.UI.Animation;

namespace Antventure.UI
{
    /// <summary>
    /// Cursor diagnostic tool - helps find cursor display issues
    /// </summary>
    public class CursorDiagnostic : MonoBehaviour
    {
        [Header("Diagnostic Info")]
        [SerializeField] private Text diagnosticText;
        
        [Header("Test Button")]
        [SerializeField] private Button testButton;
        
        private void Start()
        {
            if (testButton != null)
            {
                // Add event listener to the test button
                var buttonAnimator = testButton.GetComponent<ButtonAnimator>();
                if (buttonAnimator == null)
                {
                    Debug.LogWarning("Test button does not have ButtonAnimator component!");
                }
            }
            
            RunDiagnostic();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                RunDiagnostic();
            }
        }
        
        [ContextMenu("Run Diagnostic")]
        public void RunDiagnostic()
        {
            string diagnostic = "CURSOR DIAGNOSTIC REPORT:\n\n";
            
            // Check CursorManager
            diagnostic += "1. CursorManager Check:\n";
            if (CursorManager.Instance != null)
            {
                diagnostic += "   [OK] CursorManager exists\n";
                
                // Check private fields through reflection
                var cursorManagerType = typeof(CursorManager);
                var handCursorField = cursorManagerType.GetField("handCursor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var useCustomCursorsField = cursorManagerType.GetField("useCustomCursors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (handCursorField != null)
                {
                    var handCursor = handCursorField.GetValue(CursorManager.Instance) as Texture2D;
                    if (handCursor != null)
                    {
                        diagnostic += $"   [OK] Hand cursor set: {handCursor.name}\n";
                        diagnostic += $"   [OK] Cursor size: {handCursor.width}x{handCursor.height}\n";
                        
                        // Check texture settings
                        #if UNITY_EDITOR
                        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(handCursor);
                        var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
                        if (importer != null)
                        {
                            if (importer.textureType == UnityEditor.TextureImporterType.Cursor)
                            {
                                diagnostic += "   [OK] Texture type set to Cursor\n";
                            }
                            else
                            {
                                diagnostic += $"   [ERROR] Wrong texture type: {importer.textureType} (should be Cursor)\n";
                            }
                            
                            if (importer.isReadable)
                            {
                                diagnostic += "   [OK] Read/Write Enabled is on\n";
                            }
                            else
                            {
                                diagnostic += "   [ERROR] Read/Write Enabled is off\n";
                            }
                        }
                        #endif
                    }
                    else
                    {
                        diagnostic += "   [ERROR] Hand cursor not set\n";
                    }
                }
                
                if (useCustomCursorsField != null)
                {
                    var useCustomCursors = (bool)useCustomCursorsField.GetValue(CursorManager.Instance);
                    if (useCustomCursors)
                    {
                        diagnostic += "   [OK] useCustomCursors enabled\n";
                    }
                    else
                    {
                        diagnostic += "   [ERROR] useCustomCursors disabled\n";
                    }
                }
            }
            else
            {
                diagnostic += "   [ERROR] CursorManager does not exist\n";
            }
            
            // Check current cursor state
            diagnostic += "\n2. Current Cursor State:\n";
            diagnostic += $"   Cursor.visible: {Cursor.visible}\n";
            diagnostic += $"   Cursor.lockState: {Cursor.lockState}\n";
            
            // Check buttons
            diagnostic += "\n3. Button Check:\n";
            var buttonAnimators = FindObjectsOfType<ButtonAnimator>();
            diagnostic += $"   ButtonAnimator count in scene: {buttonAnimators.Length}\n";
            
            if (buttonAnimators.Length > 0)
            {
                diagnostic += "   [OK] Found ButtonAnimator components\n";
            }
            else
            {
                diagnostic += "   [ERROR] No ButtonAnimator components found\n";
            }
            
            // Check EventSystem
            diagnostic += "\n4. EventSystem Check:\n";
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                diagnostic += "   [OK] EventSystem exists\n";
            }
            else
            {
                diagnostic += "   [ERROR] EventSystem does not exist\n";
            }
            
            diagnostic += "\nTest Keys:\n";
            diagnostic += "   H key: Manually set hand cursor\n";
            diagnostic += "   D key: Manually set default cursor\n";
            diagnostic += "   I key: Show cursor information\n";
            diagnostic += "   F1 key: Re-run diagnostic\n";
            
            Debug.Log(diagnostic);
            
            if (diagnosticText != null)
            {
                diagnosticText.text = diagnostic;
            }
        }
        
        [ContextMenu("Force Set Hand Cursor")]
        public void ForceSetHandCursor()
        {
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetHoverCursor();
            }
            else
            {
                Debug.LogError("CursorManager.Instance is null!");
            }
        }
    }
}
