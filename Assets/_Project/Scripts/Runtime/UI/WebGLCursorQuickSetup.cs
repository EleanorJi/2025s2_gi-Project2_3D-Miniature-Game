using UnityEngine;

namespace Antventure.UI
{
    /// <summary>
    /// Quick setup tool for WebGL cursor fix
    /// </summary>
    public class WebGLCursorQuickSetup : MonoBehaviour
    {
        [Header("Auto Setup on Start")]
        [SerializeField] private bool setupOnStart = false;
        
        #if UNITY_EDITOR
        [ContextMenu("🚀 Quick Setup WebGL Cursor Fix")]
        private void QuickSetup()
        {
            Debug.Log("🚀 Setting up WebGL Cursor Fix...");
            
            // Step 1: Find or create WebGLCursorFix
            WebGLCursorFix cursorFix = FindObjectOfType<WebGLCursorFix>();
            
            if (cursorFix == null)
            {
                GameObject fixGO = new GameObject("WebGLCursorFix");
                cursorFix = fixGO.AddComponent<WebGLCursorFix>();
                Debug.Log("✅ Created WebGLCursorFix GameObject");
            }
            else
            {
                Debug.Log("✅ Found existing WebGLCursorFix");
            }
            
            // Step 2: Try to load cursor textures
            Texture2D[] cursorTextures = LoadCursorTextures();
            
            if (cursorTextures[0] != null || cursorTextures[1] != null)
            {
                var cursorFixType = typeof(WebGLCursorFix);
                
                if (cursorTextures[0] != null)
                {
                    var defaultCursorField = cursorFixType.GetField("originalDefaultCursor", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    defaultCursorField?.SetValue(cursorFix, cursorTextures[0]);
                    Debug.Log($"✅ Assigned default cursor: {cursorTextures[0].name}");
                }
                
                if (cursorTextures[1] != null)
                {
                    var hoverCursorField = cursorFixType.GetField("originalHoverCursor", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    hoverCursorField?.SetValue(cursorFix, cursorTextures[1]);
                    Debug.Log($"✅ Assigned hover cursor: {cursorTextures[1].name}");
                }
                
                // Set recommended WebGL size
                var sizeField = cursorFixType.GetField("webGLCursorSize", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                sizeField?.SetValue(cursorFix, 32);
                
                // Enable scaling
                var enableField = cursorFixType.GetField("useScaledCursorsInWebGL", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                enableField?.SetValue(cursorFix, true);
                
                Debug.Log("✅ Configured WebGL settings (size: 32x32, scaling: enabled)");
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find cursor textures automatically");
                Debug.LogWarning("Please manually assign cursor textures in the WebGLCursorFix component");
            }
            
            Debug.Log("✅ Quick Setup Completed!");
            Debug.Log("📝 Next steps:");
            Debug.Log("   1. Check the WebGLCursorFix component settings");
            Debug.Log("   2. Assign cursor textures if not done automatically");
            Debug.Log("   3. Build WebGL and test in browser");
        }
        
        [ContextMenu("🔍 Find Cursor Textures")]
        private void FindCursorTextures()
        {
            Debug.Log("🔍 Searching for cursor textures...");
            
            // Search in common locations
            string[] searchPaths = new string[]
            {
                "Assets/_Project/Art/UI/Cursor/",
                "Assets/Art/Cursor/",
                "Assets/Cursors/",
                "Assets/UI/Cursor/"
            };
            
            foreach (string path in searchPaths)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                
                if (guids.Length > 0)
                {
                    Debug.Log($"📁 Found {guids.Length} textures in: {path}");
                    
                    foreach (string guid in guids)
                    {
                        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                        
                        if (texture != null)
                        {
                            Debug.Log($"   - {texture.name} ({texture.width}x{texture.height})");
                        }
                    }
                }
            }
        }
        
        [ContextMenu("📏 Analyze Current Cursors")]
        private void AnalyzeCurrentCursors()
        {
            Debug.Log("=== Cursor Analysis ===");
            
            Texture2D[] cursors = LoadCursorTextures();
            
            if (cursors[0] != null)
            {
                Debug.Log($"Default Cursor: {cursors[0].name}");
                Debug.Log($"  Size: {cursors[0].width}x{cursors[0].height}");
                Debug.Log($"  Format: {cursors[0].format}");
                
                // Recommend size
                int recommendedSize = GetRecommendedWebGLSize(cursors[0].width);
                Debug.Log($"  💡 Recommended WebGL size: {recommendedSize}x{recommendedSize}");
            }
            else
            {
                Debug.LogWarning("❌ Default cursor not found");
            }
            
            if (cursors[1] != null)
            {
                Debug.Log($"\nHover Cursor: {cursors[1].name}");
                Debug.Log($"  Size: {cursors[1].width}x{cursors[1].height}");
                Debug.Log($"  Format: {cursors[1].format}");
                
                // Recommend size
                int recommendedSize = GetRecommendedWebGLSize(cursors[1].width);
                Debug.Log($"  💡 Recommended WebGL size: {recommendedSize}x{recommendedSize}");
            }
            else
            {
                Debug.LogWarning("❌ Hover cursor not found");
            }
        }
        
        private Texture2D[] LoadCursorTextures()
        {
            Texture2D[] cursors = new Texture2D[2];
            
            // Try to load from common paths
            cursors[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/Cursor/cursor 1.png");
            if (cursors[0] == null)
            {
                cursors[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/Cursor/cursor.png");
            }
            
            cursors[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/Cursor/hand 2.png");
            if (cursors[1] == null)
            {
                cursors[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/Cursor/hand.png");
            }
            
            return cursors;
        }
        
        private int GetRecommendedWebGLSize(int originalSize)
        {
            if (originalSize <= 32) return 24;
            if (originalSize <= 64) return 32;
            if (originalSize <= 128) return 48;
            return 64;
        }
        #endif
        
        private void Start()
        {
            if (setupOnStart)
            {
                #if UNITY_EDITOR
                QuickSetup();
                #endif
            }
        }
    }
}
