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
    [Tooltip("Base collectible spawn chance (early game)")]
    [Range(0f, 1f)]
    public float baseCollectibleChance = 0.25f;
    
    [Tooltip("Minimum collectible spawn chance (late game)")]
    [Range(0f, 1f)]
    public float minCollectibleChance = 0.05f;
    
    [Tooltip("Rare collectible spawn chance multiplier")]
    [Range(0f, 1f)]
    public float rareCollectibleChance = 0.1f;
    
    [Tooltip("Score multiplier increase per difficulty phase")]
    public float scoreMultiplierIncrease = 0.1f;

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
                          $"<color=#32CD32>Common:{GetCollectibleSpawnChance():F2}</color> " +
                          $"<color=#FF69B4>Rare:{GetRareCollectibleChance():F2}</color> | " +
                          $"<color=#FFD700>Score: {GetScoreMultiplier():F1}x</color> | " +
                          $"<color=#87CEEB>{GetPhaseDescription()}</color>";
        
        Debug.Log(phaseInfo); // Enhanced phase log with collectible spawn chances
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
    /// Get collectible spawn chance based on difficulty
    /// </summary>
    public float GetCollectibleSpawnChance()
    {
        // Reduce spawn chance as difficulty rises (makes survival harder)
        return Mathf.Lerp(baseCollectibleChance, minCollectibleChance, currentDifficulty);
    }

    /// <summary>
    /// Get rare collectible spawn chance based on difficulty
    /// </summary>
    public float GetRareCollectibleChance()
    {
        // Rare collectibles become more common as difficulty rises
        return Mathf.Lerp(rareCollectibleChance * 0.5f, rareCollectibleChance, currentDifficulty);
    }

    /// <summary>
    /// Check if a collectible should spawn
    /// </summary>
    public bool ShouldSpawnCollectible()
    {
        return Random.value <= GetCollectibleSpawnChance();
    }

    /// <summary>
    /// Get score multiplier for collectibles
    /// </summary>
    public float GetScoreMultiplier()
    {
        return 1f + (currentDifficultyPhase * scoreMultiplierIncrease);
    }

    /// <summary>
    /// Get collectible prefab based on difficulty and rarity
    /// </summary>
    public GameObject GetCollectiblePrefab(GameObject[] commonPrefabs, GameObject[] rarePrefabs)
    {
        if (commonPrefabs == null || commonPrefabs.Length == 0) return null;

        // Calculate rare spawn chance based on difficulty
        float rareChance = rareCollectibleChance * currentDifficulty;
        bool spawnRare = Random.value < rareChance;

        // Try to spawn rare collectible
        if (spawnRare && rarePrefabs != null && rarePrefabs.Length > 0)
        {
            return rarePrefabs[Random.Range(0, rarePrefabs.Length)];
        }

        // Spawn common collectible
        return commonPrefabs[Random.Range(0, commonPrefabs.Length)];
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
               $"Collectibles: {GetCollectibleSpawnChance():F2}, Score: {GetScoreMultiplier():F2}x";
    }
}
