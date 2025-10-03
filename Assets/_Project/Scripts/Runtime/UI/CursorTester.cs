using UnityEngine;
using UnityEngine.UI;

namespace Antventure.UI
{
    /// <summary>
    /// Simple cursor tester for debugging cursor functionality
    /// Attach this to a UI element to test cursor behavior
    /// </summary>
    public class CursorTester : MonoBehaviour
    {
        [Header("Debug Info")]
        [SerializeField] private Text debugText;
        
        private void Start()
        {
            UpdateDebugInfo();
        }
        
        private void Update()
        {
            // Update debug info every few frames
            if (Time.frameCount % 30 == 0)
            {
                UpdateDebugInfo();
            }
            
            // Test keys for manual cursor control
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (CursorManager.Instance != null)
                {
                    CursorManager.Instance.SetDefaultCursor();
                    Debug.Log("Set Default Cursor");
                }
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (CursorManager.Instance != null)
                {
                    CursorManager.Instance.SetHoverCursor();
                    Debug.Log("Set Hover Cursor");
                }
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (CursorManager.Instance != null)
                {
                    CursorManager.Instance.HideCursor();
                    Debug.Log("Hide Cursor");
                }
            }
        }
        
        private void UpdateDebugInfo()
        {
            if (debugText != null)
            {
                string info = $"Cursor Manager: {(CursorManager.Instance != null ? "Active" : "Missing")}\n";
                info += $"Cursor Visible: {Cursor.visible}\n";
                info += $"Lock State: {Cursor.lockState}\n";
                info += $"Press 1: Default, 2: Hover, 3: Hide";
                
                debugText.text = info;
            }
        }
    }
}

