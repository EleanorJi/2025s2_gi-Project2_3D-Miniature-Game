using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Antventure.UI
{
    /// <summary>
    /// Font Manager - Unified font management system for the entire game
    /// Handles both Unity Text and TextMeshPro components
    /// </summary>
    [CreateAssetMenu(fileName = "FontManager", menuName = "Antventure/UI/Font Manager")]
    public class FontManager : ScriptableObject
    {
        [System.Serializable]
        public class FontSettings
        {
            [Header("Font Assets")]
            public Font unityFont;                    // For Unity Text components
            public TMP_FontAsset tmpFont;            // For TextMeshPro components
            
            [Header("Font Sizes")]
            public int titleSize = 36;               // Main titles
            public int headerSize = 24;              // Section headers
            public int bodySize = 18;                // Body text
            public int smallSize = 14;               // Small text, captions
            public int buttonSize = 20;              // Button text
            public int hudSize = 16;                 // HUD elements
            
            [Header("Colors")]
            public Color primaryTextColor = Color.white;
            public Color secondaryTextColor = Color.gray;
            public Color accentTextColor = Color.yellow;
            public Color warningTextColor = Color.red;
            public Color successTextColor = Color.green;
        }
        
        [Header("Font Configuration")]
        public FontSettings fontSettings = new FontSettings();
        
        [Header("Auto-Apply Settings")]
        public bool autoApplyOnStart = true;
        public bool includeInactiveObjects = true;
        
        private static FontManager _instance;
        public static FontManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<FontManager>("FontManager");
                    if (_instance == null)
                    {
                        Debug.LogWarning("[FontManager] No FontManager found in Resources folder!");
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Apply unified fonts to all UI elements in the scene
        /// </summary>
        public void ApplyFontsToScene()
        {
            Debug.Log("[FontManager] Applying unified fonts to scene...");
            
            // Apply to Unity Text components
            ApplyToUnityTextComponents();
            
            // Apply to TextMeshPro components
            ApplyToTextMeshProComponents();
            
            Debug.Log("[FontManager] Font application completed!");
        }
        
        /// <summary>
        /// Apply fonts to all Unity Text components
        /// </summary>
        private void ApplyToUnityTextComponents()
        {
            Text[] allTexts = FindObjectsOfType<Text>(includeInactiveObjects);
            Debug.Log($"[FontManager] Found {allTexts.Length} Unity Text components");
            
            foreach (Text text in allTexts)
            {
                ApplyFontToUnityText(text);
            }
        }
        
        /// <summary>
        /// Apply fonts to all TextMeshPro components
        /// </summary>
        private void ApplyToTextMeshProComponents()
        {
            // TextMeshProUGUI (UI)
            TextMeshProUGUI[] allTMPUI = FindObjectsOfType<TextMeshProUGUI>(includeInactiveObjects);
            Debug.Log($"[FontManager] Found {allTMPUI.Length} TextMeshProUGUI components");
            
            foreach (TextMeshProUGUI tmp in allTMPUI)
            {
                ApplyFontToTextMeshPro(tmp);
            }
            
            // TextMeshPro (3D)
            TextMeshPro[] allTMP3D = FindObjectsOfType<TextMeshPro>(includeInactiveObjects);
            Debug.Log($"[FontManager] Found {allTMP3D.Length} TextMeshPro 3D components");
            
            foreach (TextMeshPro tmp in allTMP3D)
            {
                ApplyFontToTextMeshPro(tmp);
            }
        }
        
        /// <summary>
        /// Apply font settings to a Unity Text component
        /// </summary>
        public void ApplyFontToUnityText(Text text)
        {
            if (text == null) return;
            
            // Set font
            if (fontSettings.unityFont != null)
            {
                text.font = fontSettings.unityFont;
            }
            
            // Set font size based on component type or name
            int fontSize = DetermineFontSize(text.gameObject);
            text.fontSize = fontSize;
            
            // Set color based on component type
            Color textColor = DetermineTextColor(text.gameObject);
            text.color = textColor;
            
            Debug.Log($"[FontManager] Applied Unity font to: {text.name} (Size: {fontSize})");
        }
        
        /// <summary>
        /// Apply font settings to a TextMeshPro component
        /// </summary>
        public void ApplyFontToTextMeshPro(TMP_Text tmp)
        {
            if (tmp == null) return;
            
            // Set font
            if (fontSettings.tmpFont != null)
            {
                tmp.font = fontSettings.tmpFont;
            }
            
            // Set font size based on component type or name
            float fontSize = DetermineFontSize(tmp.gameObject);
            tmp.fontSize = fontSize;
            
            // Set color based on component type
            Color textColor = DetermineTextColor(tmp.gameObject);
            tmp.color = textColor;
            
            Debug.Log($"[FontManager] Applied TMP font to: {tmp.name} (Size: {fontSize})");
        }
        
        /// <summary>
        /// Determine appropriate font size based on GameObject name and hierarchy
        /// </summary>
        private int DetermineFontSize(GameObject obj)
        {
            string name = obj.name.ToLower();
            string parentName = obj.transform.parent?.name.ToLower() ?? "";
            
            // Title text
            if (name.Contains("title") || name.Contains("header") || name.Contains("heading"))
                return fontSettings.titleSize;
            
            // Button text
            if (name.Contains("button") || parentName.Contains("button"))
                return fontSettings.buttonSize;
            
            // HUD elements
            if (name.Contains("hud") || name.Contains("health") || name.Contains("score") || 
                name.Contains("timer") || parentName.Contains("hud"))
                return fontSettings.hudSize;
            
            // Small text
            if (name.Contains("small") || name.Contains("caption") || name.Contains("hint"))
                return fontSettings.smallSize;
            
            // Header text
            if (name.Contains("header") || name.Contains("label"))
                return fontSettings.headerSize;
            
            // Default to body text
            return fontSettings.bodySize;
        }
        
        /// <summary>
        /// Determine appropriate text color based on GameObject name and context
        /// </summary>
        private Color DetermineTextColor(GameObject obj)
        {
            string name = obj.name.ToLower();
            
            // Warning/Error text
            if (name.Contains("warning") || name.Contains("error") || name.Contains("danger"))
                return fontSettings.warningTextColor;
            
            // Success text
            if (name.Contains("success") || name.Contains("complete") || name.Contains("win"))
                return fontSettings.successTextColor;
            
            // Accent text
            if (name.Contains("accent") || name.Contains("highlight") || name.Contains("special"))
                return fontSettings.accentTextColor;
            
            // Secondary text
            if (name.Contains("secondary") || name.Contains("subtitle") || name.Contains("description"))
                return fontSettings.secondaryTextColor;
            
            // Default to primary text color
            return fontSettings.primaryTextColor;
        }
        
        /// <summary>
        /// Create a new Text component with unified font settings
        /// </summary>
        public Text CreateUnityText(GameObject parent, string textContent, FontType fontType = FontType.Body)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);
            
            Text text = textObj.AddComponent<Text>();
            text.text = textContent;
            
            // Apply font settings
            text.font = fontSettings.unityFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = GetFontSizeByType(fontType);
            text.color = fontSettings.primaryTextColor;
            
            return text;
        }
        
        /// <summary>
        /// Create a new TextMeshPro component with unified font settings
        /// </summary>
        public TextMeshProUGUI CreateTextMeshPro(GameObject parent, string textContent, FontType fontType = FontType.Body)
        {
            GameObject textObj = new GameObject("TextMeshPro");
            textObj.transform.SetParent(parent.transform, false);
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = textContent;
            
            // Apply font settings
            if (fontSettings.tmpFont != null)
                tmp.font = fontSettings.tmpFont;
            tmp.fontSize = GetFontSizeByType(fontType);
            tmp.color = fontSettings.primaryTextColor;
            
            return tmp;
        }
        
        /// <summary>
        /// Get font size by type
        /// </summary>
        public int GetFontSizeByType(FontType fontType)
        {
            switch (fontType)
            {
                case FontType.Title: return fontSettings.titleSize;
                case FontType.Header: return fontSettings.headerSize;
                case FontType.Body: return fontSettings.bodySize;
                case FontType.Small: return fontSettings.smallSize;
                case FontType.Button: return fontSettings.buttonSize;
                case FontType.HUD: return fontSettings.hudSize;
                default: return fontSettings.bodySize;
            }
        }
        
        /// <summary>
        /// Font type enumeration
        /// </summary>
        public enum FontType
        {
            Title,
            Header,
            Body,
            Small,
            Button,
            HUD
        }
    }
    
    /// <summary>
    /// Font Manager Component - Attach to GameObjects to automatically apply fonts
    /// </summary>
    public class FontManagerComponent : MonoBehaviour
    {
        [Header("Auto-Apply Settings")]
        public bool applyOnStart = true;
        public bool applyToChildren = true;
        
        private void Start()
        {
            if (applyOnStart && FontManager.Instance != null)
            {
                ApplyFonts();
            }
        }
        
        [ContextMenu("Apply Fonts")]
        public void ApplyFonts()
        {
            if (FontManager.Instance == null)
            {
                Debug.LogWarning("[FontManagerComponent] FontManager instance not found!");
                return;
            }
            
            if (applyToChildren)
            {
                // Apply to all text components in children
                Text[] texts = GetComponentsInChildren<Text>(true);
                foreach (Text text in texts)
                {
                    FontManager.Instance.ApplyFontToUnityText(text);
                }
                
                TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI tmp in tmps)
                {
                    FontManager.Instance.ApplyFontToTextMeshPro(tmp);
                }
            }
            else
            {
                // Apply only to this GameObject
                Text text = GetComponent<Text>();
                if (text != null)
                    FontManager.Instance.ApplyFontToUnityText(text);
                
                TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                    FontManager.Instance.ApplyFontToTextMeshPro(tmp);
            }
        }
    }
}


