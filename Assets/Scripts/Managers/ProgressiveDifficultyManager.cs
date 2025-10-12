using UnityEngine;

/// <summary>
/// Structure to hold obstacle spawn probabilities
/// </summary>
[System.Serializable]
public struct ObstacleProbabilities
{
    public float trafficConeChance;
    public float carChance;
    public float meteoriteChance;
}

/// <summary>
/// Centralized difficulty management system that coordinates obstacles and collectibles
/// Provides unified difficulty progression across all game systems
/// </summary>
public class ProgressiveDifficultyManager : MonoBehaviour
{
    public static ProgressiveDifficultyManager Instance { get; private set; }

    [Header("Difficulty Progression")]
    [Tooltip("Score points for each difficulty phase")]
    public float difficultyPhaseScore = 5000f;
    
    [Tooltip("Use score-based difficulty (true) or distance-based (false)")]
    public bool useScoreBasedDifficulty = true;
    
    [Tooltip("Maximum difficulty level (0-1)")]
    public float maxDifficulty = 1f;

    [Header("Obstacle Difficulty")]
    [Tooltip("Speed multiplier increase per phase")]
    public float speedMultiplierIncrease = 0.2f;
    
    [Tooltip("Maximum speed multiplier")]
    public float maxSpeedMultiplier = 2.5f;
    
    [Tooltip("Spawn rate increase per phase")]
    public float spawnRateIncrease = 0.1f;
    
    [Tooltip("Base obstacle spawn interval")]
    public float baseObstacleSpawnInterval = 3f;
    
    [Tooltip("Minimum obstacle spawn interval")]
    public float minObstacleSpawnInterval = 0.8f;
    
    [Tooltip("Obstacle spawn interval decrease rate")]
    public float obstacleSpawnIntervalDecreaseRate = 0.998f;

    [Header("Collectible Difficulty")]
    [Tooltip("Score multiplier increase per difficulty phase")]
    public float scoreMultiplierIncrease = 0.1f;
    
    [Header("Obstacle Scaling")]
    [Tooltip("Scale multiplier increase per difficulty phase")]
    public float scaleMultiplierIncrease = 0.1f;
    
    [Header("Player Boost Difficulty")]
    [Tooltip("Boost multiplier increase per difficulty phase")]
    public float boostMultiplierIncrease = 0.1f;
    
    [Tooltip("Boost duration increase per difficulty phase (smaller than power)")]
    public float boostDurationIncrease = 0.05f;
    
    [Tooltip("Boost cooldown increase per difficulty phase")]
    public float boostCooldownIncrease = 0.2f;
    
    [Header("Player Speed Difficulty")]
    [Tooltip("Player max speed increase per difficulty phase")]
    public float playerSpeedIncrease = 0.1f;

    [Header("References")]
    [Tooltip("Player transform for distance calculation")]
    public Transform playerTransform;

    // Private variables
    private float gameStartDistance;
    private int currentDifficultyPhase = 0;
    private float currentDifficulty = 0f;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // No need for DontDestroyOnLoad - this manager stays with the scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize game start distance
        if (playerTransform != null)
        {
            gameStartDistance = playerTransform.position.x;
        }
        
