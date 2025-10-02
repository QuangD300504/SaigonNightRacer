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

    void Start()
    {
        timer = spawnInterval;
        
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
        
        // Clean up obstacles behind player
        cleanupManager.CleanupObstaclesBehindPlayer(playerTransform);
        
        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            SpawnObstacle();
            timer = spawnInterval;
            
            // Increase difficulty over time
            spawnInterval = Mathf.Max(spawnInterval * difficultyIncreaseRate, spawnIntervalMin);
        }
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
        spawnInterval = 2f;
        timer = spawnInterval;
        cleanupManager.ClearAllObstacles();
    }
}
