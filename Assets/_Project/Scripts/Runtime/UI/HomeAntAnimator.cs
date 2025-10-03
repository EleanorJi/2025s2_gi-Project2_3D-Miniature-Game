using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Antventure.UI
{
    /// <summary>
    /// Simple animator for the home scene ant
    /// Creates various idle animations without requiring sprite sheets
    /// </summary>
    public class HomeAntAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private bool enableAnimation = true;
        [SerializeField] private float animationSpeed = 1f;
        
        [Header("Breathing Animation")]
        [SerializeField] private bool enableBreathing = true;
        [SerializeField] private float breathingScale = 0.05f; // How much to scale
        [SerializeField] private float breathingSpeed = 2f;
        
        [Header("Idle Movement")]
        [SerializeField] private bool enableIdleMovement = true;
        [SerializeField] private Vector2 movementRange = new Vector2(10f, 5f); // X and Y movement range
        [SerializeField] private float movementSpeed = 0.5f;
        
        [Header("Rotation Animation")]
        [SerializeField] private bool enableRotation = false;
        [SerializeField] private float maxRotation = 3f; // Max rotation in degrees
        [SerializeField] private float rotationSpeed = 1f;
        
        [Header("Color Pulse")]
        [SerializeField] private bool enableColorPulse = false;
        [SerializeField] private Color pulseColor = Color.white;
        [SerializeField] private float pulseSpeed = 1.5f;
        [SerializeField] private float pulseIntensity = 0.2f;
        
        [Header("Crawling Animation")]
        [SerializeField] private bool enableCrawling = false;
        [SerializeField] private Vector2 crawlStartPoint = new Vector2(-100, 0);
        [SerializeField] private Vector2 crawlEndPoint = new Vector2(100, 0);
        [SerializeField] private float crawlDuration = 5f;
        [SerializeField] private bool crawlLoop = true;
        [SerializeField] private AnimationCurve crawlCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        [Header("Walking Bobbing")]
        [SerializeField] private bool enableWalkingBob = true;
        [SerializeField] private float bobHeight = 3f;
        [SerializeField] private float bobSpeed = 8f;
        
        // Component references
        private RectTransform rectTransform;
        private Image antImage;
        
        // Original values
        private Vector3 originalScale;
        private Vector2 originalPosition;
        private Vector3 originalRotation;
        private Color originalColor;
        
        // Animation state
        private float breathingTime;
        private float movementTime;
        private float rotationTime;
        private float colorTime;
        private float crawlTime;
        private bool isCrawling = false;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            antImage = GetComponent<Image>();
            
            if (rectTransform == null)
            {
                Debug.LogError("[ANT ANIMATOR] RectTransform not found!");
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            // Store original values
            originalScale = rectTransform.localScale;
            originalPosition = rectTransform.anchoredPosition;
            originalRotation = rectTransform.localEulerAngles;
            
            if (antImage != null)
            {
                originalColor = antImage.color;
            }
            
            Debug.Log("[ANT ANIMATOR] Home ant animator initialized");
        }
        
        private void Update()
        {
            if (!enableAnimation) return;
            
            // Update animation timers
            breathingTime += Time.deltaTime * breathingSpeed * animationSpeed;
            movementTime += Time.deltaTime * movementSpeed * animationSpeed;
            rotationTime += Time.deltaTime * rotationSpeed * animationSpeed;
            colorTime += Time.deltaTime * pulseSpeed * animationSpeed;
            
            // Handle crawling animation
            if (enableCrawling)
            {
                UpdateCrawlingAnimation();
            }
            else
            {
                // Apply regular animations when not crawling
                ApplyBreathingAnimation();
                ApplyIdleMovement();
            }
            
            ApplyRotationAnimation();
            ApplyColorPulse();
        }
        
        private void ApplyBreathingAnimation()
        {
            if (!enableBreathing) return;
            
            // Breathing effect using sine wave
            float breathingFactor = 1f + Mathf.Sin(breathingTime) * breathingScale;
            Vector3 newScale = originalScale * breathingFactor;
            rectTransform.localScale = newScale;
        }
        
        private void ApplyIdleMovement()
        {
            if (!enableIdleMovement) return;
            
            // Subtle idle movement using sine waves with different phases
            float xOffset = Mathf.Sin(movementTime) * movementRange.x;
            float yOffset = Mathf.Sin(movementTime * 0.7f + 1.5f) * movementRange.y; // Different phase and speed
            
            Vector2 newPosition = originalPosition + new Vector2(xOffset, yOffset);
            rectTransform.anchoredPosition = newPosition;
        }
        
        private void ApplyRotationAnimation()
        {
            if (!enableRotation) return;
            
            // Subtle rotation animation
            float rotationOffset = Mathf.Sin(rotationTime) * maxRotation;
            Vector3 newRotation = originalRotation + new Vector3(0, 0, rotationOffset);
            rectTransform.localEulerAngles = newRotation;
        }
        
        private void ApplyColorPulse()
        {
            if (!enableColorPulse || antImage == null) return;
            
            // Color pulse animation
            float pulseFactor = (Mathf.Sin(colorTime) + 1f) * 0.5f; // 0 to 1
            Color newColor = Color.Lerp(originalColor, pulseColor, pulseFactor * pulseIntensity);
            antImage.color = newColor;
        }
        
        private void UpdateCrawlingAnimation()
        {
            crawlTime += Time.deltaTime * animationSpeed;
            
            // Calculate progress through crawl cycle
            float cycleProgress = crawlTime / crawlDuration;
            
            if (crawlLoop)
            {
                // Loop the crawling
                cycleProgress = cycleProgress % 1f;
            }
            else
            {
                // Clamp to end of animation
                cycleProgress = Mathf.Clamp01(cycleProgress);
            }
            
            // Apply easing curve
            float easedProgress = crawlCurve.Evaluate(cycleProgress);
            
            // Calculate position along crawl path
            Vector2 crawlPosition = Vector2.Lerp(crawlStartPoint, crawlEndPoint, easedProgress);
            Vector2 finalPosition = originalPosition + crawlPosition;
            
            // Add walking bobbing effect
            if (enableWalkingBob)
            {
                float bobOffset = Mathf.Sin(crawlTime * bobSpeed) * bobHeight;
                finalPosition.y += bobOffset;
            }
            
            // Apply breathing while crawling (smaller effect)
            if (enableBreathing)
            {
                float breathingFactor = 1f + Mathf.Sin(breathingTime) * (breathingScale * 0.5f); // Reduced breathing while moving
                rectTransform.localScale = originalScale * breathingFactor;
            }
            
            rectTransform.anchoredPosition = finalPosition;
            
            // Optional: Face movement direction
            if (enableRotation)
            {
                Vector2 direction = crawlEndPoint - crawlStartPoint;
                if (direction.x < 0)
                {
                    // Moving left - flip or rotate slightly
                    rectTransform.localScale = new Vector3(-Mathf.Abs(rectTransform.localScale.x), rectTransform.localScale.y, rectTransform.localScale.z);
                }
                else
                {
                    // Moving right - normal orientation
                    rectTransform.localScale = new Vector3(Mathf.Abs(rectTransform.localScale.x), rectTransform.localScale.y, rectTransform.localScale.z);
                }
            }
        }
        
        [ContextMenu("Reset Animation")]
        public void ResetAnimation()
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = originalScale;
                rectTransform.anchoredPosition = originalPosition;
                rectTransform.localEulerAngles = originalRotation;
            }
            
            if (antImage != null)
            {
                antImage.color = originalColor;
            }
            
            // Reset timers
            breathingTime = 0f;
            movementTime = 0f;
            rotationTime = 0f;
            colorTime = 0f;
            crawlTime = 0f;
        }
        
        [ContextMenu("Test Breathing Only")]
        public void TestBreathingOnly()
        {
            enableBreathing = true;
            enableIdleMovement = false;
            enableRotation = false;
            enableColorPulse = false;
        }
        
        [ContextMenu("Test All Animations")]
        public void TestAllAnimations()
        {
            enableBreathing = true;
            enableIdleMovement = true;
            enableRotation = true;
            enableColorPulse = false; // Keep color normal
            enableCrawling = false;
        }
        
        [ContextMenu("Test Crawling")]
        public void TestCrawling()
        {
            enableCrawling = true;
            enableBreathing = true;
            enableWalkingBob = true;
            enableIdleMovement = false; // Disable idle movement when crawling
            enableRotation = true; // Enable to show direction facing
            crawlTime = 0f; // Reset crawl animation
        }
        
        [ContextMenu("Stop Crawling")]
        public void StopCrawling()
        {
            enableCrawling = false;
            ResetAnimation();
        }
        
        /// <summary>
        /// Enable/disable animations
        /// </summary>
        public void SetAnimationEnabled(bool enabled)
        {
            enableAnimation = enabled;
            if (!enabled)
            {
                ResetAnimation();
            }
        }
        
        /// <summary>
        /// Set animation speed multiplier
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            animationSpeed = Mathf.Max(0f, speed);
        }
    }
}
