using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Antventure.UI.Animation
{
    /// <summary>
    /// Button Animation Controller
    /// Adds various interactive animation effects to buttons
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Scale Animation")]
        [SerializeField] private bool enableScaleAnimation = true;
        [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
        [SerializeField] private Vector3 pressScale = new Vector3(0.95f, 0.95f, 0.95f);
        [SerializeField] private float scaleAnimationDuration = 0.2f;

        [Header("Color Animation")]
        [SerializeField] private bool enableColorAnimation = true;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color pressColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private float colorAnimationDuration = 0.15f;

        [Header("Rotation Animation")]
        [SerializeField] private bool enableRotationAnimation = false;
        [SerializeField] private Vector3 hoverRotation = new Vector3(0, 0, 5f);
        [SerializeField] private float rotationAnimationDuration = 0.3f;

        [Header("Bounce Animation")]
        [SerializeField] private bool enableBounceOnClick = true;
        [SerializeField] private float bounceStrength = 0.3f;
        [SerializeField] private float bounceDuration = 0.5f;

        [Header("Glow Effect")]
        [SerializeField] private bool enableGlowEffect = false;
        [SerializeField] private Image glowImage;
        [SerializeField] private float glowIntensity = 1.5f;
        [SerializeField] private float glowDuration = 0.8f;

        [Header("Particle Effects")]
        [SerializeField] private bool enableParticleEffects = false;
        [SerializeField] private ParticleSystem clickParticles;
        [SerializeField] private ParticleSystem hoverParticles;

        [Header("Audio")]
        [SerializeField] private bool enableAudioFeedback = true;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;

        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHapticFeedback = false;

        // Component references
        private Button button;
        private Image buttonImage;
        private RectTransform rectTransform;
        
        // Original values
        private Vector3 originalScale;
        private Vector3 originalRotation;
        private Color originalColor;
        
        // Animation state
        private bool isHovering = false;
        private bool isPressed = false;
        private Coroutine currentAnimation;

        private void Awake()
        {
            // Get component references
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            
            // If no audio source is specified, try to get or create one
            if (enableAudioFeedback && audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }

        private void Start()
        {
            // Save original values
            originalScale = rectTransform.localScale;
            originalRotation = rectTransform.localEulerAngles;
            
            if (buttonImage != null)
            {
                originalColor = buttonImage.color;
                normalColor = originalColor;
            }

            // Set initial state
            UpdateButtonState();
        }

        private void OnEnable()
        {
            // Listen for button state changes
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnDisable()
        {
            // Remove listeners
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
            
            // Stop all animations
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
        }

        #region Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable) return;

            Debug.Log("[BUTTON] OnPointerEnter called on: " + gameObject.name);
            isHovering = true;
            PlayHoverAnimation();
            PlayHoverSound();
            PlayHoverParticles();
            
            // Set hover cursor when entering button area
            if (CursorManager.Instance != null)
            {
                Debug.Log("[BUTTON] Calling SetHoverCursor from: " + gameObject.name);
                CursorManager.Instance.SetHoverCursor();
            }
            else
            {
                Debug.LogWarning("[BUTTON] CursorManager.Instance is null!");
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log("[BUTTON] OnPointerExit called on: " + gameObject.name);
            isHovering = false;
            if (!isPressed)
            {
                PlayNormalAnimation();
            }
            StopHoverParticles();
            
            // Reset to default cursor when leaving button area
            if (CursorManager.Instance != null)
            {
                Debug.Log("[BUTTON] Calling SetDefaultCursor from: " + gameObject.name);
                CursorManager.Instance.SetDefaultCursor();
            }
            else
            {
                Debug.LogWarning("[BUTTON] CursorManager.Instance is null!");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;

            isPressed = true;
            PlayPressAnimation();
            PlayHapticFeedback();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            if (isHovering)
            {
                PlayHoverAnimation();
            }
            else
            {
                PlayNormalAnimation();
            }
        }

        private void OnButtonClick()
        {
            if (!button.interactable) return;

            PlayClickAnimation();
            PlayClickSound();
            PlayClickParticles();
            PlayGlowEffect();
        }

        #endregion

        #region Animation Methods

        /// <summary>
        /// Play hover animation
        /// </summary>
        private void PlayHoverAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentAnimation = StartCoroutine(AnimateToState(hoverScale, hoverColor, originalRotation + hoverRotation, scaleAnimationDuration));
        }

        /// <summary>
        /// Play press animation
        /// </summary>
        private void PlayPressAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentAnimation = StartCoroutine(AnimateToState(pressScale, pressColor, originalRotation, scaleAnimationDuration * 0.5f));
        }

        /// <summary>
        /// Play normal state animation
        /// </summary>
        private void PlayNormalAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentAnimation = StartCoroutine(AnimateToState(originalScale, normalColor, originalRotation, scaleAnimationDuration));
        }

        /// <summary>
        /// Play click animation
        /// </summary>
        private void PlayClickAnimation()
        {
            if (enableBounceOnClick)
            {
                StartCoroutine(BounceAnimation());
            }
        }

        /// <summary>
        /// Play glow effect
        /// </summary>
        private void PlayGlowEffect()
        {
            if (enableGlowEffect && glowImage != null)
            {
                StartCoroutine(GlowAnimation());
            }
        }

        /// <summary>
        /// Animate to target state
        /// </summary>
        private IEnumerator AnimateToState(Vector3 targetScale, Color targetColor, Vector3 targetRotation, float duration)
        {
            Vector3 startScale = rectTransform.localScale;
            Color startColor = buttonImage != null ? buttonImage.color : Color.white;
            Vector3 startRotation = rectTransform.localEulerAngles;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                // Apply easing (simple ease out)
                t = 1f - (1f - t) * (1f - t);

                if (enableScaleAnimation)
                {
                    rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                }

                if (enableColorAnimation && buttonImage != null)
                {
                    buttonImage.color = Color.Lerp(startColor, targetColor, t);
                }

                if (enableRotationAnimation)
                {
                    rectTransform.localEulerAngles = Vector3.Lerp(startRotation, targetRotation, t);
                }

                yield return null;
            }

            // Ensure final values are set
            if (enableScaleAnimation)
            {
                rectTransform.localScale = targetScale;
            }

            if (enableColorAnimation && buttonImage != null)
            {
                buttonImage.color = targetColor;
            }

            if (enableRotationAnimation)
            {
                rectTransform.localEulerAngles = targetRotation;
            }
        }

        /// <summary>
        /// Bounce animation coroutine
        /// </summary>
        private IEnumerator BounceAnimation()
        {
            Vector3 originalScale = rectTransform.localScale;
            Vector3 bounceScale = originalScale + Vector3.one * bounceStrength;

            float halfDuration = bounceDuration * 0.5f;

            // Scale up
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                rectTransform.localScale = Vector3.Lerp(originalScale, bounceScale, t);
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                rectTransform.localScale = Vector3.Lerp(bounceScale, originalScale, t);
                yield return null;
            }

            rectTransform.localScale = originalScale;
        }

        /// <summary>
        /// Glow animation coroutine
        /// </summary>
        private IEnumerator GlowAnimation()
        {
            glowImage.gameObject.SetActive(true);
            
            Color startColor = glowImage.color;
            Color glowColor = new Color(startColor.r, startColor.g, startColor.b, glowIntensity);
            Color fadeColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            float fadeInDuration = glowDuration * 0.3f;
            float fadeOutDuration = glowDuration * 0.7f;

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeInDuration;
                glowImage.color = Color.Lerp(startColor, glowColor, t);
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeOutDuration;
                glowImage.color = Color.Lerp(glowColor, fadeColor, t);
                yield return null;
            }

            glowImage.gameObject.SetActive(false);
            glowImage.color = startColor;
        }

        /// <summary>
        /// Update button state
        /// </summary>
        private void UpdateButtonState()
        {
            if (button != null && buttonImage != null)
            {
                if (!button.interactable)
                {
                    buttonImage.color = disabledColor;
                    rectTransform.localScale = originalScale;
                    rectTransform.localEulerAngles = originalRotation;
                }
                else
                {
                    buttonImage.color = normalColor;
                }
            }
        }

        #endregion

        #region Audio Methods

        /// <summary>
        /// Play hover sound effect
        /// </summary>
        private void PlayHoverSound()
        {
            if (enableAudioFeedback && audioSource != null && hoverSound != null)
            {
                audioSource.PlayOneShot(hoverSound);
            }
        }

        /// <summary>
        /// Play click sound effect
        /// </summary>
        private void PlayClickSound()
        {
            if (enableAudioFeedback && audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }

        #endregion

        #region Particle Effects

        /// <summary>
        /// Play hover particle effects
        /// </summary>
        private void PlayHoverParticles()
        {
            if (enableParticleEffects && hoverParticles != null)
            {
                hoverParticles.Play();
            }
        }

        /// <summary>
        /// Stop hover particle effects
        /// </summary>
        private void StopHoverParticles()
        {
            if (enableParticleEffects && hoverParticles != null)
            {
                hoverParticles.Stop();
            }
        }

        /// <summary>
        /// Play click particle effects
        /// </summary>
        private void PlayClickParticles()
        {
            if (enableParticleEffects && clickParticles != null)
            {
                clickParticles.Play();
            }
        }

        #endregion

        #region Haptic Feedback

        /// <summary>
        /// Play haptic feedback
        /// </summary>
        private void PlayHapticFeedback()
        {
            if (enableHapticFeedback)
            {
                // Play haptic feedback on mobile devices
                #if UNITY_ANDROID || UNITY_IOS
                    Handheld.Vibrate();
                #endif
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set button interactable state
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
                UpdateButtonState();
            }
        }

        /// <summary>
        /// Force play click animation
        /// </summary>
        public void ForceClickAnimation()
        {
            OnButtonClick();
        }

        /// <summary>
        /// Reset button to original state
        /// </summary>
        public void ResetToOriginalState()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            rectTransform.localScale = originalScale;
            rectTransform.localEulerAngles = originalRotation;
            
            if (buttonImage != null)
            {
                buttonImage.color = originalColor;
            }

            isHovering = false;
            isPressed = false;
        }

        #endregion

        private void Update()
        {
            // Check for button state changes
            if (button != null)
            {
                bool wasInteractable = buttonImage != null ? buttonImage.color != disabledColor : true;
                bool isInteractable = button.interactable;
                
                if (wasInteractable != isInteractable)
                {
                    UpdateButtonState();
                }
            }
        }
    }
}