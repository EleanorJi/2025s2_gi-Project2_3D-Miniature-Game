using UnityEngine;

namespace Antventure.UI
{
    public class CursorManager : MonoBehaviour
    {
        [Header("Cursor Textures")]
        [SerializeField] private Texture2D handCursor; // Hand-shaped cursor
        [SerializeField] private Texture2D defaultCursor; //Default arrow cursor
        
        [Header("Cursor Settings")]
        [SerializeField] private bool useCustomCursors = true; // Use custom cursor
        [SerializeField] private Vector2 handHotspot = new Vector2(16, 16); // The position of the cursor hotspot for the hand shape
        [SerializeField] private Vector2 defaultHotspot = new Vector2(0, 0); // Default cursor hotspot position
        
        public static CursorManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Load cursor textures from resources if not assigned
            LoadCursorTextures();
            
            SetDefaultCursor();
        }

        /// <summary>
        /// Set system default cursor
        /// </summary>
        public void SetDefaultCursor()
        {
            isGameplayMode = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Use custom default cursor if available, otherwise system cursor
            CursorMode cursorMode = GetCursorMode();
            if (defaultCursor != null && useCustomCursors)
            {
                Cursor.SetCursor(defaultCursor, defaultHotspot, cursorMode);
                Debug.Log("[CURSOR] Setting custom default cursor: " + defaultCursor.name);
            }
            else
            {
                // Use system default cursor
                Cursor.SetCursor(null, Vector2.zero, cursorMode);
                Debug.Log("[CURSOR] Using system default cursor");
            }
        }

        /// <summary>
        /// Set hover cursor (custom hand cursor or system default)
        /// </summary>
        public void SetHoverCursor()
        {
            Debug.Log("[CURSOR] SetHoverCursor() called!");
            Debug.Log($"[CURSOR] handCursor is null: {handCursor == null}");
            Debug.Log($"[CURSOR] useCustomCursors: {useCustomCursors}");
            
            isGameplayMode = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Always use custom hand cursor if available
            if (handCursor != null && useCustomCursors)
            {
                Debug.Log($"[CURSOR] Setting hand cursor: {handCursor.name}");
                Debug.Log($"[CURSOR] Cursor size: {handCursor.width}x{handCursor.height}");
                Debug.Log($"[CURSOR] Hotspot position: {handHotspot}");
                
                Cursor.SetCursor(handCursor, handHotspot, GetCursorMode());
                
                // Verify if setting was successful
                Debug.Log($"[CURSOR] After setting - Cursor.visible: {Cursor.visible}");
            }
            else
            {
                Debug.LogWarning("[CURSOR] Hand cursor not set or useCustomCursors is false");
                Debug.LogWarning($"[CURSOR] handCursor: {(handCursor != null ? handCursor.name : "null")}");
                Debug.LogWarning($"[CURSOR] useCustomCursors: {useCustomCursors}");
                
                // Fallback to system cursor
                Cursor.SetCursor(null, Vector2.zero, GetCursorMode());
            }
        }

        /// <summary>
        /// Hide cursor for gameplay
        /// </summary>
        public void HideCursor()
        {
            Debug.Log("[CURSOR] HideCursor() called - hiding cursor for gameplay");
            isGameplayMode = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Debug.Log($"[CURSOR] After hiding - Cursor.visible: {Cursor.visible}, lockState: {Cursor.lockState}");
        }
        
        /// <summary>
        /// Force hide cursor immediately (for debugging)
        /// </summary>
        public void ForceHideCursor()
        {
            Debug.Log("[CURSOR] ForceHideCursor() called - forcing cursor to hide");
            isGameplayMode = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            // Also set cursor to null to remove any custom cursor
            Cursor.SetCursor(null, Vector2.zero, GetCursorMode());
            Debug.Log("[CURSOR] Cursor forcefully hidden");
        }

        /// <summary>
        /// Show cursor for UI scenes
        /// </summary>
        public void ShowCursor()
        {
            Debug.Log("[CURSOR] ShowCursor() called - showing cursor for UI");
            isGameplayMode = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.SetCursor(null, Vector2.zero, GetCursorMode());
        }

        // Cursor state tracking
        private bool isGameplayMode = false;
        
