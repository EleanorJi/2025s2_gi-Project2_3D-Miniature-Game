using UnityEngine;
using UnityEngine.UI;

namespace Antventure.UI.Menus
{
    /// <summary>
    /// Settings UI Creator - Helper script to quickly create settings UI elements
    /// Use the context menu options to automatically create UI components
    /// </summary>
    public class SettingsUICreator : MonoBehaviour
    {
        [Header("UI Creation Settings")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Font uiFont;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color buttonColor = new Color(0.2f, 0.6f, 1f, 1f);
        
        #if UNITY_EDITOR
        [ContextMenu("Create Complete Settings Panel")]
        private void CreateCompleteSettingsPanel()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
                if (targetCanvas == null)
                {
                    Debug.LogError("No Canvas found! Please assign a Canvas or create one first.");
                    return;
                }
            }
            
            // Create main settings panel
            GameObject settingsPanel = CreateSettingsPanel();
            
            // Create title
            CreateTitle(settingsPanel, "Settings");
            
            // Create music volume section
            CreateVolumeSection(settingsPanel, "Music Volume", "musicVolumeSlider", "musicVolumeText", new Vector2(0, 50));
            
            // Create SFX volume section
            CreateVolumeSection(settingsPanel, "Sound Effects Volume", "sfxVolumeSlider", "sfxVolumeText", new Vector2(0, -50));
            
            // Create buttons
            CreateCloseButton(settingsPanel);
            CreateResetButton(settingsPanel);
            
            Debug.Log("Complete settings panel created! Don't forget to assign references to GameSettingsManager.");
        }
        
        private GameObject CreateSettingsPanel()
        {
            GameObject panel = new GameObject("SettingsPanel");
            panel.transform.SetParent(targetCanvas.transform, false);
            
            // Add RectTransform
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(800, 600);
            
            // Add Image component for background
            Image image = panel.AddComponent<Image>();
            image.color = backgroundColor;
            
            // Add CanvasGroup for easy fade effects
            CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();
            
            // Start inactive
            panel.SetActive(false);
            
            return panel;
        }
        
        private void CreateTitle(GameObject parent, string titleText)
        {
            GameObject title = new GameObject("Title");
            title.transform.SetParent(parent.transform, false);
            
            RectTransform rectTransform = title.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0, -50);
            rectTransform.sizeDelta = new Vector2(400, 60);
            
            Text text = title.AddComponent<Text>();
            text.text = titleText;
            text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.color = textColor;
            text.alignment = TextAnchor.MiddleCenter;
        }
        
        private void CreateVolumeSection(GameObject parent, string labelText, string sliderName, string textName, Vector2 position)
        {
            // Create container
            GameObject container = new GameObject($"{labelText}Container");
            container.transform.SetParent(parent.transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = position;
            containerRect.sizeDelta = new Vector2(600, 100);
            
            // Create label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(container.transform, false);
            
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.7f);
            labelRect.anchorMax = new Vector2(1, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            
            Text labelText_component = label.AddComponent<Text>();
            labelText_component.text = labelText;
            labelText_component.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText_component.fontSize = 24;
            labelText_component.color = textColor;
            labelText_component.alignment = TextAnchor.MiddleLeft;
            
            // Create slider
            GameObject slider = CreateSlider(container, sliderName);
            
            // Create value text
            GameObject valueText = new GameObject(textName);
            valueText.transform.SetParent(container.transform, false);
            
            RectTransform textRect = valueText.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.7f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.4f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text valueText_component = valueText.AddComponent<Text>();
            valueText_component.text = "70%";
            valueText_component.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText_component.fontSize = 18;
            valueText_component.color = textColor;
            valueText_component.alignment = TextAnchor.MiddleCenter;
        }
        
        private GameObject CreateSlider(GameObject parent, string sliderName)
        {
            GameObject slider = new GameObject(sliderName);
            slider.transform.SetParent(parent.transform, false);
            
            RectTransform sliderRect = slider.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(0.65f, 0.4f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
            
            Slider sliderComponent = slider.AddComponent<Slider>();
            sliderComponent.minValue = 0f;
            sliderComponent.maxValue = 1f;
            sliderComponent.value = 0.7f;
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(slider.transform, false);
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Create fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slider.transform, false);
            
            RectTransform fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            // Create fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            
            RectTransform fillImageRect = fill.AddComponent<RectTransform>();
            fillImageRect.anchorMin = Vector2.zero;
            fillImageRect.anchorMax = Vector2.one;
            fillImageRect.offsetMin = Vector2.zero;
            fillImageRect.offsetMax = Vector2.zero;
            
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = buttonColor;
            
            // Create handle slide area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(slider.transform, false);
            
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;
            
            // Create handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(20, 20);
            
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            
            // Assign slider components
            sliderComponent.fillRect = fillImageRect;
            sliderComponent.handleRect = handleRect;
            sliderComponent.targetGraphic = handleImage;
            
            return slider;
        }
        
        private void CreateCloseButton(GameObject parent)
        {
            GameObject button = new GameObject("CloseButton");
            button.transform.SetParent(parent.transform, false);
            
            RectTransform buttonRect = button.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-30, -30);
            buttonRect.sizeDelta = new Vector2(50, 50);
            
            Image buttonImage = button.AddComponent<Image>();
            buttonImage.color = new Color(1f, 0.3f, 0.3f, 1f);
            
            Button buttonComponent = button.AddComponent<Button>();
            buttonComponent.targetGraphic = buttonImage;
            
            // Create button text
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(button.transform, false);
            
            RectTransform textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text text = buttonText.AddComponent<Text>();
            text.text = "×";
            text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }
        
        private void CreateResetButton(GameObject parent)
        {
            GameObject button = new GameObject("ResetButton");
            button.transform.SetParent(parent.transform, false);
            
            RectTransform buttonRect = button.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0, 50);
            buttonRect.sizeDelta = new Vector2(200, 40);
            
            Image buttonImage = button.AddComponent<Image>();
            buttonImage.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            
            Button buttonComponent = button.AddComponent<Button>();
            buttonComponent.targetGraphic = buttonImage;
            
            // Create button text
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(button.transform, false);
            
            RectTransform textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text text = buttonText.AddComponent<Text>();
            text.text = "Reset to Default";
            text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
        }
        
        [ContextMenu("Create GameSettingsManager")]
        private void CreateGameSettingsManager()
        {
            GameObject manager = new GameObject("GameSettingsManager");
            manager.AddComponent<GameSettingsManager>();
            
            Debug.Log("GameSettingsManager created! Don't forget to assign UI references.");
        }
        #endif
    }
}
