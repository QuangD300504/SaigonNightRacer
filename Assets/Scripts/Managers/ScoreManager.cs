using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Advanced score system for bike chase game
/// Tracks distance, airtime, flips, survival time, and car proximity bonuses
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Main bike transform (Player root)")]
    public Transform player;
    
    [Tooltip("Front wheel Rigidbody2D for ground detection")]
    public Rigidbody2D frontWheelRB;
    
    [Tooltip("Back wheel Rigidbody2D for ground detection")]
    public Rigidbody2D backWheelRB;
    
    [Tooltip("UI Text component to display score")]
    public TextMeshProUGUI scoreText;
    
    [Header("Scoring Values")]
    [Tooltip("Points per unit distance traveled")]
    public float distanceMultiplier = 10f;
    
    [Tooltip("Points per second survived")]
    public float survivalMultiplier = 5f;
    
    [Tooltip("Points per second airborne")]
    public float airMultiplier = 50f;
    
    [Tooltip("Points for completing a full flip (360°)")]
    public int flipBonus = 500;

    [Header("Animation")]
    [Tooltip("Animate score text when score changes")]
    public bool animateScoreChanges = true;
    
    [Tooltip("Animation duration in seconds")]
    public float animationDuration = 0.3f;
    
    [Tooltip("Scale multiplier for animation")]
    public float scaleMultiplier = 1.2f;

    [Header("Movement Tracking")]
    [Tooltip("Minimum speed to count as moving (units/second)")]
    public float movementThreshold = 0.5f;

    [Header("Debug")]
    [Tooltip("Show detailed score breakdown in console")]
    public bool showDebugInfo = false;

    // Private variables
    private float startX;
    private float totalScore;
    private float airTimer;
    private bool inAir;
    private float lastRotation;
    private float survivalTime;
    private int flipCount;
    
    // Score breakdown for debugging
    private float distanceScore;
    private float survivalScore;
    private float airScore;
    
    // Animation variables
    private bool isAnimating = false;
    private float animationTimer = 0f;
    private Vector3 originalScale;
    private float lastDisplayedScore = 0f;
    
    // Movement tracking variables
    private float maxDistanceX;
    private float lastX;
    private float lastMoveTime;
    private bool isMoving;

    public static ScoreManager Instance { get; private set; }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Initialize starting position
        if (player != null)
        {
            startX = player.position.x;
        }
        
        // Initialize rotation tracking
        if (player != null)
        {
            lastRotation = player.eulerAngles.z;
        }
        
        // Store original scale for animation
        if (scoreText != null)
        {
            originalScale = scoreText.transform.localScale;
        }
        
        // Initialize movement tracking
        maxDistanceX = startX;
        lastX = startX;
        lastMoveTime = Time.time;
        isMoving = false;
        
        // Initialize ground layer mask
        if (frontWheelRB == null || backWheelRB == null)
        {
            Debug.LogWarning("ScoreManager: Front or back wheel Rigidbody2D not assigned!");
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateMovementTracking();
        UpdateDistanceScore();
        UpdateSurvivalScore();
        UpdateAirTime();
        CheckFlips();
        UpdateUI();
        HandleAnimation();
        
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }

    /// <summary>
    /// Track player movement and update max distance
    /// </summary>
    private void UpdateMovementTracking()
    {
        float currentX = player.position.x;
        
        // Update max distance reached (never decreases)
        if (currentX > maxDistanceX)
        {
            maxDistanceX = currentX;
        }
        
        // Check if player is moving
        float speed = Mathf.Abs(player.GetComponent<Rigidbody2D>().linearVelocity.x);
        isMoving = speed > movementThreshold;
        
        // Update movement timer
        if (Mathf.Abs(currentX - lastX) > 0.1f)
        {
            lastMoveTime = Time.time;
            lastX = currentX;
        }
    }

    /// <summary>
    /// Calculate distance-based score (using max distance reached)
    /// </summary>
    private void UpdateDistanceScore()
    {
        float distance = maxDistanceX - startX;
        distanceScore = distance * distanceMultiplier;
    }

    /// <summary>
    /// Calculate survival time bonus (only when moving or airborne)
    /// </summary>
    private void UpdateSurvivalScore()
    {
        // Only add survival points if player is actively doing something
        bool activelyPlaying = isMoving || inAir || (Time.time - lastMoveTime < 1f); // 1 second grace period
        
        if (activelyPlaying)
        {
            survivalTime += Time.deltaTime;
        }
        
        survivalScore = survivalTime * survivalMultiplier;
    }

    /// <summary>
    /// Track air time and award bonus when landing
    /// </summary>
    private void UpdateAirTime()
    {
        if (frontWheelRB == null || backWheelRB == null) return;

        // Check if both wheels are off the ground
        bool frontOffGround = !frontWheelRB.IsTouchingLayers(LayerMask.GetMask("Ground"));
        bool backOffGround = !backWheelRB.IsTouchingLayers(LayerMask.GetMask("Ground"));
        bool bothWheelsOff = frontOffGround && backOffGround;

        if (bothWheelsOff)
        {
            if (!inAir)
            {
                inAir = true;
                airTimer = 0f;
            }
            airTimer += Time.deltaTime;
        }
        else if (inAir)
        {
            // Just landed - award air time bonus
            airScore += airTimer * airMultiplier;
            inAir = false;
            airTimer = 0f;
        }
    }

    /// <summary>
    /// Detect full rotations (flips) while airborne
    /// </summary>
    private void CheckFlips()
    {
        if (!inAir) return; // Only count flips while airborne

        float currentRot = player.eulerAngles.z;
        float delta = Mathf.DeltaAngle(lastRotation, currentRot);
        
        // Accumulate rotation changes
        if (Mathf.Abs(delta) >= 360f)
        {
            flipCount++;
            lastRotation = currentRot; // Reset to avoid multiple counts
        }
    }


    /// <summary>
    /// Update total score and UI display
    /// </summary>
    private void UpdateUI()
    {
        totalScore = distanceScore + survivalScore + airScore + (flipCount * flipBonus);
        
        if (scoreText != null)
        {
            int currentScore = Mathf.FloorToInt(totalScore);
            scoreText.text = "SCORE: " + currentScore.ToString();
            
            // Trigger animation if score changed significantly
            if (animateScoreChanges && Mathf.Abs(currentScore - lastDisplayedScore) >= 10f)
            {
                StartScoreAnimation();
                lastDisplayedScore = currentScore;
            }
        }
    }

    /// <summary>
    /// Debug information display
    /// </summary>
    private void ShowDebugInfo()
    {
        if (Time.frameCount % 60 == 0) // Update every second
        {
            Debug.Log($"Score Breakdown - Distance: {distanceScore:F0}, Survival: {survivalScore:F0}, " +
                     $"Air: {airScore:F0}, Flips: {flipCount}, Total: {totalScore:F0}");
        }
    }

    /// <summary>
    /// Handle score text animation
    /// </summary>
    private void HandleAnimation()
    {
        if (!animateScoreChanges || !isAnimating || scoreText == null) return;

        animationTimer += Time.deltaTime;
        float progress = animationTimer / animationDuration;

        if (progress < 1f)
        {
            // Animate scale up then down
            float scale = Mathf.Lerp(originalScale.x, originalScale.x * scaleMultiplier, 
                Mathf.Sin(progress * Mathf.PI));
            scoreText.transform.localScale = Vector3.one * scale;
        }
        else
        {
            // Reset scale
            scoreText.transform.localScale = originalScale;
            isAnimating = false;
            animationTimer = 0f;
        }
    }

    /// <summary>
    /// Start score animation
    /// </summary>
    public void StartScoreAnimation()
    {
        if (animateScoreChanges && scoreText != null)
        {
            isAnimating = true;
            animationTimer = 0f;
        }
    }

    /// <summary>
    /// Get final score for game over screen
    /// </summary>
    public int GetFinalScore()
    {
        return Mathf.FloorToInt(totalScore);
    }

    /// <summary>
    /// Get individual score components for detailed display
    /// </summary>
    public ScoreBreakdown GetScoreBreakdown()
    {
        return new ScoreBreakdown
        {
            distance = Mathf.FloorToInt(distanceScore),
            survival = Mathf.FloorToInt(survivalScore),
            airtime = Mathf.FloorToInt(airScore),
            flips = flipCount,
            total = Mathf.FloorToInt(totalScore)
        };
    }

    /// <summary>
    /// Reset score for new game
    /// </summary>
    public void ResetScore()
    {
        totalScore = 0f;
        distanceScore = 0f;
        survivalScore = 0f;
        airScore = 0f;
        survivalTime = 0f;
        flipCount = 0;
        airTimer = 0f;
        inAir = false;
        
        // Reset movement tracking
        maxDistanceX = startX;
        lastX = startX;
        lastMoveTime = Time.time;
        isMoving = false;
        
        if (player != null)
        {
            startX = player.position.x;
            lastRotation = player.eulerAngles.z;
        }
    }
}

/// <summary>
/// Data structure for detailed score breakdown
/// </summary>
[System.Serializable]
public struct ScoreBreakdown
{
    public int distance;
    public int survival;
    public int airtime;
    public int flips;
    public int total;
}
