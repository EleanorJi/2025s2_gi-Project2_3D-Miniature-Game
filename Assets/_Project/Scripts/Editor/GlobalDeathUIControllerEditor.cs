using UnityEngine;
using UnityEditor;
using TMPro;
using Antventure.UI;

namespace Antventure.Editor
{
    [CustomEditor(typeof(GlobalDeathUIController))]
    public class GlobalDeathUIControllerEditor : UnityEditor.Editor
    {
        private GlobalDeathUIController controller;

        private void OnEnable()
        {
            controller = (GlobalDeathUIController)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

            // Create runtime UI button
            if (GUILayout.Button("Create Death UI (Runtime)", GUILayout.Height(30)))
            {
                controller.CreateDeathUIRuntime();
                EditorUtility.SetDirty(controller);
            }

            EditorGUILayout.Space(5);

            // Test buttons (only shown at runtime)
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Test Functions", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Test Death UI - Default"))
                {
                    controller.ShowDeathUI();
                }

                if (GUILayout.Button("Test Death UI - Custom Message"))
                {
                    controller.ShowDeathUI("This is a test death message", null, 2f);
                }

                if (GUILayout.Button("Hide Death UI"))
                {
                    controller.HideDeathUI();
                }

                EditorGUILayout.Space(5);

                // Real-time adjustment parameters
                EditorGUILayout.LabelField("Real-time Adjustment", EditorStyles.boldLabel);
                
                // Get current serialized properties
                SerializedObject so = new SerializedObject(controller);
                SerializedProperty ratioProperty = so.FindProperty("imageMaxScreenRatio");
                SerializedProperty alphaProperty = so.FindProperty("backgroundAlpha");
                
                EditorGUI.BeginChangeCheck();
                
                float newRatio = EditorGUILayout.Slider("Image Screen Ratio", 
                    ratioProperty.floatValue, 0.1f, 1f);
                
                float newAlpha = EditorGUILayout.Slider("Background Alpha", 
                    alphaProperty.floatValue, 0f, 1f);

                if (EditorGUI.EndChangeCheck())
                {
                    ratioProperty.floatValue = newRatio;
                    alphaProperty.floatValue = newAlpha;
                    so.ApplyModifiedProperties();
                    
                    controller.SetImageMaxScreenRatio(newRatio);
                    controller.SetBackgroundAlpha(newAlpha);
                }
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Force Apply Current Settings"))
                {
                    controller.ForceApplySettings();
                }

                EditorGUILayout.Space(5);

                // Text settings adjustment
                EditorGUILayout.LabelField("Text Settings", EditorStyles.boldLabel);
                
                SerializedProperty fontSizeProperty = so.FindProperty("fontSize");
                SerializedProperty textColorProperty = so.FindProperty("textColor");
                SerializedProperty customFontProperty = so.FindProperty("customFont");
                SerializedProperty enableOutlineProperty = so.FindProperty("enableTextOutline");
                
                EditorGUI.BeginChangeCheck();
                
                float newFontSize = EditorGUILayout.Slider("Font Size", 
                    fontSizeProperty.floatValue, 8f, 100f);
                
                Color newTextColor = EditorGUILayout.ColorField("Text Color", 
                    textColorProperty.colorValue);
                
                TMP_FontAsset newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Custom Font", 
                    customFontProperty.objectReferenceValue, typeof(TMP_FontAsset), false);
                
                bool newEnableOutline = EditorGUILayout.Toggle("Enable Outline", 
                    enableOutlineProperty.boolValue);

                if (EditorGUI.EndChangeCheck())
                {
                    fontSizeProperty.floatValue = newFontSize;
                    textColorProperty.colorValue = newTextColor;
                    customFontProperty.objectReferenceValue = newFont;
                    enableOutlineProperty.boolValue = newEnableOutline;
                    so.ApplyModifiedProperties();
                    
                    controller.SetFontSize(newFontSize);
                    controller.SetTextColor(newTextColor);
                    controller.SetCustomFont(newFont);
                    controller.ApplyTextSettings();
                }

                if (GUILayout.Button("Apply Text Settings"))
                {
                    controller.ApplyTextSettings();
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Global Death UI Controller Instructions:\n" +
                "1. Click 'Create Death UI' button to automatically create UI structure\n" +
                "2. Adjust image max screen ratio to control death image size\n" +
                "3. Existing DeathUIOverlay and DeathUI_AutoWire will automatically use this controller\n" +
                "4. Use test buttons to preview effects at runtime", 
                MessageType.Info);
        }
    }

    /// <summary>
    /// Menu item: Create Global Death UI Controller
    /// </summary>
    public class GlobalDeathUIControllerMenu
    {
        [MenuItem("GameObject/UI/Global Death UI Controller", false, 10)]
        public static void CreateGlobalDeathUIController()
        {
            // Check if already exists
#if UNITY_2023_1_OR_NEWER
            GlobalDeathUIController existing = Object.FindFirstObjectByType<GlobalDeathUIController>();
#else
            GlobalDeathUIController existing = Object.FindObjectOfType<GlobalDeathUIController>();
#endif
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Warning", "GlobalDeathUIController already exists in scene!", "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create new GameObject
            GameObject go = new GameObject("GlobalDeathUIController");
            GlobalDeathUIController controller = go.AddComponent<GlobalDeathUIController>();
            
            // Set as DontDestroyOnLoad
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(go);
            }

            // Auto create UI
            controller.CreateDeathUIRuntime();

            // Select newly created object
            Selection.activeGameObject = go;
            
            // Mark scene as modified
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            Debug.Log("[GlobalDeathUI] Global Death UI Controller creation completed!");
        }

        [MenuItem("Tools/Antventure/Setup Global Death UI")]
        public static void SetupGlobalDeathUI()
        {
            // Find all existing death UI scripts and enable global controller option
#if UNITY_2023_1_OR_NEWER
            DeathUIOverlay[] overlays = Object.FindObjectsByType<DeathUIOverlay>(FindObjectsSortMode.None);
            DeathUI_AutoWire[] autoWires = Object.FindObjectsByType<DeathUI_AutoWire>(FindObjectsSortMode.None);
#else
            DeathUIOverlay[] overlays = Object.FindObjectsOfType<DeathUIOverlay>();
            DeathUI_AutoWire[] autoWires = Object.FindObjectsOfType<DeathUI_AutoWire>();
#endif

            int count = 0;

            foreach (var overlay in overlays)
            {
                SerializedObject so = new SerializedObject(overlay);
                SerializedProperty prop = so.FindProperty("useGlobalController");
                if (prop != null)
                {
                    prop.boolValue = true;
                    so.ApplyModifiedProperties();
                    count++;
                }
            }

            foreach (var autoWire in autoWires)
            {
                SerializedObject so = new SerializedObject(autoWire);
                SerializedProperty prop = so.FindProperty("useGlobalController");
                if (prop != null)
                {
                    prop.boolValue = true;
                    so.ApplyModifiedProperties();
                    count++;
                }
            }

            // Create global controller (if not exists)
#if UNITY_2023_1_OR_NEWER
            if (Object.FindFirstObjectByType<GlobalDeathUIController>() == null)
#else
            if (Object.FindObjectOfType<GlobalDeathUIController>() == null)
#endif
            {
                CreateGlobalDeathUIController();
            }

            EditorUtility.DisplayDialog("Setup Complete", 
                $"Enabled global controller for {count} death UI scripts.\n" +
                "Global Death UI Controller has been created or already exists.", "OK");
        }
    }
}
