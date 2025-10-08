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
    public float airMultiplier = 20f;
    
    [Tooltip("Points for completing a full flip (360°)")]
    public int flipBonus = 200;

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

    [Header("Cheat Prevention")]
    [Tooltip("Minimum forward speed to count as progress (units/second)")]
    public float minimumForwardSpeed = 0.5f;
    
    [Tooltip("Maximum time without forward progress before score stops")]
    public float maxIdleTime = 3.0f;
    
    [Tooltip("Minimum distance change to count as forward progress")]
    public float minimumProgressDistance = 0.3f;
    
    [Tooltip("Enable comprehensive cheat detection")]
    public bool enableCheatPrevention = true;
    
    [Tooltip("Show cheat detection warnings")]
    public bool showCheatWarnings = false;

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
    
    // Cheat prevention variables
    private float lastProgressTime;
    private float lastProgressX;
    private bool isMakingProgress;
    private float idleStartTime;
    private bool isIdle;
    private int cheatAttempts;
    private float lastCheatWarningTime;

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
        
        // Initialize cheat prevention
        lastProgressTime = Time.time;
        lastProgressX = startX;
        isMakingProgress = false;
        idleStartTime = Time.time;
        isIdle = false;
        cheatAttempts = 0;
        lastCheatWarningTime = 0f;
        
        // Initialize ground layer mask
        if (frontWheelRB == null || backWheelRB == null)
        {
            Debug.LogWarning("ScoreManager: Front or back wheel Rigidbody2D not assigned!");
        }
        
        if (enableCheatPrevention)
        {
            Debug.Log("=== CHEAT PREVENTION ENABLED ===");
            Debug.Log($"Minimum Forward Speed: {minimumForwardSpeed} units/sec");
            Debug.Log($"Max Idle Time: {maxIdleTime}s");
            Debug.Log($"Min Progress Distance: {minimumProgressDistance} units");
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateMovementTracking();
        UpdateCheatPrevention();
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
    /// Comprehensive cheat prevention system
    /// </summary>
    private void UpdateCheatPrevention()
    {
        if (!enableCheatPrevention) return;
        
        float currentX = player.position.x;
        float currentTime = Time.time;
        Rigidbody2D playerRB = player.GetComponent<Rigidbody2D>();
        
        if (playerRB == null) return;
        
        float currentSpeed = playerRB.linearVelocity.x;
        float distanceFromLastProgress = currentX - lastProgressX;
        
        // Check for forward progress
        bool hasForwardProgress = distanceFromLastProgress >= minimumProgressDistance;
        bool hasForwardSpeed = currentSpeed >= minimumForwardSpeed;
        
        // Update progress tracking
        if (hasForwardProgress && hasForwardSpeed)
        {
            lastProgressTime = currentTime;
            lastProgressX = currentX;
            isMakingProgress = true;
            isIdle = false;
        }
        else
        {
            // Check if player has been idle too long
            float timeSinceProgress = currentTime - lastProgressTime;
            
            if (timeSinceProgress >= maxIdleTime)
            {
                if (!isIdle)
                {
                    isIdle = true;
                    idleStartTime = currentTime;
                    OnCheatDetected("IDLE_EXPLOIT", $"Player idle for {timeSinceProgress:F1}s");
                }
            }
        }
        
        // Detect back-and-forth movement
        if (Mathf.Abs(currentSpeed) > movementThreshold)
        {
            // Check for oscillating movement pattern
            if (Mathf.Sign(currentSpeed) != Mathf.Sign(playerRB.linearVelocity.x))
            {
                OnCheatDetected("OSCILLATION", "Back-and-forth movement detected");
            }
        }
        
        // Detect reverse movement (only for significant backward movement)
        if (currentSpeed < -2.0f) // Only detect significant backward movement
        {
            OnCheatDetected("REVERSE_MOVEMENT", $"Moving backwards at {currentSpeed:F1} units/sec");
        }
        
        // Detect speed manipulation
        if (Mathf.Abs(currentSpeed) > 20f) // Unrealistically high speed
        {
            OnCheatDetected("SPEED_HACK", $"Unrealistic speed: {currentSpeed:F1} units/sec");
        }
    }
    
    /// <summary>
    /// Handle cheat detection and warnings
    /// </summary>
    private void OnCheatDetected(string cheatType, string details)
    {
        cheatAttempts++;
        
        // Rate limit warnings to avoid spam
        if (Time.time - lastCheatWarningTime > 5f)
        {
            if (showCheatWarnings)
            {
                Debug.LogWarning($"🚨 CHEAT DETECTED: {cheatType} - {details}");
                Debug.LogWarning($"Total cheat attempts: {cheatAttempts}");
            }
            lastCheatWarningTime = Time.time;
        }
        
        // Log to console for debugging
        if (showDebugInfo)
        {
            Debug.Log($"Cheat Detection: {cheatType} - {details}");
        }
    }
    
    /// <summary>
    /// Check if player is making legitimate progress
    /// </summary>
    private bool IsMakingLegitimateProgress()
    {
        if (!enableCheatPrevention) return true;
        
        return isMakingProgress && !isIdle;
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
    /// Calculate survival time bonus (only when making legitimate progress)
    /// </summary>
    private void UpdateSurvivalScore()
    {
        // Only add survival points if player is making legitimate progress
        bool legitimatelyPlaying = IsMakingLegitimateProgress() || inAir;
        
        if (legitimatelyPlaying)
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
    /// Get cheat detection statistics
    /// </summary>
    public CheatStats GetCheatStats()
    {
        return new CheatStats
        {
            cheatAttempts = cheatAttempts,
            isIdle = isIdle,
            isMakingProgress = isMakingProgress,
            timeSinceLastProgress = Time.time - lastProgressTime,
            distanceFromLastProgress = player != null ? player.position.x - lastProgressX : 0f
        };
    }
    
    /// <summary>
    /// Check if player is currently idle (for external systems)
    /// </summary>
    public bool IsPlayerIdle()
    {
        return isIdle;
    }
    
    /// <summary>
    /// Check if player is making legitimate progress (for external systems)
    /// </summary>
    public bool IsPlayerMakingProgress()
    {
        return IsMakingLegitimateProgress();
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
        
        // Reset cheat prevention
        lastProgressTime = Time.time;
        lastProgressX = startX;
        isMakingProgress = false;
        idleStartTime = Time.time;
        isIdle = false;
        cheatAttempts = 0;
        lastCheatWarningTime = 0f;
        
        if (player != null)
        {
            startX = player.position.x;
            lastRotation = player.eulerAngles.z;
        }
        
        Debug.Log("=== SCORE RESET WITH CHEAT PREVENTION ===");
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

/// <summary>
/// Data structure for cheat detection statistics
/// </summary>
[System.Serializable]
public struct CheatStats
{
    public int cheatAttempts;
    public bool isIdle;
    public bool isMakingProgress;
    public float timeSinceLastProgress;
    public float distanceFromLastProgress;
}