        Debug.Log("=== PROGRESSIVE DIFFICULTY MANAGER INITIALIZED ===");
    }

    void Update()
    {
        UpdateDifficultyProgression();
    }

    /// <summary>
    /// Update difficulty progression based on score or distance
    /// </summary>
    private void UpdateDifficultyProgression()
    {
        float progressionValue;
        int newPhase;
        
        if (useScoreBasedDifficulty)
        {
            // Score-based progression
            float currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetFinalScore() : 0f;
            progressionValue = currentScore;
            newPhase = Mathf.FloorToInt(currentScore / difficultyPhaseScore);
        }
        else
        {
            // Distance-based progression
            if (playerTransform == null) return;
            float currentDistance = playerTransform.position.x - gameStartDistance;
            progressionValue = currentDistance;
            newPhase = Mathf.FloorToInt(currentDistance / difficultyPhaseScore);
        }
        
        // Update difficulty phase
        if (newPhase > currentDifficultyPhase)
        {
            currentDifficultyPhase = newPhase;
            OnDifficultyPhaseChanged(progressionValue);
        }
        
        // Update current difficulty (0-1)
        currentDifficulty = Mathf.Clamp01(progressionValue / (difficultyPhaseScore * 5f)); // Scale over 5 phases
    }

    /// <summary>
    /// Called when difficulty phase changes
    /// </summary>
    private void OnDifficultyPhaseChanged(float progressionValue)
    {
        string progressionType = useScoreBasedDifficulty ? "Score" : "Distance";
        ObstacleProbabilities probabilities = GetObstacleProbabilities();
        
        // Get collectible weights
        var (gemWeight, diamondWeight) = GetPointsCollectibleWeights();
        var (healthWeight, shieldWeight, speedWeight) = GetBuffsCollectibleWeights();
        var (pointsInterval, buffsInterval) = GetCollectibleSpawnIntervals();
        
        // Create a single, colorful log message
        string phaseInfo = $"<color=#FFD700>=== DIFFICULTY PHASE {currentDifficultyPhase} ===</color>\n" +
                          $"<color=#00FF00>{progressionType}: {progressionValue:F0}</color> | " +
                          $"<color=#FF6B6B>Speed: {GetSpeedMultiplier():F1}x</color> | " +
                          $"<color=#4ECDC4>Spawn: {GetObstacleSpawnInterval():F1}s</color>\n" +
                          $"<color=#FFE66D>Obstacles:</color> " +
                          $"<color=#FFA500>Cone:{probabilities.trafficConeChance:P0}</color> " +
                          $"<color=#87CEEB>Car:{probabilities.carChance:P0}</color> " +
                          $"<color=#FF4500>Meteor:{probabilities.meteoriteChance:P0}</color>\n" +
                          $"<color=#DA70D6>Collectibles:</color> " +
                          $"<color=#FFD700>Score: {GetScoreMultiplier():F1}x</color> | " +
                          $"<color=#FF69B4>Buff Duration: {GetBuffDurationMultiplier():F1}x</color> | " +
                          $"<color=#FFA500>Scale: {GetObstacleScaleMultiplier():F1}x</color>\n" +
                          $"<color=#FF6B6B>Boost:</color> " +
                          $"<color=#FFD700>Power: {GetBoostMultiplier():F1}x</color> | " +
                          $"<color=#FF69B4>Duration: {GetBoostDuration():F1}s</color> | " +
                          $"<color=#FFA500>Cooldown: {GetBoostCooldown():F1}s</color>\n" +
                          $"<color=#00FF00>Player:</color> " +
                          $"<color=#87CEEB>Speed: {GetPlayerSpeedMultiplier():F1}x</color>\n" +
                          $"<color=#FFFF00>Points:</color> " +
                          $"<color=#FFD700>Gem:{gemWeight:F0}%</color> " +
                          $"<color=#00FFFF>Diamond:{diamondWeight:F0}%</color> " +
                          $"<color=#FFA500>Interval:{pointsInterval:F1}s</color>\n" +
                          $"<color=#FF69B4>Buffs:</color> " +
                          $"<color=#00FF00>Health:{healthWeight:F0}%</color> " +
                          $"<color=#0080FF>Shield:{shieldWeight:F0}%</color> " +
                          $"<color=#FF0000>Speed:{speedWeight:F0}%</color> " +
                          $"<color=#FFA500>Interval:{buffsInterval:F1}s</color>\n" +
                          $"<color=#87CEEB>{GetPhaseDescription()}</color>";
        
        Debug.Log(phaseInfo); // Enhanced phase log with detailed collectible info
    }

    // ===== PUBLIC API METHODS =====

    /// <summary>
    /// Get current difficulty level (0-1)
    /// </summary>
    public float GetDifficulty()
    {
        return currentDifficulty;
    }

    /// <summary>
    /// Get current difficulty phase (integer)
    /// </summary>
    public int GetDifficultyPhase()
    {
        return currentDifficultyPhase;
    }

    /// <summary>
    /// Get speed multiplier for obstacles
    /// </summary>
    public float GetSpeedMultiplier()
    {
        return Mathf.Min(1f + (currentDifficultyPhase * speedMultiplierIncrease), maxSpeedMultiplier);
    }

    /// <summary>
    /// Get spawn rate multiplier for obstacles
    /// </summary>
    public float GetSpawnRateMultiplier()
    {
        return 1f + (currentDifficultyPhase * spawnRateIncrease);
    }

    /// <summary>
    /// Get obstacle spawn interval based on difficulty
    /// </summary>
    public float GetObstacleSpawnInterval()
    {
        // Gradually decrease spawn interval as difficulty increases
        float currentInterval = baseObstacleSpawnInterval;
        for (int i = 0; i < currentDifficultyPhase; i++)
        {
            currentInterval *= obstacleSpawnIntervalDecreaseRate;
        }
        return Mathf.Max(currentInterval, minObstacleSpawnInterval);
    }

    /// <summary>
    /// Get obstacle probabilities based on difficulty phase
    /// </summary>
    public ObstacleProbabilities GetObstacleProbabilities()
    {
        ObstacleProbabilities probabilities = new ObstacleProbabilities();
        
        if (currentDifficultyPhase == 0)
        {
            // Easy phase - Mostly traffic cones, few cars, some meteorites
            probabilities.trafficConeChance = 0.8f;
            probabilities.carChance = 0.1f;
            probabilities.meteoriteChance = 0.1f;
        }
        else if (currentDifficultyPhase == 1)
        {
            // Medium phase - Increase meteorites, keep cars limited
            probabilities.trafficConeChance = 0.6f;
            probabilities.carChance = 0.2f;
            probabilities.meteoriteChance = 0.2f;
        }
        else if (currentDifficultyPhase == 2)
        {
            // Hard phase - High meteorite probability
            probabilities.trafficConeChance = 0.4f;
            probabilities.carChance = 0.2f;
            probabilities.meteoriteChance = 0.4f;
        }
        else
        {
            // Expert+ phase - Meteorites dominate
            probabilities.trafficConeChance = 0.3f;
            probabilities.carChance = 0.3f;
            probabilities.meteoriteChance = 0.4f;
        }
        
        return probabilities;
    }

    /// <summary>
    /// Get score multiplier for collectibles
    /// </summary>
    public float GetScoreMultiplier()
    {
        return 1f + (currentDifficultyPhase * scoreMultiplierIncrease);
    }

    /// <summary>
    /// Get obstacle scale multiplier based on difficulty phase
    /// </summary>
    public float GetObstacleScaleMultiplier()
    {
        return 1f + (currentDifficultyPhase * scaleMultiplierIncrease);
    }

    /// <summary>
    /// Get boost multiplier based on difficulty phase (higher difficulty = stronger boost)
    /// </summary>
    public float GetBoostMultiplier()
    {
        return 1f + (currentDifficultyPhase * boostMultiplierIncrease);
    }

    /// <summary>
    /// Get boost duration based on difficulty phase (higher difficulty = longer boost)
    /// </summary>
    public float GetBoostDuration()
    {
        return 1f + (currentDifficultyPhase * boostDurationIncrease);
    }

    /// <summary>
    /// Get boost cooldown based on difficulty phase (higher difficulty = longer cooldown)
    /// </summary>
    public float GetBoostCooldown()
    {
        return 2f + (currentDifficultyPhase * boostCooldownIncrease);
    }

    /// <summary>
    /// Get player max speed multiplier based on difficulty phase (higher difficulty = faster player)
    /// </summary>
    public float GetPlayerSpeedMultiplier()
    {
        return 1f + (currentDifficultyPhase * playerSpeedIncrease);
    }

    /// <summary>
    /// Get difficulty-adjusted spawn weights for points collectibles
    /// Higher difficulty = more valuable collectibles (diamonds over gems)
    /// </summary>
    public (float gemWeight, float diamondWeight) GetPointsCollectibleWeights()
    {
        // Base weights
        float gemWeight = 70f;
        float diamondWeight = 30f;
        
        // Adjust based on difficulty phase
        // Higher difficulty = more diamonds (more valuable)
        float difficultyAdjustment = currentDifficultyPhase * 0.1f; // 10% shift per phase
        gemWeight = Mathf.Max(40f, gemWeight - (difficultyAdjustment * 100f));
        diamondWeight = Mathf.Min(60f, diamondWeight + (difficultyAdjustment * 100f));
        
        return (gemWeight, diamondWeight);
    }

    /// <summary>
    /// Get difficulty-adjusted spawn weights for buff collectibles
    /// Higher difficulty = more defensive buffs (shield over speed boost)
    /// </summary>
    public (float healthWeight, float shieldWeight, float speedWeight) GetBuffsCollectibleWeights()
    {
        // Base weights
        float healthWeight = 45f;
        float shieldWeight = 35f;
        float speedWeight = 20f;
        
        // Adjust based on difficulty phase
        // Higher difficulty = more shield (defensive), less speed (offensive)
        float difficultyAdjustment = currentDifficultyPhase * 0.05f; // 5% shift per phase
        
        // Shield becomes more important in harder phases
        shieldWeight = Mathf.Min(50f, shieldWeight + (difficultyAdjustment * 100f));
        
        // Speed boost becomes less important in harder phases
        speedWeight = Mathf.Max(10f, speedWeight - (difficultyAdjustment * 50f));
        
        // Health stays relatively stable
        healthWeight = 100f - shieldWeight - speedWeight;
        
        return (healthWeight, shieldWeight, speedWeight);
    }

    /// <summary>
    /// Get difficulty-adjusted buff duration multiplier
    /// Higher difficulty = longer buffs (more helpful)
    /// </summary>
    public float GetBuffDurationMultiplier()
    {
        // Base duration multiplier
        float baseMultiplier = 1f;
        
        // Increase duration for each difficulty phase (more conservative)
        float durationIncrease = currentDifficultyPhase * 0.1f; // 10% increase per phase (was 20%)
        
        return baseMultiplier + durationIncrease;
    }

    /// <summary>
    /// Get difficulty-adjusted collectible value multiplier
    /// Higher difficulty = more valuable collectibles
    /// </summary>
    public float GetCollectibleValueMultiplier(CollectibleType collectibleType)
    {
        float baseMultiplier = GetScoreMultiplier();
        
        // Additional bonus based on collectible rarity and difficulty
        switch (collectibleType)
        {
            case CollectibleType.Apple:
                // Apples get moderate bonus
                return baseMultiplier * (1f + currentDifficultyPhase * 0.1f);
                
            case CollectibleType.Diamond:
                // Diamonds get higher bonus (they're rarer)
                return baseMultiplier * (1f + currentDifficultyPhase * 0.2f);
                
            case CollectibleType.Health:
                // Health is always valuable, moderate scaling
                return baseMultiplier * (1f + currentDifficultyPhase * 0.15f);
                
            case CollectibleType.Shield:
            case CollectibleType.SpeedBoost:
                // Buffs get higher scaling (more important in harder phases)
                return baseMultiplier * (1f + currentDifficultyPhase * 0.25f);
                
            default:
                return baseMultiplier;
        }
    }

    /// <summary>
    /// Get visual effect intensity based on difficulty and collectible rarity
    /// Higher difficulty + rarer collectibles = more intense effects
    /// </summary>
    public float GetVisualEffectIntensity(CollectibleType collectibleType)
    {
        float baseIntensity = 1f;
        
        // Increase intensity with difficulty
        float difficultyIntensity = 1f + (currentDifficultyPhase * 0.3f);
        
        // Additional intensity based on rarity
        switch (collectibleType)
        {
            case CollectibleType.Apple:
                return baseIntensity * difficultyIntensity;
                
            case CollectibleType.Diamond:
                return baseIntensity * difficultyIntensity * 1.5f; // Diamonds are sparklier
                
            case CollectibleType.Health:
                return baseIntensity * difficultyIntensity * 0.8f; // Health is less flashy
                
            case CollectibleType.Shield:
                return baseIntensity * difficultyIntensity * 1.2f; // Shield has nice glow
                
            case CollectibleType.SpeedBoost:
                return baseIntensity * difficultyIntensity * 1.3f; // Speed boost is energetic
                
            default:
                return baseIntensity * difficultyIntensity;
        }
    }

    /// <summary>
    /// Get current collectible spawn intervals (for display purposes)
    /// </summary>
    public (float pointsInterval, float buffsInterval) GetCollectibleSpawnIntervals()
    {
        // Fallback calculation based on current phase
        float basePoints = 2f;
        float baseBuffs = 4f;
        float increaseRate = 1.1f;
        
        float pointsInterval = basePoints;
        float buffsInterval = baseBuffs;
        
        for (int i = 0; i < currentDifficultyPhase; i++)
        {
            pointsInterval *= increaseRate;
            buffsInterval *= increaseRate;
        }
        
        return (Mathf.Min(pointsInterval, 8f), Mathf.Min(buffsInterval, 12f));
    }

    /// <summary>
    /// Get current progression value (score or distance)
    /// </summary>
    public float GetCurrentProgressionValue()
    {
        if (useScoreBasedDifficulty)
        {
            return ScoreManager.Instance != null ? ScoreManager.Instance.GetFinalScore() : 0f;
        }
        else
        {
            return playerTransform != null ? playerTransform.position.x - gameStartDistance : 0f;
        }
    }

    /// <summary>
    /// Get progression type (Score or Distance)
    /// </summary>
    public string GetProgressionType()
    {
        return useScoreBasedDifficulty ? "Score" : "Distance";
    }

    /// <summary>
    /// Reset difficulty manager to initial state
    /// </summary>
    public void ResetDifficulty()
    {
        currentDifficultyPhase = 0;
        currentDifficulty = 0f;
        
        if (playerTransform != null)
        {
            gameStartDistance = playerTransform.position.x;
        }
        
        Debug.Log("=== DIFFICULTY MANAGER RESET ===");
    }

    /// <summary>
    /// Get description of current difficulty phase
    /// </summary>
    private string GetPhaseDescription()
    {
        switch (currentDifficultyPhase)
        {
            case 0:
                return "EASY - Mostly traffic cones, few cars, some meteorites";
            case 1:
                return "MEDIUM - More cars and meteorites, fewer cones";
            case 2:
                return "HARD - High meteorite challenge, balanced mix";
            default:
                return $"EXPERT+ (Phase {currentDifficultyPhase}) - Meteorites dominate, maximum challenge";
        }
    }

    /// <summary>
    /// Get difficulty info for debugging
    /// </summary>
    public string GetDifficultyInfo()
    {
        ObstacleProbabilities probabilities = GetObstacleProbabilities();
        return $"Phase: {currentDifficultyPhase}, Difficulty: {currentDifficulty:F2}, " +
               $"Speed: {GetSpeedMultiplier():F2}x, Spawn: {GetObstacleSpawnInterval():F2}s, " +
               $"Obstacles: C{probabilities.trafficConeChance:P0}/Ca{probabilities.carChance:P0}/M{probabilities.meteoriteChance:P0}, " +
               $"Score: {GetScoreMultiplier():F2}x";
    }
}
