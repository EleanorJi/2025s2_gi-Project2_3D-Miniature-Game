using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Antventure.UI
{
    /// <summary>
    /// Local Hint Display - Controls text display directly on the checkpoint object
    /// </summary>
    public class LocalHintDisplay : MonoBehaviour
    {
        [Header("Hint Settings")]
        [SerializeField] private float displayDuration = 5f;
        [SerializeField] private bool allowRepeat = false; // Whether hint can be shown multiple times
        
        [Header("Animation Settings")]
        [SerializeField] private bool useAnimation = true;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        [Header("Auto Find Components")]
        [SerializeField] private bool autoFindTextComponents = true;
        
        [Header("Manual Component Assignment")]
        [SerializeField] private Text legacyText; // Legacy UI Text
        [SerializeField] private TextMeshProUGUI tmpText; // TextMeshPro Text
        [SerializeField] private CanvasGroup canvasGroup; // For fading animation
        
        // State tracking
        private bool isDisplaying = false;
        private bool hasShownOnce = false;
        private Coroutine displayCoroutine;
        
        private void Awake()
        {
            if (autoFindTextComponents)
            {
                FindTextComponents();
            }
            
            // Initially hide the hint
            SetHintVisibility(false, true);
        }
        
        /// <summary>
        /// Automatically find text components in children
        /// </summary>
        private void FindTextComponents()
        {
            if (legacyText == null)
            {
                legacyText = GetComponentInChildren<Text>();
            }
            
            if (tmpText == null)
            {
                tmpText = GetComponentInChildren<TextMeshProUGUI>();
            }
            
            if (canvasGroup == null)
            {
                canvasGroup = GetComponentInChildren<CanvasGroup>();
            }
            
            // If no CanvasGroup found, try to add one to the text component's parent
            if (canvasGroup == null)
            {
                GameObject targetObject = null;
                
                if (tmpText != null)
                    targetObject = tmpText.gameObject;
                else if (legacyText != null)
                    targetObject = legacyText.gameObject;
                
                if (targetObject != null)
                {
                    canvasGroup = targetObject.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = targetObject.AddComponent<CanvasGroup>();
                    }
                }
            }
        }
        
        /// <summary>
        /// Show hint with specified text
        /// </summary>
        public void ShowHint(string text)
        {
            // Check if we can show the hint
            if (!allowRepeat && hasShownOnce)
            {
                Debug.Log($"Hint already shown once and repeat is disabled: {gameObject.name}");
                return;
            }
            
            if (isDisplaying)
            {
                Debug.Log($"Hint is already displaying: {gameObject.name}");
                return;
            }
            
            // Update text content
            UpdateTextContent(text);
            
            // Start display sequence
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }
            
            displayCoroutine = StartCoroutine(DisplaySequence());
            hasShownOnce = true;
        }
        
        /// <summary>
        /// Hide hint immediately
        /// </summary>
        public void HideHint()
        {
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
                displayCoroutine = null;
            }
            
            SetHintVisibility(false, true);
            isDisplaying = false;
        }
        
        /// <summary>
        /// Update text content in all text components
        /// </summary>
        private void UpdateTextContent(string text)
        {
            if (legacyText != null)
            {
                legacyText.text = text;
            }
            
            if (tmpText != null)
            {
                tmpText.text = text;
            }
        }
        
        /// <summary>
        /// Display sequence with fade in, wait, fade out
        /// </summary>
        private IEnumerator DisplaySequence()
        {
            isDisplaying = true;
            
            // Fade in
            if (useAnimation)
            {
                yield return StartCoroutine(FadeIn());
            }
            else
            {
                SetHintVisibility(true, true);
            }
            
            // Wait for display duration
            yield return new WaitForSeconds(displayDuration);
            
            // Fade out
            if (useAnimation)
            {
                yield return StartCoroutine(FadeOut());
            }
            else
            {
                SetHintVisibility(false, true);
            }
            
            isDisplaying = false;
            displayCoroutine = null;
        }
        
        /// <summary>
        /// Fade in animation
        /// </summary>
        private IEnumerator FadeIn()
        {
            SetHintVisibility(true, false); // Show but transparent
            
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = elapsedTime / fadeInDuration;
                SetAlpha(alpha);
                yield return null;
            }
            
            SetAlpha(1f);
        }
        
        /// <summary>
        /// Fade out animation
        /// </summary>
        private IEnumerator FadeOut()
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = 1f - (elapsedTime / fadeOutDuration);
                SetAlpha(alpha);
                yield return null;
            }
            
            SetHintVisibility(false, true);
        }
        
        /// <summary>
        /// Set hint visibility
        /// </summary>
        private void SetHintVisibility(bool visible, bool immediate)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
            else
            {
                // Fallback: directly control text components
                if (legacyText != null)
                {
                    legacyText.gameObject.SetActive(visible);
                }
                
                if (tmpText != null)
                {
                    tmpText.gameObject.SetActive(visible);
                }
            }
        }
        
        /// <summary>
        /// Set alpha value
        /// </summary>
        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
            else
            {
                // Fallback: directly control text color alpha
                if (legacyText != null)
                {
                    Color color = legacyText.color;
                    color.a = alpha;
                    legacyText.color = color;
                }
                
                if (tmpText != null)
                {
                    Color color = tmpText.color;
                    color.a = alpha;
                    tmpText.color = color;
                }
            }
        }
        
        /// <summary>
        /// Set display duration
        /// </summary>
        public void SetDisplayDuration(float duration)
        {
            displayDuration = duration;
        }
        
        /// <summary>
        /// Set whether hint can be repeated
        /// </summary>
        public void SetAllowRepeat(bool allow)
        {
            allowRepeat = allow;
        }
        
        /// <summary>
        /// Reset the hint state (allow it to be shown again)
        /// </summary>
        public void ResetHintState()
        {
            hasShownOnce = false;
            HideHint();
        }
        
        /// <summary>
        /// Check if hint is currently displaying
        /// </summary>
        public bool IsDisplaying => isDisplaying;
        
        /// <summary>
        /// Check if hint has been shown before
        /// </summary>
        public bool HasShownOnce => hasShownOnce;
        
        private void OnDestroy()
        {
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }
        }
        
        // Editor helper methods
        #if UNITY_EDITOR
        [ContextMenu("Test Show Hint")]
        private void TestShowHint()
        {
            ShowHint("Test Hint Message");
        }
        
        [ContextMenu("Test Hide Hint")]
        private void TestHideHint()
        {
            HideHint();
        }
        
        [ContextMenu("Reset Hint State")]
        private void TestResetHintState()
        {
            ResetHintState();
        }
        #endif
    }
}
