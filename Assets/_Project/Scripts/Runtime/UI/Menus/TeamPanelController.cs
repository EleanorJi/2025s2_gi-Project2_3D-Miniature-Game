using UnityEngine;
using UnityEngine.UI;

namespace Antventure.UI.Menus
{
    /// <summary>
    /// Controls the team panel display functionality
    /// Handles opening/closing the team member list panel
    /// </summary>
    public class TeamPanelController : MonoBehaviour
    {
        [Header("Team Panel References")]
        [SerializeField] private GameObject teamPanel;
        [SerializeField] private Button backgroundButton; // The black background panel button
        [SerializeField] private Button closeButton; // Optional close button if you have one
        
        [Header("Audio Settings")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        private void Start()
        {
            // Ensure team panel is initially hidden
            if (teamPanel != null)
            {
                teamPanel.SetActive(false);
            }

            // Set up button listeners
            SetupButtonListeners();
        }

        /// <summary>
        /// Set up button event listeners
        /// </summary>
        private void SetupButtonListeners()
        {
            // If teamPanel is null, try to find it
            if (teamPanel == null)
            {
                FindTeamPanel();
            }

            // Add listener for background click to close panel
            if (backgroundButton != null)
            {
                backgroundButton.onClick.AddListener(CloseTeamPanel);
            }

            // Add listener for close button if it exists
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseTeamPanel);
            }
        }

        /// <summary>
        /// Find TeamPanel in Canvas hierarchy
        /// </summary>
        private void FindTeamPanel()
        {
            // Method 1: Try direct find first (for backward compatibility)
            teamPanel = GameObject.Find("TeamPanel");
            
            if (teamPanel == null)
            {
                // Method 2: Search in Canvas hierarchy
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    Transform teamPanelTransform = canvas.transform.Find("TeamPanel");
                    if (teamPanelTransform != null)
                    {
                        teamPanel = teamPanelTransform.gameObject;
                        Debug.Log(" Found TeamPanel in Canvas");
                    }
                    else
                    {
                        // Method 3: Search deeper in Canvas children
                        Transform[] allChildren = canvas.GetComponentsInChildren<Transform>(true);
                        foreach (Transform child in allChildren)
                        {
                            if (child.name == "TeamPanel")
                            {
                                teamPanel = child.gameObject;
                                Debug.Log($" Found TeamPanel in Canvas children: {GetGameObjectPath(teamPanel)}");
                                break;
                            }
                        }
                    }
                }
            }

            // Set up background button if TeamPanel was found
            if (teamPanel != null && backgroundButton == null)
            {
                backgroundButton = teamPanel.GetComponent<Button>();
                if (backgroundButton == null)
                {
                    backgroundButton = teamPanel.AddComponent<Button>();
                    Debug.Log(" Added Button component to TeamPanel");
                }
            }
        }

        /// <summary>
        /// Get full path of GameObject for debugging
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        /// <summary>
        /// Open the team panel
        /// </summary>
        public void OpenTeamPanel()
        {
            // If teamPanel is null, try to find it
            if (teamPanel == null)
            {
                FindTeamPanel();
            }

            if (teamPanel != null)
            {
                teamPanel.SetActive(true);
                PlaySound(openSound);
                
                Debug.Log("Team panel opened");
            }
            else
            {
                Debug.LogWarning("Team panel reference is not assigned and could not be found!");
                Debug.LogWarning("Please ensure TeamPanel exists in the Canvas hierarchy");
            }
        }

        /// <summary>
        /// Close the team panel
        /// </summary>
        public void CloseTeamPanel()
        {
            if (teamPanel != null && teamPanel.activeInHierarchy)
            {
                teamPanel.SetActive(false);
                PlaySound(closeSound);
                
                Debug.Log("Team panel closed");
            }
        }

        /// <summary>
        /// Toggle team panel visibility
        /// </summary>
        public void ToggleTeamPanel()
        {
            if (teamPanel != null)
            {
                if (teamPanel.activeInHierarchy)
                {
                    CloseTeamPanel();
                }
                else
                {
                    OpenTeamPanel();
                }
            }
        }

        /// <summary>
        /// Play UI sound effect
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
            {
                uiAudioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Handle ESC key to close panel
        /// </summary>
        private void Update()
        {
            // Allow ESC key to close the team panel
            if (Input.GetKeyDown(KeyCode.Escape) && teamPanel != null && teamPanel.activeInHierarchy)
            {
                CloseTeamPanel();
            }
        }

        /// <summary>
        /// Clean up event listeners when destroyed
        /// </summary>
        private void OnDestroy()
        {
            if (backgroundButton != null)
            {
                backgroundButton.onClick.RemoveListener(CloseTeamPanel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseTeamPanel);
            }
        }
    }
}