        /// <summary>
        /// manual test cursor functionality and force cursor hiding during gameplay
        /// </summary>
        private void Update()
        {
            // Force cursor to stay hidden during gameplay if needed
            if (isGameplayMode)
            {
                // Check if cursor became visible
                if (Cursor.visible)
                {
                    Debug.LogWarning("[CURSOR] Cursor became visible during gameplay! Force hiding...");
                    Cursor.visible = false;
                }
                
                // Check if cursor lock state changed
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Debug.LogWarning("[CURSOR] Cursor lock state changed during gameplay! Force locking...");
                    Cursor.lockState = CursorLockMode.Locked;
                }
                
                // Detect any input that might cause cursor to show
                if (Input.anyKeyDown || Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
                {
                    // Immediately force cursor to stay hidden after any input
                    if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                }
            }
            
            // Test keys (only work when not in gameplay mode to avoid interference)
            if (!isGameplayMode)
            {
                if (Input.GetKeyDown(KeyCode.H))
                {
                    Debug.Log("[TEST] Manual test: set hover cursor");
                    SetHoverCursor();
                }
                
                if (Input.GetKeyDown(KeyCode.D))
                {
                    Debug.Log("[TEST] Manual test: set default cursor");
                    SetDefaultCursor();
                }
            }
            
            // Info and force hide keys always work
            if (Input.GetKeyDown(KeyCode.I))
            {
                Debug.Log("[TEST] Cursor information check:");
                Debug.Log($"   CursorManager.Instance: {Instance != null}");
                Debug.Log($"   handCursor: {(handCursor != null ? handCursor.name : "null")}");
                Debug.Log($"   useCustomCursors: {useCustomCursors}");
                Debug.Log($"   Cursor.visible: {Cursor.visible}");
                Debug.Log($"   Cursor.lockState: {Cursor.lockState}");
                Debug.Log($"   isGameplayMode: {isGameplayMode}");
            }
            
            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("[TEST] Force hide cursor");
                ForceHideCursor();
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
        /// Load cursor textures from the project assets
        /// </summary>
        private void LoadCursorTextures()
        {
            // Try to load existing cursor textures
            if (handCursor == null)
            {
                // Try to load from the project's cursor folder
                handCursor = Resources.Load<Texture2D>("UI/Cursor/hand");
                if (handCursor == null)
                {
                    // Fallback: try to load from Assets folder directly
                    handCursor = LoadTextureFromAssets("Assets/_Project/Art/UI/Cursor/hand 2.png");
                }
            }
            
            if (defaultCursor == null)
            {
                defaultCursor = Resources.Load<Texture2D>("UI/Cursor/cursor");
                if (defaultCursor == null)
                {
                    defaultCursor = LoadTextureFromAssets("Assets/_Project/Art/UI/Cursor/cursor 1.png");
                }
            }
            
            // If still no hand cursor, create a simple one
            if (handCursor == null)
            {
                CreateDefaultHandCursor();
            }
        }

        /// <summary>
        /// Load texture from Assets folder (Editor only, fallback method)
        /// </summary>
        private Texture2D LoadTextureFromAssets(string path)
        {
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            #else
            return null;
            #endif
        }

        /// <summary>
        /// Create a simple default hand cursor texture as fallback
        /// </summary>
        private void CreateDefaultHandCursor()
        {
            // Create a simple 32x32 hand cursor texture
            int size = 32;
            handCursor = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            // Create a simple hand shape using pixels
            Color32[] pixels = new Color32[size * size];
            
            // Initialize all pixels to transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }
            
            // Draw a simple hand shape (white with black outline)
            // This is a very basic hand cursor - you can replace with a proper texture
            DrawHandShape(pixels, size);
            
            handCursor.SetPixels32(pixels);
            handCursor.Apply();
            
            // Set hotspot to the tip of the finger
            handHotspot = new Vector2(10, 2);
        }

        /// <summary>
        /// Draw a simple hand shape on the cursor texture
        /// </summary>
        private void DrawHandShape(Color32[] pixels, int size)
        {
            // Define hand shape coordinates (simplified)
            // This creates a basic pointing hand cursor
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 black = new Color32(0, 0, 0, 255);
            
            // Draw finger (pointing up)
            for (int y = 2; y < 18; y++)
            {
                for (int x = 8; x < 12; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        pixels[y * size + x] = white;
                    }
                }
            }
            
            // Draw palm
            for (int y = 18; y < 28; y++)
            {
                for (int x = 6; x < 20; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        pixels[y * size + x] = white;
                    }
                }
            }
            
            // Draw thumb
            for (int y = 20; y < 26; y++)
            {
                for (int x = 4; x < 8; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        pixels[y * size + x] = white;
                    }
                }
            }
            
            // Add black outline for better visibility
            // This is a simplified outline - you might want to improve this
            AddOutline(pixels, size, white, black);
        }

        /// <summary>
        /// Add a black outline to the cursor for better visibility
        /// </summary>
        private void AddOutline(Color32[] pixels, int size, Color32 fillColor, Color32 outlineColor)
        {
            Color32[] originalPixels = new Color32[pixels.Length];
            System.Array.Copy(pixels, originalPixels, pixels.Length);
            
            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    int index = y * size + x;
                    
                    // If current pixel is transparent, check if it should be outline
                    if (originalPixels[index].a == 0)
                    {
                        // Check 8 surrounding pixels
                        bool hasFilledNeighbor = false;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                                {
                                    int neighborIndex = ny * size + nx;
                                    if (originalPixels[neighborIndex].a > 0)
                                    {
                                        hasFilledNeighbor = true;
                                        break;
                                    }
                                }
                            }
                            if (hasFilledNeighbor) break;
                        }
                        
                        if (hasFilledNeighbor)
                        {
                            pixels[index] = outlineColor;
                        }
                    }
                }
            }
        }
    }
}