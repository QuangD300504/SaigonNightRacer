using UnityEngine;

/// <summary>
/// Obstacle spawner that integrates with ProgressiveDifficultyManager
/// Automatically uses centralized difficulty system when available
/// </summary>
public class ObstacleSpawnerNew : MonoBehaviour
{
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

    [Header("Settings")]
    [Tooltip("Minimum spawn interval")]
    public float minSpawnInterval = 0.8f;

    [Header("Cheat Settings")]
    [Tooltip("Enable invincible mode (no damage from obstacles)")]
    public bool invincibleMode = false;

    // Private variables
    private float timer;
    private bool spawning = true;

    void Start()
    {
        // Initialize timer from ProgressiveDifficultyManager
        timer = ProgressiveDifficultyManager.Instance.GetObstacleSpawnInterval();
        
        // Auto-find components if not assigned
        AutoFindComponents();
        
        // Validate setup
        ValidateSetup();
    }

    void Update()
    {
        if (!spawning) return;
        
        // Update obstacle probabilities from centralized difficulty manager
        UpdateObstacleProbabilities();
        
        // Clean up obstacles behind player
        if (cleanupManager != null && playerTransform != null)
        {
            cleanupManager.CleanupObstaclesBehindPlayer(playerTransform);
        }
        
        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            SpawnObstacle();
            
            // Set next spawn interval from ProgressiveDifficultyManager
            timer = ProgressiveDifficultyManager.Instance.GetObstacleSpawnInterval();
        }
    }

    /// <summary>
    /// Update obstacle probabilities from centralized difficulty manager
    /// </summary>
    private void UpdateObstacleProbabilities()
    {
        if (ProgressiveDifficultyManager.Instance == null || obstacleSelector == null) return;
        
        ObstacleProbabilities probabilities = ProgressiveDifficultyManager.Instance.GetObstacleProbabilities();
        
        obstacleSelector.trafficConeChance = probabilities.trafficConeChance;
        obstacleSelector.carChance = probabilities.carChance;
        obstacleSelector.meteoriteChance = probabilities.meteoriteChance;
    }

    /// <summary>
    /// Spawn a single obstacle
    /// </summary>
    void SpawnObstacle()
    {
        if (playerTransform == null || obstacleSelector == null || positionCalculator == null) return;

        // Choose obstacle type
        GameObject obstaclePrefab = obstacleSelector.ChooseObstacleType();
        if (obstaclePrefab == null) return;

        // Calculate spawn position based on obstacle type
        Vector3 spawnPosition;
        if (obstacleSelector.IsMeteorite(obstaclePrefab))
        {
            spawnPosition = positionCalculator.CalculateMeteoriteSpawnPosition(playerTransform, Random.Range(-5f, 5f));
        }
        else
        {
            bool isCar = obstaclePrefab.GetComponent<Car>() != null;
            spawnPosition = positionCalculator.CalculateGroundSpawnPosition(playerTransform, Random.Range(-3f, 3f), isCar);
        }

        // Calculate spawn rotation
        Quaternion spawnRotation = CalculateSpawnRotation(obstaclePrefab, spawnPosition);

        // Spawn the obstacle
        GameObject spawnedObstacle = Instantiate(obstaclePrefab, spawnPosition, spawnRotation);

        // Track obstacle for cleanup
        if (cleanupManager != null)
        {
            cleanupManager.TrackObstacle(spawnedObstacle);
        }

        // Configure the obstacle
        if (obstacleConfigurator != null)
        {
            obstacleConfigurator.ConfigureObstacle(spawnedObstacle);
        }
    }

    /// <summary>
    /// Calculate spawn rotation based on obstacle type and position
    /// </summary>
    private Quaternion CalculateSpawnRotation(GameObject obstaclePrefab, Vector3 spawnPosition)
    {
        if (obstacleSelector.IsMeteorite(obstaclePrefab))
        {
            // Meteorites fall from above
            return Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
        }
        else if (obstacleSelector.IsTrafficCone(obstaclePrefab))
        {
            // Traffic cones are upright
            return Quaternion.identity;
        }
        else
        {
            // Cars and other obstacles
            return Quaternion.Euler(0, 0, Random.Range(-5f, 5f));
        }
    }

    /// <summary>
    /// Auto-find required components
    /// </summary>
    private void AutoFindComponents()
    {
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
        
        if (positionCalculator == null)
            positionCalculator = GetComponent<ObstaclePositionCalculator>();
        if (obstacleSelector == null)
            obstacleSelector = GetComponent<ObstacleSelector>();
        if (cleanupManager == null)
            cleanupManager = GetComponent<ObstacleCleanupManager>();
        if (obstacleConfigurator == null)
            obstacleConfigurator = GetComponent<ObstacleConfigurator>();
    }

    /// <summary>
    /// Validate that all required components are assigned
    /// </summary>
    private void ValidateSetup()
    {
        if (playerTransform == null)
        {
            Debug.LogError("ObstacleSpawnerNew: Player not found! Please assign playerTransform or tag Player object.");
        }
    }

    // ===== PUBLIC API METHODS =====

    /// <summary>
    /// Get current speed multiplier for obstacles
    /// </summary>
    public float GetCurrentSpeedMultiplier()
    {
        if (ProgressiveDifficultyManager.Instance != null)
        {
            return ProgressiveDifficultyManager.Instance.GetSpeedMultiplier();
        }
        return 1f; // Default fallback
    }

    /// <summary>
    /// Get current difficulty phase
    /// </summary>
    public int GetCurrentDifficultyPhase()
    {
        if (ProgressiveDifficultyManager.Instance != null)
        {
            return ProgressiveDifficultyManager.Instance.GetDifficultyPhase();
        }
        return 0; // Default fallback
    }

    /// <summary>
    /// Get current progression value (score or distance)
    /// </summary>
    public float GetCurrentProgressionValue()
    {
        if (ProgressiveDifficultyManager.Instance != null)
        {
            return ProgressiveDifficultyManager.Instance.GetCurrentProgressionValue();
        }
        return 0f; // Default fallback
    }

    /// <summary>
    /// Get progression type (Score or Distance)
    /// </summary>
    public string GetProgressionType()
    {
        if (ProgressiveDifficultyManager.Instance != null)
        {
            return ProgressiveDifficultyManager.Instance.GetProgressionType();
        }
        return "Score"; // Default fallback
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
        ProgressiveDifficultyManager.Instance.ResetDifficulty();
        timer = ProgressiveDifficultyManager.Instance.GetObstacleSpawnInterval();
        
        if (cleanupManager != null)
        {
            cleanupManager.ClearAllObstacles();
        }
    }

    /// <summary>
    /// Check if invincible mode is enabled
    /// </summary>
    public bool IsInvincibleModeEnabled()
    {
        return invincibleMode;
    }

    /// <summary>
    /// Toggle invincible mode for testing/debugging
    /// </summary>
    public void ToggleInvincibleMode()
    {
        invincibleMode = !invincibleMode;
    }
}