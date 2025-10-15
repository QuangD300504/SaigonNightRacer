using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Centralized HUD controller that manages all in-game UI elements
/// Integrates with existing GameManager and ScoreManager systems
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Score text (reuse existing ScoreText)")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("Speed text (reuse existing SpeedText)")]
    public TextMeshProUGUI speedText;
    
    [Tooltip("High score text (reuse existing HighScoreText)")]
    public TextMeshProUGUI highScoreText;
    
    [Tooltip("Lives container with heart icons")]
    public Transform livesContainer;
    
    [Tooltip("Heart icon prefab for lives display")]
    public GameObject heartIconPrefab;

    [Header("Boost UI")]
    [Tooltip("Boost bar slider")]
    public Slider boostBar;
    
    [Tooltip("Boost bar fill image")]
    public Image boostBarFill;
    
    [Tooltip("Boost ready indicator")]
    public Image boostReadyIcon;

    [Header("Heart Sprites")]
    [Tooltip("Full heart sprite")]
    public Sprite fullHeartSprite;
    
    [Tooltip("Empty heart sprite")]
    public Sprite emptyHeartSprite;

    [Header("UI Styling")]
    [Tooltip("Background panel for HUD elements")]
    public Image hudBackground;
    
    [Tooltip("Background color with alpha")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.4f);
    
    [Tooltip("Pixel font for retro styling")]
    public TMP_FontAsset pixelFont;
    
    [Tooltip("Text outline color")]
    public Color textOutlineColor = Color.black;
    
    [Tooltip("Text outline width")]
    public float textOutlineWidth = 0.2f;
    
    [Header("Dynamic Text Colors")]
    [Tooltip("Color for speed values")]
    public Color speedValueColor = Color.cyan;
    
    [Tooltip("Color for score values")]
    public Color scoreValueColor = Color.yellow;
    
    [Tooltip("Color for high score values")]
    public Color highScoreValueColor = Color.green;
    
    [Header("Buff Colors")]
    [Tooltip("Color for invincible buff")]
    public Color invincibleColor = Color.magenta;
    
    [Tooltip("Color for speed boost buff")]
    public Color speedBoostColor = Color.yellow;
    
    [Tooltip("Color for shield buff")]
    public Color shieldColor = Color.cyan;
    
    [Tooltip("Color for obstacle pass-through cheat")]
    public Color obstaclePassThroughColor = Color.red;
    
    [Tooltip("Color for buff text outline")]
    public Color buffOutlineColor = Color.white;
    
    [Tooltip("Buff text outline width")]
    public float buffOutlineWidth = 0.3f;

    [Header("Boost Colors")]
    [Tooltip("Boost bar ready color")]
    public Color boostReadyColor = Color.cyan;
    
    [Tooltip("Boost bar cooldown color")]
    public Color boostCooldownColor = Color.red;
    
    [Tooltip("Boost bar active color")]
    public Color boostActiveColor = Color.yellow;
    
    [Header("Buff Status UI")]
    [Tooltip("Text to show active buffs")]
    public TextMeshProUGUI buffStatusText;

    // Private variables
    private GameManager gameManager;
    private ScoreManager scoreManager;
    private Image[] heartIcons;
    private int maxLives = 3; // Will be updated dynamically
    
    // Boost tracking
    private BikeController bikeController;
    
    // Buff state tracking
    private bool lastShieldState = false;
    private bool lastSpeedBoostState = false;
    
    // Heart sizing constants
    private const float heartWidth = 60f; // Heart icon width (increased for fuller look)
    private const float heartHeight = 40f; // Heart icon height (increased proportionally)
    
    // Animation variables
    private Coroutine heartFlashRoutine;
    private Coroutine buffFlashRoutine;
    private Coroutine invincibleFlashRoutine;
    private Coroutine speedBoostFlashRoutine;
    private Coroutine shieldFlashRoutine;
    
    // Speed color feedback
    private Color normalSpeedColor = Color.white;
    private Color highSpeedColor = Color.red;
    private float speedThreshold = 100f; // km/h

    void Start()
    {
        // Find managers
        gameManager = GameManager.Instance;
        scoreManager = ScoreManager.Instance;
        
        // Find bike controller for boost tracking
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            bikeController = player.GetComponent<BikeController>();
        }
        
        // Get max lives from GameManager
        if (gameManager != null)
        {
            maxLives = gameManager.lives; // Use GameManager's lives as max
        }

        // Fix speed text wrapping issue
        SetupSpeedText();

        // Setup HUD background
        SetupHudBackground();
        
        // Apply pixel styling to all text
        ApplyPixelStyling();

        // Setup lives display
        SetupLivesDisplay();

        // Subscribe to GameManager events
        SubscribeToEvents();

        // Validate references
        ValidateReferences();
    }

    /// <summary>
    /// Subscribe to GameManager events for automatic UI updates
    /// </summary>
    private void SubscribeToEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnScoreChanged.AddListener(OnScoreChanged);
            gameManager.OnLivesChanged.AddListener(OnLivesChanged);
            gameManager.OnSpeedChanged.AddListener(OnSpeedChanged);
        }
    }

    /// <summary>
    /// Unsubscribe from events when destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnScoreChanged.RemoveListener(OnScoreChanged);
            gameManager.OnLivesChanged.RemoveListener(OnLivesChanged);
            gameManager.OnSpeedChanged.RemoveListener(OnSpeedChanged);
        }
    }

    /// <summary>
    /// Event handler for score changes
    /// </summary>
    private void OnScoreChanged(int currentScore, int highScore)
    {
        UpdateScoreDisplay(currentScore, highScore);
    }

    /// <summary>
    /// Event handler for lives changes
    /// </summary>
    private void OnLivesChanged(int lives)
    {
        UpdateLivesDisplay(lives);
    }

    /// <summary>
    /// Event handler for speed changes
    /// </summary>
    private void OnSpeedChanged(float speedKmh)
    {
        UpdateSpeedDisplay(speedKmh);
    }

    /// <summary>
    /// Setup speed text to prevent line wrapping and align with hearts
    /// </summary>
    private void SetupSpeedText()
    {
        if (speedText != null)
        {
            // Disable text wrapping to prevent line breaks
            speedText.textWrappingMode = TextWrappingModes.NoWrap;
            speedText.overflowMode = TextOverflowModes.Overflow;
            
            // Simple alignment like hearts - just set position with padding
            RectTransform speedRect = speedText.GetComponent<RectTransform>();
            if (speedRect != null)
            {
                speedRect.anchoredPosition = new Vector2(20f, speedRect.anchoredPosition.y);
            }
        }
    }

    /// <summary>
    /// Setup HUD background for better readability
    /// </summary>
    private void SetupHudBackground()
    {
        if (hudBackground != null)
        {
            hudBackground.color = backgroundColor;
        }
    }
    
    /// <summary>
    /// Apply pixel art styling to all text elements
    /// </summary>
    private void ApplyPixelStyling()
    {
        // Apply pixel font to all text elements
        if (pixelFont != null)
        {
            if (scoreText != null)
            {
                scoreText.font = pixelFont;
                ApplyTextOutline(scoreText);
            }
            
            if (speedText != null)
            {
                speedText.font = pixelFont;
                ApplyTextOutline(speedText);
            }
            
            if (highScoreText != null)
            {
                highScoreText.font = pixelFont;
                ApplyTextOutline(highScoreText);
            }
            
                if (buffStatusText != null)
                {
                    buffStatusText.font = pixelFont;
                    ApplyTextOutline(buffStatusText, true); // Use buff text styling
                }
        }
    }
    
    /// <summary>
    /// Apply outline effect to text for better readability
    /// </summary>
    private void ApplyTextOutline(TextMeshProUGUI text, bool isBuffText = false)
    {
        if (text != null)
        {
            // Enable outline with different settings for buff text
            if (isBuffText)
            {
                text.outlineWidth = buffOutlineWidth;
                text.outlineColor = buffOutlineColor;
            }
            else
            {
                text.outlineWidth = textOutlineWidth;
                text.outlineColor = textOutlineColor;
            }
            
            // Set font size for pixel art (typically smaller for crisp look)
            text.fontSize = 24f; // Adjust as needed for your pixel art scale
        }
    }

    /// <summary>
    /// Setup lives display with heart icons
    /// </summary>
    private void SetupLivesDisplay()
    {
        if (livesContainer == null || heartIconPrefab == null) return;

        // Clear existing hearts
        foreach (Transform child in livesContainer)
        {
            Destroy(child.gameObject);
        }

        // Create heart icons
        heartIcons = new Image[maxLives];
        for (int i = 0; i < maxLives; i++)
        {
            GameObject heartObj = Instantiate(heartIconPrefab, livesContainer);
            heartIcons[i] = heartObj.GetComponent<Image>();
            
            // Ensure proper sizing for heart icons
            RectTransform heartRect = heartObj.GetComponent<RectTransform>();
            heartRect.sizeDelta = new Vector2(heartWidth, heartHeight); // Use separate width and height
            
            // Set proper anchor for consistent positioning
            heartRect.anchorMin = new Vector2(0, 0.5f);
            heartRect.anchorMax = new Vector2(0, 0.5f);
        }
        
        // Dynamically resize the container based on number of hearts
        ResizeLivesContainer();
    }

    /// <summary>
    /// Resize the lives container to fit the number of hearts properly
    /// </summary>
    private void ResizeLivesContainer()
    {
        if (livesContainer == null) return;
        
        RectTransform containerRect = livesContainer.GetComponent<RectTransform>();
        if (containerRect == null) return;
        
        // Calculate container width based on number of hearts
        float spacing = 10f; // Spacing between hearts (from HorizontalLayoutGroup)
        float padding = 20f; // Left padding for better spacing
        
        float containerWidth = (heartWidth * maxLives) + (spacing * (maxLives - 1)) + (padding * 2);
        float containerHeight = 50f; // Fixed height
        
        // Set container size
        containerRect.sizeDelta = new Vector2(containerWidth, containerHeight);
        
        // Position container with left padding
        containerRect.anchoredPosition = new Vector2(padding, containerRect.anchoredPosition.y);
        
        // Set pivot to left edge so hearts start from left
        containerRect.pivot = new Vector2(0, 0.5f);
        
        // Get HorizontalLayoutGroup and set alignment to Upper Left
        HorizontalLayoutGroup layoutGroup = livesContainer.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
        }
    }

    /// <summary>
    /// Update score display via events (called when score changes)
    /// </summary>
    private void UpdateScoreDisplay(int currentScore, int highScore)
    {
        if (scoreText != null)
        {
            // Split text into label and value for color coding
            string labelText = "SCORE: ";
            string valueText = currentScore.ToString("N0");
            scoreText.text = $"<color=white>{labelText}</color><color=#{ColorUtility.ToHtmlStringRGB(scoreValueColor)}>{valueText}</color>";
        }
        
        if (highScoreText != null)
        {
            // Split text into label and value for color coding
            string labelText = "HIGH SCORE: ";
            string valueText = highScore.ToString("N0");
            highScoreText.text = $"<color=white>{labelText}</color><color=#{ColorUtility.ToHtmlStringRGB(highScoreValueColor)}>{valueText}</color>";
        }
    }

    void Update()
    {
        // Update boost display
        UpdateBoostDisplay();
        
        // Update buff status display
        UpdateBuffStatusDisplay();
    }

    /// <summary>
    /// Update speed display via events (called when speed changes)
    /// </summary>
    private void UpdateSpeedDisplay(float speedKmh)
    {
        if (speedText != null)
        {
            // Split text into label and value for color coding
            string labelText = "SPEED: ";
            string valueText = $"{speedKmh:F0}km/h";
            
            // Enhanced color feedback based on speed
            Color valueColor;
            if (speedKmh > speedThreshold * 1.5f)
            {
                valueColor = Color.red; // Very high speed - red
            }
            else if (speedKmh > speedThreshold)
            {
                valueColor = Color.yellow; // High speed - yellow
            }
            else if (speedKmh > speedThreshold * 0.5f)
            {
                valueColor = speedValueColor; // Medium speed - cyan (from settings)
            }
            else
            {
                valueColor = Color.white; // Low speed - white
            }
            
            speedText.text = $"<color=white>{labelText}</color><color=#{ColorUtility.ToHtmlStringRGB(valueColor)}>{valueText}</color>";
        }
    }

    /// <summary>
    /// Update lives display via events (called when lives change)
    /// </summary>
    private void UpdateLivesDisplay(int lives)
    {
        if (gameManager == null) return;
        
        // Update maxLives from GameManager (in case it changed in Inspector)
        int currentMaxLives = lives;
        
        // Check if we need to recreate hearts (if max lives changed)
        if (heartIcons == null || heartIcons.Length != currentMaxLives)
        {
            maxLives = currentMaxLives; // Update maxLives
            SetupLivesDisplay();
        }
        
        if (heartIcons == null) return;

        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
            {
                // Show full heart if player has that life, empty otherwise
                bool hasLife = i < lives;
                heartIcons[i].sprite = hasLife ? fullHeartSprite : emptyHeartSprite;
                
                // Optional: Add color tinting
                heartIcons[i].color = hasLife ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }
        }
        
        // Flash hearts when losing life
        if (heartFlashRoutine != null) StopCoroutine(heartFlashRoutine);
        heartFlashRoutine = StartCoroutine(FlashHearts());
    }

    /// <summary>
    /// Flash hearts animation when losing a life
    /// </summary>
    private IEnumerator FlashHearts()
    {
        for (int i = 0; i < 3; i++)
        {
            livesContainer.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            livesContainer.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Update boost display
    /// </summary>
    private void UpdateBoostDisplay()
    {
        if (bikeController == null || boostBar == null) return;
        
        float boostProgress = bikeController.GetBoostCooldownProgress();
        bool canBoost = bikeController.CanBoost();
        bool isBoosting = bikeController.IsBoosting();
        
        // Update boost bar
        boostBar.value = boostProgress;
        
        // Update boost bar color
        if (boostBarFill != null)
        {
            if (isBoosting)
                boostBarFill.color = boostActiveColor;
            else if (canBoost)
                boostBarFill.color = boostReadyColor;
            else
                boostBarFill.color = boostCooldownColor;
        }
        
        // Update boost ready icon
        if (boostReadyIcon != null)
        {
            boostReadyIcon.gameObject.SetActive(canBoost);
        }
    }

    /// <summary>
    /// Update buff status display
    /// </summary>
    private void UpdateBuffStatusDisplay()
    {
        if (bikeController == null || buffStatusText == null) return;
        
        string buffText = "";
        bool hasActiveBuffs = false;
        
        // Check for shield
        bool isShielded = bikeController.IsShielded();
        if (isShielded != lastShieldState)
        {
            lastShieldState = isShielded;
        }
        
        if (isShielded)
        {
            buffText += "SHIELD ACTIVE ";
            hasActiveBuffs = true;
        }
        
        // Check for speed boost
        bool isSpeedBoosted = bikeController.IsSpeedBoosted();
        if (isSpeedBoosted != lastSpeedBoostState)
        {
            lastSpeedBoostState = isSpeedBoosted;
        }
        
        if (isSpeedBoosted)
        {
            buffText += "SPEED BOOST ACTIVE ";
            hasActiveBuffs = true;
        }
        
        // Check for invincibility
        bool isInvincible = bikeController.IsInvincible();
        if (isInvincible)
        {
            buffText += "INVINCIBLE ";
            hasActiveBuffs = true;
        }
        
        // Check for invincible mode cheat
        var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
        if (obstacleSpawner != null && obstacleSpawner.IsInvincibleModeEnabled())
        {
            buffText += "INVINCIBLE MODE ";
            hasActiveBuffs = true;
        }
        
        // Update text with enhanced styling
        if (hasActiveBuffs)
        {
            // Apply enhanced outline for active buffs
            buffStatusText.outlineWidth = buffOutlineWidth;
            buffStatusText.outlineColor = buffOutlineColor;
            
            // Start flashing animation if not already running
            if (buffFlashRoutine == null)
            {
                buffFlashRoutine = StartCoroutine(FlashBuffText());
            }
        }
        else
        {
            // Stop flashing animation
            if (buffFlashRoutine != null)
            {
                StopCoroutine(buffFlashRoutine);
                buffFlashRoutine = null;
            }
            
            // Reset to normal styling
            buffStatusText.outlineWidth = textOutlineWidth;
            buffStatusText.outlineColor = textOutlineColor;
        }
        
        buffStatusText.gameObject.SetActive(hasActiveBuffs);
    }

    /// <summary>
    /// Flash buff text for attention-grabbing effect
    /// </summary>
    private IEnumerator FlashBuffText()
    {
        while (buffStatusText != null && buffStatusText.gameObject.activeInHierarchy)
        {
            // Get current buff states
            bool isShielded = bikeController != null && bikeController.IsShielded();
            bool isSpeedBoosted = bikeController != null && bikeController.IsSpeedBoosted();
            bool isInvincible = bikeController != null && bikeController.IsInvincible();
            var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
            bool isInvincibleMode = obstacleSpawner != null && obstacleSpawner.IsInvincibleModeEnabled();
            
            // Build colored text for each active buff
            string coloredText = "";
            
            if (isShielded)
            {
                Color flashColor = Color.Lerp(shieldColor, Color.white, 0.4f);
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(flashColor)}>SHIELD ACTIVE </color>";
            }
            
            if (isSpeedBoosted)
            {
                Color flashColor = Color.Lerp(speedBoostColor, Color.white, 0.4f);
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(flashColor)}>SPEED BOOST ACTIVE </color>";
            }
            
            if (isInvincible)
            {
                Color flashColor = Color.Lerp(invincibleColor, Color.white, 0.4f);
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(flashColor)}>INVINCIBLE </color>";
            }
            
            if (isInvincibleMode)
            {
                Color flashColor = Color.Lerp(invincibleColor, Color.white, 0.4f);
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(flashColor)}>INVINCIBLE MODE </color>";
            }
            
            buffStatusText.text = coloredText;
            
            yield return new WaitForSeconds(0.4f);
            
            // Return to normal colors
            coloredText = "";
            
            if (isShielded)
            {
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(shieldColor)}>SHIELD ACTIVE </color>";
            }
            
            if (isSpeedBoosted)
            {
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(speedBoostColor)}>SPEED BOOST ACTIVE </color>";
            }
            
            if (isInvincible)
            {
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(invincibleColor)}>INVINCIBLE </color>";
            }
            
            if (isInvincibleMode)
            {
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(invincibleColor)}>INVINCIBLE MODE </color>";
            }
            
            buffStatusText.text = coloredText;
            
            yield return new WaitForSeconds(0.4f);
        }
        
        buffFlashRoutine = null;
    }

    /// <summary>
    /// Show collectible notification
    /// </summary>
    public void ShowCollectibleNotification(string message, Color color)
    {
        // TODO: Add UI notification system here if needed
        // Could show floating text, update UI elements, etc.
    }

    /// <summary>
    /// Validate that all required references are assigned
    /// </summary>
    private void ValidateReferences()
    {
        if (scoreText == null) Debug.LogWarning("HUDController: ScoreText not assigned!");
        if (speedText == null) Debug.LogWarning("HUDController: SpeedText not assigned!");
        if (highScoreText == null) Debug.LogWarning("HUDController: HighScoreText not assigned!");
        if (livesContainer == null) Debug.LogWarning("HUDController: LivesContainer not assigned!");
        if (heartIconPrefab == null) Debug.LogWarning("HUDController: HeartIconPrefab not assigned!");
        if (fullHeartSprite == null) Debug.LogWarning("HUDController: FullHeartSprite not assigned!");
        if (emptyHeartSprite == null) Debug.LogWarning("HUDController: EmptyHeartSprite not assigned!");
    }

}
