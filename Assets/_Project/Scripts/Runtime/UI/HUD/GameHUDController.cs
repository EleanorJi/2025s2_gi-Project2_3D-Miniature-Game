using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Antventure.UI.HUD
{
    /// <summary>
    /// Game HUD Controller
    /// </summary>
    public class GameHUDController : MonoBehaviour
    {
        [Header("Health Display")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Color healthFullColor = Color.green;
        [SerializeField] private Color healthLowColor = Color.red;
        [SerializeField] private float healthLowThreshold = 0.3f;

        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private GameObject scorePopupPrefab;
        [SerializeField] private Transform scorePopupParent;

        [Header("Timer Display")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerIcon;
        [SerializeField] private Color timerNormalColor = Color.white;
        [SerializeField] private Color timerWarningColor = Color.red;
        [SerializeField] private float timerWarningThreshold = 30f;

        [Header("Lives Display")]
        [SerializeField] private Transform livesContainer;
        [SerializeField] private GameObject lifeIconPrefab;
        [SerializeField] private Sprite fullLifeSprite;
        [SerializeField] private Sprite emptyLifeSprite;

        [Header("Power-ups Display")]
        [SerializeField] private Transform powerupsContainer;
        [SerializeField] private GameObject powerupIconPrefab;

        [Header("Mini Map")]
        [SerializeField] private RawImage miniMapImage;
        [SerializeField] private Transform playerMarker;
        [SerializeField] private Camera miniMapCamera;

        [Header("Interaction Prompts")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TextMeshProUGUI interactionText;
        [SerializeField] private Image interactionIcon;

        [Header("Game State")]
        [SerializeField] private GameObject pauseIndicator;
        [SerializeField] private TextMeshProUGUI gameStateText;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Game data
        private int currentHealth = 100;
        private int maxHealth = 100;
        private int currentScore = 0;
        private int highScore = 0;
        private float currentTime = 0f;
        private int currentLives = 3;
        private int maxLives = 3;

        // Animation coroutines
        private Coroutine healthAnimationCoroutine;
        private Coroutine scoreAnimationCoroutine;

        private void Start()
        {
            InitializeHUD();
            LoadHighScore();
        }

        /// <summary>
        /// Initialize HUD
        /// </summary>
        private void InitializeHUD()
        {
            UpdateHealthDisplay();
            UpdateScoreDisplay();
            UpdateLivesDisplay();
            UpdateTimerDisplay();
            
            // Hide interaction prompt
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }

            // Hide pause indicator
            if (pauseIndicator != null)
            {
                pauseIndicator.SetActive(false);
            }
        }

        #region Health Management

        /// <summary>
        /// Set health value
        /// </summary>
        public void SetHealth(int health)
        {
            int previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            
            if (currentHealth != previousHealth)
            {
                UpdateHealthDisplay();
                
                // If health decreased, play damage animation
                if (currentHealth < previousHealth)
                {
                    PlayHealthDamageAnimation();
                }
            }
        }

        /// <summary>
        /// Set maximum health value
        /// </summary>
        public void SetMaxHealth(int maxHealth)
        {
            this.maxHealth = maxHealth;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthDisplay();
        }

        /// <summary>
        /// Update health display
        /// </summary>
        private void UpdateHealthDisplay()
        {
            if (healthSlider != null)
            {
                float healthPercentage = (float)currentHealth / maxHealth;
                
                if (healthAnimationCoroutine != null)
                {
                    StopCoroutine(healthAnimationCoroutine);
                }
                
                healthAnimationCoroutine = StartCoroutine(AnimateHealthSlider(healthPercentage));
            }

            if (healthText != null)
            {
                healthText.text = $"{currentHealth}/{maxHealth}";
            }

            // Update health color
            if (healthFillImage != null)
            {
                float healthPercentage = (float)currentHealth / maxHealth;
                healthFillImage.color = Color.Lerp(healthLowColor, healthFullColor, healthPercentage);
            }
        }

        /// <summary>
        /// Health slider animation
        /// </summary>
        private IEnumerator AnimateHealthSlider(float targetValue)
        {
            float startValue = healthSlider.value;
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / animationDuration;
                float curveValue = animationCurve.Evaluate(progress);
                
                healthSlider.value = Mathf.Lerp(startValue, targetValue, curveValue);
                yield return null;
            }

            healthSlider.value = targetValue;
        }

        /// <summary>
        /// Play damage animation
        /// </summary>
        private void PlayHealthDamageAnimation()
        {
            // Here you can add screen flash red, vibration and other effects
            StartCoroutine(FlashHealthBar());
        }

        /// <summary>
        /// Health bar flash effect
        /// </summary>
        private IEnumerator FlashHealthBar()
        {
            if (healthFillImage != null)
            {
                Color originalColor = healthFillImage.color;
                
                for (int i = 0; i < 3; i++)
                {
                    healthFillImage.color = Color.white;
                    yield return new WaitForSeconds(0.1f);
                    healthFillImage.color = originalColor;
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        #endregion

        #region Score Management

        /// <summary>
        /// Add score points
        /// </summary>
        public void AddScore(int points)
        {
            currentScore += points;
            UpdateScoreDisplay();
            
            // Show score popup effect
            ShowScorePopup(points);
            
            // Check if new high score is achieved
            if (currentScore > highScore)
            {
                highScore = currentScore;
                SaveHighScore();
                UpdateHighScoreDisplay();
            }
        }

        /// <summary>
        /// Set score value
        /// </summary>
        public void SetScore(int score)
        {
            currentScore = score;
            UpdateScoreDisplay();
        }

        /// <summary>
        /// Update score display
        /// </summary>
        private void UpdateScoreDisplay()
        {
            if (scoreText != null)
            {
                if (scoreAnimationCoroutine != null)
                {
                    StopCoroutine(scoreAnimationCoroutine);
                }
                
                scoreAnimationCoroutine = StartCoroutine(AnimateScoreText());
            }
        }

        /// <summary>
        /// Score text animation
        /// </summary>
        private IEnumerator AnimateScoreText()
        {
            scoreText.text = currentScore.ToString("N0");
            
            // Scale animation
            Vector3 originalScale = scoreText.transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;
            
            float elapsedTime = 0f;
            while (elapsedTime < animationDuration / 2)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (animationDuration / 2);
                scoreText.transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
                yield return null;
            }
            
            elapsedTime = 0f;
            while (elapsedTime < animationDuration / 2)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (animationDuration / 2);
                scoreText.transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
                yield return null;
            }
            
            scoreText.transform.localScale = originalScale;
        }

        /// <summary>
        /// Show score popup effect
        /// </summary>
        private void ShowScorePopup(int points)
        {
            if (scorePopupPrefab != null && scorePopupParent != null)
            {
                GameObject popup = Instantiate(scorePopupPrefab, scorePopupParent);
                TextMeshProUGUI popupText = popup.GetComponent<TextMeshProUGUI>();
                
                if (popupText != null)
                {
                    popupText.text = "+" + points.ToString();
                }
                
                // Start popup animation
                StartCoroutine(AnimateScorePopup(popup));
            }
        }

        /// <summary>
        /// Score popup animation
        /// </summary>
        private IEnumerator AnimateScorePopup(GameObject popup)
        {
            Vector3 startPos = popup.transform.localPosition;
            Vector3 endPos = startPos + Vector3.up * 100f;
            
            CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = popup.AddComponent<CanvasGroup>();
            }
            
            float elapsedTime = 0f;
            float duration = 1f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                
                popup.transform.localPosition = Vector3.Lerp(startPos, endPos, progress);
                canvasGroup.alpha = 1f - progress;
                
                yield return null;
            }
            
            Destroy(popup);
        }

        /// <summary>
        /// Update high score display
        /// </summary>
        private void UpdateHighScoreDisplay()
        {
            if (highScoreText != null)
            {
                highScoreText.text = "High Score: " + highScore.ToString("N0");
            }
        }

        /// <summary>
        /// Save high score
        /// </summary>
        private void SaveHighScore()
        {
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load high score
        /// </summary>
        private void LoadHighScore()
        {
            highScore = PlayerPrefs.GetInt("HighScore", 0);
            UpdateHighScoreDisplay();
        }

        #endregion

        #region Timer Management

        /// <summary>
        /// Set timer time
        /// </summary>
        public void SetTimer(float time)
        {
            currentTime = time;
            UpdateTimerDisplay();
        }

        /// <summary>
        /// Update timer display
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(currentTime / 60);
                int seconds = Mathf.FloorToInt(currentTime % 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                
                // Timer warning effect
                if (currentTime <= timerWarningThreshold)
                {
                    timerText.color = timerWarningColor;
                    if (timerIcon != null)
                    {
                        timerIcon.color = timerWarningColor;
                    }
                }
                else
                {
                    timerText.color = timerNormalColor;
                    if (timerIcon != null)
                    {
                        timerIcon.color = timerNormalColor;
                    }
                }
            }
        }

        #endregion

        #region Lives Management

        /// <summary>
        /// Set number of lives
        /// </summary>
        public void SetLives(int lives)
        {
            currentLives = Mathf.Clamp(lives, 0, maxLives);
            UpdateLivesDisplay();
        }

        /// <summary>
        /// Update lives display
        /// </summary>
        private void UpdateLivesDisplay()
        {
            if (livesContainer == null || lifeIconPrefab == null) return;

            // Clear existing life icons
            foreach (Transform child in livesContainer)
            {
                Destroy(child.gameObject);
            }

            // Create new life icons
            for (int i = 0; i < maxLives; i++)
            {
                GameObject lifeIcon = Instantiate(lifeIconPrefab, livesContainer);
                Image iconImage = lifeIcon.GetComponent<Image>();
                
                if (iconImage != null)
                {
                    iconImage.sprite = i < currentLives ? fullLifeSprite : emptyLifeSprite;
                }
            }
        }

        #endregion

        #region Interaction Prompts

        /// <summary>
        /// Show interaction prompt
        /// </summary>
        public void ShowInteractionPrompt(string text, Sprite icon = null)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
                
                if (interactionText != null)
                {
                    interactionText.text = text;
                }
                
                if (interactionIcon != null && icon != null)
                {
                    interactionIcon.sprite = icon;
                    interactionIcon.gameObject.SetActive(true);
                }
                else if (interactionIcon != null)
                {
                    interactionIcon.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Hide interaction prompt
        /// </summary>
        public void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        #endregion

        #region Game State

        /// <summary>
        /// Show pause indicator
        /// </summary>
        public void ShowPauseIndicator()
        {
            if (pauseIndicator != null)
            {
                pauseIndicator.SetActive(true);
            }
        }

        /// <summary>
        /// Hide pause indicator
        /// </summary>
        public void HidePauseIndicator()
        {
            if (pauseIndicator != null)
            {
                pauseIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Set game state text
        /// </summary>
        public void SetGameStateText(string text)
        {
            if (gameStateText != null)
            {
                gameStateText.text = text;
            }
        }

        #endregion

        #region Public Getters

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public int CurrentScore => currentScore;
        public int HighScore => highScore;
        public float CurrentTime => currentTime;
        public int CurrentLives => currentLives;

        #endregion
    }
}

