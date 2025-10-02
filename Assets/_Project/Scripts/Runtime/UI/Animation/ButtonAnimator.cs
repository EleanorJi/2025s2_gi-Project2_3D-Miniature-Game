using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

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
        [SerializeField] private Ease scaleEase = Ease.OutBack;

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
        [SerializeField] private Ease rotationEase = Ease.OutElastic;

        [Header("Bounce Animation")]
        [SerializeField] private bool enableBounceOnClick = true;
        [SerializeField] private float bounceStrength = 0.3f;
        [SerializeField] private int bounceVibrato = 2;
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
        private Sequence currentAnimation;

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
                currentAnimation.Kill();
            }
        }

        #region Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable) return;

            isHovering = true;
            PlayHoverAnimation();
            PlayHoverSound();
            PlayHoverParticles();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            if (!isPressed)
            {
                PlayNormalAnimation();
            }
            StopHoverParticles();
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
                currentAnimation.Kill();
            }

            currentAnimation = DOTween.Sequence();

            if (enableScaleAnimation)
            {
                currentAnimation.Join(rectTransform.DOScale(hoverScale, scaleAnimationDuration).SetEase(scaleEase));
            }

            if (enableColorAnimation && buttonImage != null)
            {
                currentAnimation.Join(buttonImage.DOColor(hoverColor, colorAnimationDuration));
            }

            if (enableRotationAnimation)
            {
                currentAnimation.Join(rectTransform.DOLocalRotate(originalRotation + hoverRotation, rotationAnimationDuration).SetEase(rotationEase));
            }
        }

        /// <summary>
        /// Play press animation
        /// </summary>
        private void PlayPressAnimation()
        {
            if (currentAnimation != null)
            {
                currentAnimation.Kill();
            }

            currentAnimation = DOTween.Sequence();

            if (enableScaleAnimation)
            {
                currentAnimation.Join(rectTransform.DOScale(pressScale, scaleAnimationDuration * 0.5f));
            }

            if (enableColorAnimation && buttonImage != null)
            {
                currentAnimation.Join(buttonImage.DOColor(pressColor, colorAnimationDuration * 0.5f));
            }
        }

        /// <summary>
        /// Play normal state animation
        /// </summary>
        private void PlayNormalAnimation()
        {
            if (currentAnimation != null)
            {
                currentAnimation.Kill();
            }

            currentAnimation = DOTween.Sequence();

            if (enableScaleAnimation)
            {
                currentAnimation.Join(rectTransform.DOScale(originalScale, scaleAnimationDuration));
            }

            if (enableColorAnimation && buttonImage != null)
            {
                currentAnimation.Join(buttonImage.DOColor(normalColor, colorAnimationDuration));
            }

            if (enableRotationAnimation)
            {
                currentAnimation.Join(rectTransform.DOLocalRotate(originalRotation, rotationAnimationDuration));
            }
        }

        /// <summary>
        /// Play click animation
        /// </summary>
        private void PlayClickAnimation()
        {
            if (enableBounceOnClick)
            {
                rectTransform.DOPunchScale(Vector3.one * bounceStrength, bounceDuration, bounceVibrato);
            }
        }

        /// <summary>
        /// Play glow effect
        /// </summary>
        private void PlayGlowEffect()
        {
            if (enableGlowEffect && glowImage != null)
            {
                glowImage.gameObject.SetActive(true);
                
                Sequence glowSequence = DOTween.Sequence();
                glowSequence.Append(glowImage.DOFade(glowIntensity, glowDuration * 0.3f));
                glowSequence.Append(glowImage.DOFade(0f, glowDuration * 0.7f));
                glowSequence.OnComplete(() => glowImage.gameObject.SetActive(false));
            }
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
                currentAnimation.Kill();
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