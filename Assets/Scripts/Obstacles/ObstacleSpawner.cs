using UnityEngine;

/// <summary>
/// Simplified obstacle spawner that coordinates specialized components
/// Focuses only on timing and coordination, delegates specific tasks to other components
/// </summary>
public class ObstacleSpawnerNew : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Time between spawn cycles")]
    public float spawnInterval = 3f;
    
    [Tooltip("Minimum spawn interval (fastest possible)")]
    public float spawnIntervalMin = 0.8f;
    
    [Tooltip("Rate of difficulty increase")]
    public float difficultyIncreaseRate = 0.998f;

    [Header("Difficulty Progression")]
    [Tooltip("Score points for each difficulty phase")]
    public float difficultyPhaseScore = 5000f;
    
    [Tooltip("Speed multiplier increase per phase")]
    public float speedMultiplierIncrease = 0.2f;
    
    [Tooltip("Maximum speed multiplier")]
    public float maxSpeedMultiplier = 2.5f;
    
    [Tooltip("Enable dynamic obstacle probability changes")]
    public bool enableDynamicProbabilities = true;
    
    [Tooltip("Use score-based difficulty (true), distance-based (false), or time-based (null)")]
    public bool useScoreBasedDifficulty = true;

    [Header("Cheat Settings")]
    [Tooltip("Enable invincible mode (no damage from obstacles)")]
    public bool invincibleMode = false;

    [Header("References")]
    [Tooltip("Player transform for positioning")]
    public Transform playerTransform;
    
    [Tooltip("Position calculator component")]
    public ObstaclePositionCalculator positionCalculator;
    
    [Tooltip("Obstacle selector component")]
    public ObstacleSelector obstacleSelector;
    
    [Tooltip("Cleanup manager component")]
    public ObstacleCleanupManager cleanupManager;
    
    [Tooltip("Obstacle configurator component")]
    public ObstacleConfigurator obstacleConfigurator;

    // Private variables
    private float timer;
    private bool spawning = true;
    
    // Difficulty progression variables
    private float gameStartTime;
    private float gameStartDistance;
    private int currentDifficultyPhase = 0;
    private float currentSpeedMultiplier = 1f;
    private float lastLoggedSpawnInterval = 0f;

    void Start()
    {
        timer = spawnInterval;
        gameStartTime = Time.time;
        gameStartDistance = playerTransform != null ? playerTransform.position.x : 0f;
        currentDifficultyPhase = 0;
        currentSpeedMultiplier = 1f;
        lastLoggedSpawnInterval = spawnInterval;
        
        // Log initial spawn settings
        Debug.Log($"=== OBSTACLE SPAWNER INITIALIZED ===");
        Debug.Log($"Initial Spawn Interval: {spawnInterval:F2}s");
        Debug.Log($"Initial Spawn Rate: {60f/spawnInterval:F1} obstacles/minute");
        Debug.Log($"Difficulty Progression: {(useScoreBasedDifficulty ? "Score-based" : "Distance-based")}");
        if (useScoreBasedDifficulty)
        {
            Debug.Log($"Difficulty Phase Score: {difficultyPhaseScore} points");
        }
        else
        {
            Debug.Log($"Difficulty Phase Distance: {difficultyPhaseScore} units");
        }
        Debug.Log($"Speed Multiplier Increase: {speedMultiplierIncrease:F1}x per phase");
        Debug.Log($"Max Speed Multiplier: {maxSpeedMultiplier:F1}x");
        Debug.Log("=== INITIALIZATION COMPLETE ===");
        
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
        
        // Auto-find components if not assigned
        if (positionCalculator == null)
            positionCalculator = GetComponent<ObstaclePositionCalculator>();
        if (obstacleSelector == null)
            obstacleSelector = GetComponent<ObstacleSelector>();
        if (cleanupManager == null)
            cleanupManager = GetComponent<ObstacleCleanupManager>();
        if (obstacleConfigurator == null)
            obstacleConfigurator = GetComponent<ObstacleConfigurator>();
        
        if (playerTransform == null)
        {
            Debug.LogError("ObstacleSpawnerNew: Player not found! Please assign playerTransform or tag Player object.");
        }
    }

    void Update()
    {
        if (!spawning) return;
        
        // Update difficulty progression
        UpdateDifficultyProgression();
        
        // Clean up obstacles behind player
        cleanupManager.CleanupObstaclesBehindPlayer(playerTransform);
        
        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            SpawnObstacle();
            timer = spawnInterval;
            
            // Increase difficulty over time
            float newSpawnInterval = Mathf.Max(spawnInterval * difficultyIncreaseRate, spawnIntervalMin);
            
            // Log spawn rate changes if significant
            if (Mathf.Abs(newSpawnInterval - lastLoggedSpawnInterval) > 0.1f)
            {
                Debug.Log($"--- SPAWN RATE CHANGE ---");
                Debug.Log($"Spawn Interval: {spawnInterval:F2}s → {newSpawnInterval:F2}s");
                Debug.Log($"Spawn Rate: {60f/spawnInterval:F1} → {60f/newSpawnInterval:F1} obstacles/minute");
                Debug.Log($"Change: {((60f/newSpawnInterval) - (60f/spawnInterval)):F1} obstacles/minute");
                lastLoggedSpawnInterval = newSpawnInterval;
            }
            
            spawnInterval = newSpawnInterval;
        }
    }

    /// <summary>
    /// Update difficulty progression based on score or distance
    /// </summary>
    private void UpdateDifficultyProgression()
    {
        if (playerTransform == null) return;
        
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
            float currentDistance = playerTransform.position.x - gameStartDistance;
            progressionValue = currentDistance;
            newPhase = Mathf.FloorToInt(currentDistance / difficultyPhaseScore);
        }
        
        if (newPhase > currentDifficultyPhase)
        {
            currentDifficultyPhase = newPhase;
            OnDifficultyPhaseChanged(progressionValue);
        }
        
        // Update speed multiplier
        currentSpeedMultiplier = Mathf.Min(1f + (currentDifficultyPhase * speedMultiplierIncrease), maxSpeedMultiplier);
    }
    
    /// <summary>
    /// Called when difficulty phase changes
    /// </summary>
    private void OnDifficultyPhaseChanged(float progressionValue)
    {
        string progressionType = useScoreBasedDifficulty ? "Score" : "Distance";
        string progressionUnit = useScoreBasedDifficulty ? "points" : "units";
        
        Debug.Log($"=== DIFFICULTY PHASE {currentDifficultyPhase + 1} REACHED ===");
        Debug.Log($"Progression: {progressionValue:F1} {progressionUnit} ({progressionType}-based)");
        Debug.Log($"Speed Multiplier: {currentSpeedMultiplier:F1}x");
        Debug.Log($"Current Spawn Interval: {spawnInterval:F2}s");
        Debug.Log($"Spawn Rate: {60f/spawnInterval:F1} obstacles/minute");
        
        if (enableDynamicProbabilities && obstacleSelector != null)
        {
            UpdateObstacleProbabilities();
        }
        
        Debug.Log("=== END DIFFICULTY PHASE ===");
    }
    
    /// <summary>
    /// Update obstacle probabilities based on difficulty phase
    /// </summary>
    private void UpdateObstacleProbabilities()
    {
        // Store previous probabilities for comparison
        float prevTrafficCone = obstacleSelector.trafficConeChance;
        float prevCar = obstacleSelector.carChance;
        float prevMeteorite = obstacleSelector.meteoriteChance;
        
        // Phase-based probability progression
        // Phase 0: Easy - Mostly traffic cones (your starting setup)
        // Phase 1: Medium - Introduce cars gradually
        // Phase 2: Hard - Introduce meteorites, reduce cones
        // Phase 3+: Expert - Balanced challenging mix
        
        if (currentDifficultyPhase == 0)
        {
            // Easy phase - Introduce meteorites early for more challenge
            obstacleSelector.trafficConeChance = 0.8f;  // 80% - mostly static obstacles
            obstacleSelector.carChance = 0.1f;           // 10% - very few cars
            obstacleSelector.meteoriteChance = 0.1f;    // 10% - introduce meteorites early
        }
        else if (currentDifficultyPhase == 1)
        {
            // Medium phase - Increase meteorites, keep cars limited
            obstacleSelector.trafficConeChance = 0.6f;   // 60% - fewer cones
            obstacleSelector.carChance = 0.2f;           // 20% - more cars but still limited
            obstacleSelector.meteoriteChance = 0.2f;    // 20% - more meteorites
        }
        else if (currentDifficultyPhase == 2)
        {
            // Hard phase - High meteorite probability
            obstacleSelector.trafficConeChance = 0.4f;   // 40% - fewer cones
            obstacleSelector.carChance = 0.2f;            // 20% - same car difficulty
            obstacleSelector.meteoriteChance = 0.4f;    // 40% - high meteorite challenge
        }
        else
        {
            // Expert+ phase - Meteorites dominate
            obstacleSelector.trafficConeChance = 0.3f;   // 30% - minimal cones
            obstacleSelector.carChance = 0.3f;            // 30% - challenging cars
            obstacleSelector.meteoriteChance = 0.4f;     // 40% - meteorites dominate
        }
        
        // Log probability changes
        Debug.Log($"--- OBSTACLE PROBABILITY CHANGES ---");
        Debug.Log($"Traffic Cone: {prevTrafficCone:P0} → {obstacleSelector.trafficConeChance:P0}");
        Debug.Log($"Car: {prevCar:P0} → {obstacleSelector.carChance:P0}");
        Debug.Log($"Meteorite: {prevMeteorite:P0} → {obstacleSelector.meteoriteChance:P0}");
        Debug.Log($"Total Probability: {(obstacleSelector.trafficConeChance + obstacleSelector.carChance + obstacleSelector.meteoriteChance):P0}");
        
        // Log phase description
        string phaseDescription = GetPhaseDescription();
        Debug.Log($"Phase Description: {phaseDescription}");
    }
    
    /// <summary>
    /// Get description of current difficulty phase
    /// </summary>
    private string GetPhaseDescription()
    {
        if (currentDifficultyPhase == 0)
        {
            return "EASY - Mostly traffic cones, few cars, some meteorites";
        }
        else if (currentDifficultyPhase == 1)
        {
            return "MEDIUM - More cars and meteorites, fewer cones";
        }
        else if (currentDifficultyPhase == 2)
        {
            return "HARD - High meteorite challenge, balanced mix";
        }
        else
        {
            return $"EXPERT+ - Meteorites dominate, maximum challenge";
        }
    }
    
    /// <summary>
    /// Get current speed multiplier for obstacles
    /// </summary>
    public float GetCurrentSpeedMultiplier()
    {
        return currentSpeedMultiplier;
    }
    
    /// <summary>
    /// Get current difficulty phase
    /// </summary>
    public int GetCurrentDifficultyPhase()
    {
        return currentDifficultyPhase;
    }
    

    void SpawnObstacle()
    {
        if (playerTransform == null) return;
        
        GameObject obstaclePrefab = obstacleSelector.ChooseObstacleType();
        
        if (obstaclePrefab != null)
        {
            Vector3 spawnPos = CalculateSpawnPosition(obstaclePrefab);
            Quaternion spawnRotation = CalculateSpawnRotation(obstaclePrefab, spawnPos);
            
            GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, spawnRotation);
            
            // Track obstacle for cleanup
            cleanupManager.TrackObstacle(obstacle);
            
            // Configure obstacle-specific settings
            obstacleConfigurator.ConfigureObstacle(obstacle);
        }
    }
    
    /// <summary>
    /// Calculate spawn position based on obstacle type
    /// </summary>
    private Vector3 CalculateSpawnPosition(GameObject obstaclePrefab)
    {
        float randomXOffset = Random.Range(positionCalculator.spawnMinX, positionCalculator.spawnMaxX);
        
        if (obstacleSelector.IsMeteorite(obstaclePrefab))
        {
            return positionCalculator.CalculateMeteoriteSpawnPosition(playerTransform, randomXOffset);
        }
        else
        {
            // Try to find a position that's not too close to existing obstacles
            Vector3 candidatePos;
            int attempts = 0;
            bool isCar = obstaclePrefab.GetComponent<Car>() != null;
            do
            {
                randomXOffset = Random.Range(positionCalculator.spawnMinX, positionCalculator.spawnMaxX);
                candidatePos = positionCalculator.CalculateGroundSpawnPosition(playerTransform, randomXOffset, isCar);
                attempts++;
            } while (cleanupManager.IsTooCloseToExistingObstacle(candidatePos, obstaclePrefab) && attempts < 20);
            
            return candidatePos;
        }
    }
    
    /// <summary>
    /// Calculate spawn rotation based on obstacle type
    /// </summary>
    private Quaternion CalculateSpawnRotation(GameObject obstaclePrefab, Vector3 spawnPos)
    {
        if (obstacleSelector.IsTrafficCone(obstaclePrefab))
        {
            return positionCalculator.CalculateTerrainRotation(spawnPos.x);
        }
        
        return Quaternion.identity;
    }

    /// <summary>
    /// Stop spawning obstacles
    /// </summary>
    public void StopSpawning()
    {
        spawning = false;
    }
    
    /// <summary>
    /// Start spawning obstacles
    /// </summary>
    public void StartSpawning()
    {
        spawning = true;
    }
    
    /// <summary>
    /// Reset spawner to initial state
    /// </summary>
    public void ResetSpawner()
    {
        spawnInterval = 3f;
        timer = spawnInterval;
        gameStartTime = Time.time;
        gameStartDistance = playerTransform != null ? playerTransform.position.x : 0f;
        currentDifficultyPhase = 0;
        currentSpeedMultiplier = 1f;
        cleanupManager.ClearAllObstacles();
        
        // Reset obstacle probabilities to initial values (meteorites from start)
        if (obstacleSelector != null)
        {
            obstacleSelector.trafficConeChance = 0.8f;  // 80% - mostly easy obstacles
            obstacleSelector.carChance = 0.1f;           // 10% - very few hard cars
            obstacleSelector.meteoriteChance = 0.1f;    // 10% - meteorites from start
        }
        
        Debug.Log("=== SPAWNER RESET ===");
        Debug.Log($"Reset to Phase 0, Distance: {gameStartDistance:F1} units");
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
    /// Toggle invincible mode for testing/debugging
    /// </summary>
    public void ToggleInvincibleMode()
    {
        invincibleMode = !invincibleMode;
        Debug.Log($"=== INVINCIBLE MODE {(invincibleMode ? "ENABLED" : "DISABLED")} ===");
        Debug.Log($"Player will {(invincibleMode ? "NOT" : "now")} take damage from obstacles");
    }
    
    /// <summary>
    /// Check if invincible mode is enabled
    /// </summary>
    public bool IsInvincibleModeEnabled()
    {
        return invincibleMode;
    }
}
