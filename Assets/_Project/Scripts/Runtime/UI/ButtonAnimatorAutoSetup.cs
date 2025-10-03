using UnityEngine;
using UnityEngine.UI;
using Antventure.UI.Animation;

namespace Antventure.UI
{
    /// <summary>
    /// Automatically adds ButtonAnimator to all buttons in the scene
    /// </summary>
    public class ButtonAnimatorAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnStart = true;
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupAllButtons();
            }
        }
        
        [ContextMenu("Setup All Buttons")]
        public void SetupAllButtons()
        {
            // Find all buttons in the scene
            Button[] allButtons = FindObjectsOfType<Button>();
            
            int addedCount = 0;
            int existingCount = 0;
            
            foreach (Button button in allButtons)
            {
                // Check if ButtonAnimator already exists
                ButtonAnimator existingAnimator = button.GetComponent<ButtonAnimator>();
                
                if (existingAnimator == null)
                {
                    // Add ButtonAnimator component
                    ButtonAnimator newAnimator = button.gameObject.AddComponent<ButtonAnimator>();
                    addedCount++;
                    
                    Debug.Log($"[SETUP] Added ButtonAnimator to: {button.gameObject.name}");
                }
                else
                {
                    existingCount++;
                    Debug.Log($"[SETUP] ButtonAnimator already exists on: {button.gameObject.name}");
                }
            }
            
            Debug.Log($"[SETUP] Setup complete! Added: {addedCount}, Existing: {existingCount}, Total buttons: {allButtons.Length}");
        }
        
        [ContextMenu("List All Buttons")]
        public void ListAllButtons()
        {
            Button[] allButtons = FindObjectsOfType<Button>();
            
            Debug.Log($"[SETUP] Found {allButtons.Length} buttons in scene:");
            
            foreach (Button button in allButtons)
            {
                ButtonAnimator animator = button.GetComponent<ButtonAnimator>();
                string status = animator != null ? "[HAS ANIMATOR]" : "[NO ANIMATOR]";
                Debug.Log($"[SETUP] {status} {button.gameObject.name}");
            }
        }
    }
}
