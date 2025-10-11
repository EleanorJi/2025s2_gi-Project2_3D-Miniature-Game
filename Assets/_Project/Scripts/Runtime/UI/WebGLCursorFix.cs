using UnityEngine;

namespace Antventure.UI
{
    /// <summary>
    /// WebGL Cursor Fix - Automatically scales custom cursors for WebGL builds
    /// In WebGL, Unity's custom cursors don't scale with the canvas, causing them to appear too large
    /// </summary>
    public class WebGLCursorFix : MonoBehaviour
    {
        [Header("Cursor Textures - Original (High-Res)")]
        [Tooltip("Drag your original cursor images here")]
        [SerializeField] private Texture2D originalDefaultCursor;
        [SerializeField] private Texture2D originalHoverCursor;
        
        [Header("WebGL Scaling Settings")]
        [Tooltip("Target size for WebGL cursors (recommended: 32x32 or 24x24)")]
        [SerializeField] private int webGLCursorSize = 32;
        
        [Tooltip("Enable this to use scaled cursors in WebGL builds")]
        [SerializeField] private bool useScaledCursorsInWebGL = true;
        
        [Header("Hotspot Settings")]
        [SerializeField] private Vector2 defaultCursorHotspot = Vector2.zero;
        [SerializeField] private Vector2 hoverCursorHotspot = new Vector2(16, 16);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        private Texture2D scaledDefaultCursor;
        private Texture2D scaledHoverCursor;
        
        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (useScaledCursorsInWebGL)
            {
                InitializeWebGLCursors();
            }
#endif
        }
        
        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (useScaledCursorsInWebGL)
            {
                ApplyWebGLCursors();
            }
#endif
        }
        
        /// <summary>
        /// Initialize cursors for WebGL by creating scaled versions
        /// </summary>
        private void InitializeWebGLCursors()
        {
            if (originalDefaultCursor != null)
            {
                scaledDefaultCursor = ScaleTexture(originalDefaultCursor, webGLCursorSize, webGLCursorSize);
                if (showDebugInfo)
                {
                    Debug.Log($"[WebGL Cursor] Scaled default cursor from {originalDefaultCursor.width}x{originalDefaultCursor.height} to {webGLCursorSize}x{webGLCursorSize}");
                }
            }
            
            if (originalHoverCursor != null)
            {
                scaledHoverCursor = ScaleTexture(originalHoverCursor, webGLCursorSize, webGLCursorSize);
                if (showDebugInfo)
                {
                    Debug.Log($"[WebGL Cursor] Scaled hover cursor from {originalHoverCursor.width}x{originalHoverCursor.height} to {webGLCursorSize}x{webGLCursorSize}");
                }
            }
        }
        
        /// <summary>
        /// Apply the scaled cursors to the cursor managers
        /// </summary>
        private void ApplyWebGLCursors()
        {
            // Update UnifiedCursorManager if it exists
            if (UnifiedCursorManager.Instance != null)
            {
                UpdateUnifiedCursorManager();
            }
            
            // Update CursorManager if it exists
            if (CursorManager.Instance != null)
            {
                UpdateCursorManager();
            }
            
            if (showDebugInfo)
            {
                Debug.Log("[WebGL Cursor] Applied scaled cursors to cursor managers");
            }
        }
        
        /// <summary>
        /// Update UnifiedCursorManager with scaled cursors
        /// </summary>
        private void UpdateUnifiedCursorManager()
        {
            var cursorManagerType = typeof(UnifiedCursorManager);
            
            if (scaledDefaultCursor != null)
            {
                var defaultCursorField = cursorManagerType.GetField("defaultCursor", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                defaultCursorField?.SetValue(UnifiedCursorManager.Instance, scaledDefaultCursor);
            }
            
            if (scaledHoverCursor != null)
            {
                var hoverCursorField = cursorManagerType.GetField("hoverCursor", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                hoverCursorField?.SetValue(UnifiedCursorManager.Instance, scaledHoverCursor);
            }
            
            // Update hotspot
            var hotspotField = cursorManagerType.GetField("cursorHotspot", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (hotspotField != null)
            {
                // Scale hotspot proportionally
                float scale = (float)webGLCursorSize / originalDefaultCursor.width;
                Vector2 scaledHotspot = hoverCursorHotspot * scale;
                hotspotField.SetValue(UnifiedCursorManager.Instance, scaledHotspot);
            }
        }
        
        /// <summary>
        /// Update CursorManager with scaled cursors
        /// </summary>
        private void UpdateCursorManager()
        {
            var cursorManagerType = typeof(CursorManager);
            
            if (scaledDefaultCursor != null)
            {
                var defaultCursorField = cursorManagerType.GetField("defaultCursor", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                defaultCursorField?.SetValue(CursorManager.Instance, scaledDefaultCursor);
            }
            
            if (scaledHoverCursor != null)
            {
                var handCursorField = cursorManagerType.GetField("handCursor", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                handCursorField?.SetValue(CursorManager.Instance, scaledHoverCursor);
            }
            
            // Update hotspot
            var hotspotField = cursorManagerType.GetField("handHotspot", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (hotspotField != null && originalHoverCursor != null)
            {
                // Scale hotspot proportionally
                float scale = (float)webGLCursorSize / originalHoverCursor.width;
                Vector2 scaledHotspot = hoverCursorHotspot * scale;
                hotspotField.SetValue(CursorManager.Instance, scaledHotspot);
            }
        }
        
        /// <summary>
        /// Scale texture to specified size using bilinear filtering
        /// </summary>
        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.filterMode = FilterMode.Bilinear;
            
            // Create temporary RenderTexture
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            rt.filterMode = FilterMode.Bilinear;
            
            // Copy source to RenderTexture
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            
            // Read pixels from RenderTexture
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            
            // Clean up
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Test Cursor Scaling")]
        private void TestCursorScaling()
        {
            if (originalDefaultCursor == null && originalHoverCursor == null)
            {
                Debug.LogError("[WebGL Cursor] No cursor textures assigned!");
                return;
            }
            
            Debug.Log("=== WebGL Cursor Scaling Test ===");
            
            if (originalDefaultCursor != null)
            {
                Debug.Log($"Default Cursor: {originalDefaultCursor.width}x{originalDefaultCursor.height}");
                Debug.Log($"Will scale to: {webGLCursorSize}x{webGLCursorSize}");
                float scaleRatio = (float)originalDefaultCursor.width / webGLCursorSize;
                Debug.Log($"Scale ratio: {scaleRatio}x");
            }
            
            if (originalHoverCursor != null)
            {
                Debug.Log($"Hover Cursor: {originalHoverCursor.width}x{originalHoverCursor.height}");
                Debug.Log($"Will scale to: {webGLCursorSize}x{webGLCursorSize}");
                float scaleRatio = (float)originalHoverCursor.width / webGLCursorSize;
                Debug.Log($"Scale ratio: {scaleRatio}x");
            }
            
            Debug.Log("\n💡 Recommendation:");
            Debug.Log("- For WebGL, cursor size should be 24-32 pixels");
            Debug.Log("- Fullscreen uses original size (looks normal)");
            Debug.Log("- Windowed mode needs scaled size (this script handles it)");
        }
        
        [ContextMenu("Check Current Setup")]
        private void CheckCurrentSetup()
        {
            Debug.Log("=== Current Cursor Setup ===");
            Debug.Log($"Original Default Cursor: {(originalDefaultCursor != null ? $"{originalDefaultCursor.name} ({originalDefaultCursor.width}x{originalDefaultCursor.height})" : "Not assigned")}");
            Debug.Log($"Original Hover Cursor: {(originalHoverCursor != null ? $"{originalHoverCursor.name} ({originalHoverCursor.width}x{originalHoverCursor.height})" : "Not assigned")}");
            Debug.Log($"Target WebGL Size: {webGLCursorSize}x{webGLCursorSize}");
            Debug.Log($"Scaling Enabled: {useScaledCursorsInWebGL}");
            
            // Check cursor managers
            if (UnifiedCursorManager.Instance != null)
            {
                Debug.Log("✅ UnifiedCursorManager found");
            }
            else
            {
                Debug.LogWarning("⚠️ UnifiedCursorManager not found");
            }
            
            if (CursorManager.Instance != null)
            {
                Debug.Log("✅ CursorManager found");
            }
            else
            {
                Debug.LogWarning("⚠️ CursorManager not found");
            }
        }
        #endif
    }
}
