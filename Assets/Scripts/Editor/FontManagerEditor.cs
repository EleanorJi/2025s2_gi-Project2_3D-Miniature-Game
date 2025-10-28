using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Antventure.UI;

namespace Antventure.Editor
{
    /// <summary>
    /// Font Manager Editor Window - Tools for managing fonts in the Unity Editor
    /// </summary>
    public class FontManagerEditor : EditorWindow
    {
        private FontManager fontManager;
        private Vector2 scrollPosition;
        private bool showFontSettings = true;
        private bool showSceneAnalysis = true;
        private bool showBatchOperations = true;
        
        // Analysis results
        private Text[] unityTexts;
        private TextMeshProUGUI[] tmpTexts;
        private int totalTextComponents;
        
        [MenuItem("Antventure/UI/Font Manager")]
        public static void ShowWindow()
        {
            FontManagerEditor window = GetWindow<FontManagerEditor>("Font Manager");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadFontManager();
            AnalyzeCurrentScene();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Label("Font Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Font Manager Reference
            DrawFontManagerSection();
            
            // Font Settings
            if (showFontSettings)
                DrawFontSettingsSection();
            
            // Scene Analysis
            if (showSceneAnalysis)
                DrawSceneAnalysisSection();
            
            // Batch Operations
            if (showBatchOperations)
                DrawBatchOperationsSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawFontManagerSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Font Manager Asset", EditorStyles.boldLabel);
            
            FontManager newFontManager = (FontManager)EditorGUILayout.ObjectField(
                "Font Manager", fontManager, typeof(FontManager), false);
            
            if (newFontManager != fontManager)
            {
                fontManager = newFontManager;
            }
            
            if (fontManager == null)
            {
                EditorGUILayout.HelpBox("No FontManager asset found. Create one using the menu: " +
                    "Assets > Create > Antventure > UI > Font Manager", MessageType.Warning);
                
                if (GUILayout.Button("Create Font Manager Asset"))
                {
                    CreateFontManagerAsset();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFontSettingsSection()
        {
            EditorGUILayout.BeginVertical("box");
            showFontSettings = EditorGUILayout.Foldout(showFontSettings, "Font Settings", true);
            
            if (showFontSettings && fontManager != null)
            {
                SerializedObject so = new SerializedObject(fontManager);
                SerializedProperty fontSettings = so.FindProperty("fontSettings");
                
                EditorGUILayout.PropertyField(fontSettings, true);
                
                if (so.hasModifiedProperties)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(fontManager);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSceneAnalysisSection()
        {
            EditorGUILayout.BeginVertical("box");
            showSceneAnalysis = EditorGUILayout.Foldout(showSceneAnalysis, "Scene Analysis", true);
            
            if (showSceneAnalysis)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Total Text Components: {totalTextComponents}");
                if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                {
                    AnalyzeCurrentScene();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"Unity Text Components: {(unityTexts?.Length ?? 0)}");
                EditorGUILayout.LabelField($"TextMeshPro Components: {(tmpTexts?.Length ?? 0)}");
                
                EditorGUILayout.Space();
                
                // Show font usage breakdown
                if (unityTexts != null && unityTexts.Length > 0)
                {
                    GUILayout.Label("Unity Text Font Usage:", EditorStyles.boldLabel);
                    var fontUsage = new System.Collections.Generic.Dictionary<Font, int>();
                    
                    foreach (Text text in unityTexts)
                    {
                        if (text.font != null)
                        {
                            if (fontUsage.ContainsKey(text.font))
                                fontUsage[text.font]++;
                            else
                                fontUsage[text.font] = 1;
                        }
                    }
                    
                    foreach (var kvp in fontUsage)
                    {
                        EditorGUILayout.LabelField($"  {kvp.Key.name}: {kvp.Value} components");
                    }
                }
                
                if (tmpTexts != null && tmpTexts.Length > 0)
                {
                    GUILayout.Label("TextMeshPro Font Usage:", EditorStyles.boldLabel);
                    var tmpFontUsage = new System.Collections.Generic.Dictionary<TMP_FontAsset, int>();
                    
                    foreach (TextMeshProUGUI tmp in tmpTexts)
                    {
                        if (tmp.font != null)
                        {
                            if (tmpFontUsage.ContainsKey(tmp.font))
                                tmpFontUsage[tmp.font]++;
                            else
                                tmpFontUsage[tmp.font] = 1;
                        }
                    }
                    
                    foreach (var kvp in tmpFontUsage)
                    {
                        EditorGUILayout.LabelField($"  {kvp.Key.name}: {kvp.Value} components");
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBatchOperationsSection()
        {
            EditorGUILayout.BeginVertical("box");
            showBatchOperations = EditorGUILayout.Foldout(showBatchOperations, "Batch Operations", true);
            
            if (showBatchOperations)
            {
                GUI.enabled = fontManager != null;
                
                if (GUILayout.Button("Apply Fonts to Current Scene"))
                {
                    ApplyFontsToScene();
                }
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Apply Fonts to Selected Objects"))
                {
                    ApplyFontsToSelection();
                }
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset All Font Sizes"))
                {
                    if (EditorUtility.DisplayDialog("Reset Font Sizes", 
                        "This will reset all text component font sizes in the scene. Continue?", 
                        "Yes", "Cancel"))
                    {
                        ResetAllFontSizes();
                    }
                }
                
                if (GUILayout.Button("Standardize Colors"))
                {
                    StandardizeTextColors();
                }
                EditorGUILayout.EndHorizontal();
                
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void LoadFontManager()
        {
            fontManager = Resources.Load<FontManager>("FontManager");
            if (fontManager == null)
            {
                // Try to find it in the project
                string[] guids = AssetDatabase.FindAssets("t:FontManager");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    fontManager = AssetDatabase.LoadAssetAtPath<FontManager>(path);
                }
            }
        }
        
        private void AnalyzeCurrentScene()
        {
            unityTexts = FindObjectsOfType<Text>(true);
            tmpTexts = FindObjectsOfType<TextMeshProUGUI>(true);
            totalTextComponents = unityTexts.Length + tmpTexts.Length;
        }
        
        private void ApplyFontsToScene()
        {
            if (fontManager == null) return;
            
            Undo.RecordObjects(unityTexts, "Apply Fonts to Unity Text");
            Undo.RecordObjects(tmpTexts, "Apply Fonts to TextMeshPro");
            
            fontManager.ApplyFontsToScene();
            
            EditorUtility.SetDirty(this);
            Debug.Log($"[FontManagerEditor] Applied fonts to {totalTextComponents} text components");
        }
        
        private void ApplyFontsToSelection()
        {
            if (fontManager == null) return;
            
            GameObject[] selectedObjects = Selection.gameObjects;
            int appliedCount = 0;
            
            foreach (GameObject obj in selectedObjects)
            {
                Text[] texts = obj.GetComponentsInChildren<Text>(true);
                TextMeshProUGUI[] tmps = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
                
                Undo.RecordObjects(texts, "Apply Fonts to Selected Unity Text");
                Undo.RecordObjects(tmps, "Apply Fonts to Selected TextMeshPro");
                
                foreach (Text text in texts)
                {
                    fontManager.ApplyFontToUnityText(text);
                    appliedCount++;
                }
                
                foreach (TextMeshProUGUI tmp in tmps)
                {
                    fontManager.ApplyFontToTextMeshPro(tmp);
                    appliedCount++;
                }
            }
            
            Debug.Log($"[FontManagerEditor] Applied fonts to {appliedCount} text components in selection");
        }
        
        private void ResetAllFontSizes()
        {
            if (fontManager == null) return;
            
            Undo.RecordObjects(unityTexts, "Reset Unity Text Font Sizes");
            Undo.RecordObjects(tmpTexts, "Reset TextMeshPro Font Sizes");
            
            foreach (Text text in unityTexts)
            {
                text.fontSize = fontManager.fontSettings.bodySize;
            }
            
            foreach (TextMeshProUGUI tmp in tmpTexts)
            {
                tmp.fontSize = fontManager.fontSettings.bodySize;
            }
            
            Debug.Log($"[FontManagerEditor] Reset font sizes for {totalTextComponents} text components");
        }
        
        private void StandardizeTextColors()
        {
            if (fontManager == null) return;
            
            Undo.RecordObjects(unityTexts, "Standardize Unity Text Colors");
            Undo.RecordObjects(tmpTexts, "Standardize TextMeshPro Colors");
            
            foreach (Text text in unityTexts)
            {
                text.color = fontManager.fontSettings.primaryTextColor;
            }
            
            foreach (TextMeshProUGUI tmp in tmpTexts)
            {
                tmp.color = fontManager.fontSettings.primaryTextColor;
            }
            
            Debug.Log($"[FontManagerEditor] Standardized colors for {totalTextComponents} text components");
        }
        
        private void CreateFontManagerAsset()
        {
            FontManager newFontManager = CreateInstance<FontManager>();
            
            // Set up default values
            newFontManager.fontSettings.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            // Try to find the GROBOLD font
            string[] fontGuids = AssetDatabase.FindAssets("GROBOLD t:TMP_FontAsset");
            if (fontGuids.Length > 0)
            {
                string fontPath = AssetDatabase.GUIDToAssetPath(fontGuids[0]);
                newFontManager.fontSettings.tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            }
            
            // Create Resources folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            string assetPath = "Assets/Resources/FontManager.asset";
            AssetDatabase.CreateAsset(newFontManager, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            fontManager = newFontManager;
            Selection.activeObject = fontManager;
            
            Debug.Log($"[FontManagerEditor] Created FontManager asset at {assetPath}");
        }
    }
}

