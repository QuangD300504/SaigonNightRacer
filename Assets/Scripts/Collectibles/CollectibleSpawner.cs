using UnityEngine;

/// <summary>
/// Spawns collectibles continuously ahead of the player
/// Integrates with ProgressiveDifficultyManager for unified difficulty progression
/// </summary>
public class CollectibleSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player transform for positioning")]
    public Transform player;
    
    [Header("Points Collectibles")]
    [Tooltip("Point collectible prefabs (coins, gems, diamonds)")]
    public GameObject[] pointsCollectiblePrefabs;
    
    [Header("Buffs Collectibles")]
    [Tooltip("Buff collectible prefabs (health, shield, speed boost)")]
    public GameObject[] buffsCollectiblePrefabs;
    
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundMask = 64; // Ground layer (layer 6)

    [Header("Spawn Settings")]
    [Tooltip("Base time between Points spawn attempts (early game - fast)")]
    public float basePointsSpawnInterval = 2f;
    
    [Tooltip("Base time between Buffs spawn attempts (early game - fast)")]
    public float baseBuffsSpawnInterval = 4f;
    
    [Tooltip("Maximum spawn intervals (harder difficulty - slower)")]
    public float maxPointsSpawnInterval = 8f;
    public float maxBuffsSpawnInterval = 12f;
    
    [Tooltip("Spawn interval increase rate per difficulty phase")]
    [Range(1f, 1.2f)]
    public float spawnIntervalIncreaseRate = 1.1f;
    
    [Header("Spawn Lane Settings")]
    [Tooltip("Horizontal offset range for spawn position (both Points and Buffs)")]
    public float horizontalOffsetRange = 3f;
    
    [Header("Points Spawn Weights (must total 100%)")]
    [Tooltip("Gem spawn chance (%)")]
    [Range(0f, 100f)]
    public float gemSpawnWeight = 70f;
    
    [Tooltip("Diamond spawn chance (%)")]
    [Range(0f, 100f)]
    public float diamondSpawnWeight = 30f;
    
    [Header("Buffs Spawn Weights (must total 100%)")]
    [Tooltip("Health spawn chance (%)")]
    [Range(0f, 100f)]
    public float healthSpawnWeight = 45f;
    
    [Tooltip("Speed Boost spawn chance (%)")]
    [Range(0f, 100f)]
    public float speedBoostSpawnWeight = 30f;
    
    [Tooltip("Shield spawn chance (%)")]
    [Range(0f, 100f)]
    public float shieldSpawnWeight = 25f;
    
    [Tooltip("Minimum distance ahead of player to spawn")]
    public float minSpawnDistanceAhead = 10f;
    
    [Tooltip("Maximum distance ahead of player to spawn")]
    public float maxSpawnDistanceAhead = 20f;
    
    [Tooltip("Height above ground to start raycast")]
    public float spawnHeightOffset = 10f;
    
    [Tooltip("Distance to search for ground")]
    public float raycastDistance = 30f;

    [Header("Cleanup Settings")]
    [Tooltip("Distance behind player to clean up collectibles")]
    public float cleanupDistance = 25f;

    // Private variables
    private float pointsTimer;
    private float buffsTimer;
    private bool spawning = true;
    private System.Collections.Generic.List<GameObject> spawnedCollectibles = new System.Collections.Generic.List<GameObject>();
    
    // Current dynamic intervals (updated based on difficulty)
    private float currentPointsSpawnInterval;
    private float currentBuffsSpawnInterval;

    void Start()
    {
        // Initialize dynamic intervals
        UpdateSpawnIntervals();
        
        // Initialize separate timers
        pointsTimer = currentPointsSpawnInterval;
        buffsTimer = currentBuffsSpawnInterval;
        
        // Validate setup
        ValidateSetup();
    }

    void Update()
    {
        if (!spawning || player == null) return;

        // Update spawn intervals based on current difficulty
        UpdateSpawnIntervals();

        // Clean up collectibles behind player
        CleanupCollectiblesBehindPlayer();

        // Update separate timers
        pointsTimer -= Time.deltaTime;
        buffsTimer -= Time.deltaTime;
        
        // Spawn Points collectibles (frequent)
        if (pointsTimer <= 0f)
        {
            SpawnCollectibleInLane(CollectibleCategory.Points);
            pointsTimer = currentPointsSpawnInterval;
        }
        
        // Spawn Buffs collectibles (less frequent)
        if (buffsTimer <= 0f)
        {
            SpawnCollectibleInLane(CollectibleCategory.Buffs);
            buffsTimer = currentBuffsSpawnInterval;
        }
    }

    /// <summary>
    /// Spawn a collectible in the specified lane
    /// </summary>
    private void SpawnCollectibleInLane(CollectibleCategory category)
    {
        Vector3 spawnPos = CalculateSpawnPositionForLane(category);
        if (spawnPos == Vector3.zero) return;

        GameObject prefabToSpawn = ChooseCollectiblePrefabForCategory(category);
        if (prefabToSpawn != null)
        {
            GameObject spawnedCollectible = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            spawnedCollectibles.Add(spawnedCollectible);
        }
    }

    /// <summary>
    /// Calculate spawn position (both Points and Buffs spawn on right side)
    /// </summary>
    private Vector3 CalculateSpawnPositionForLane(CollectibleCategory category)
    {
        // Calculate random position ahead of player
        float randomX = player.position.x + Random.Range(minSpawnDistanceAhead, maxSpawnDistanceAhead);
        float randomY = player.position.y + spawnHeightOffset;
        float randomZ = player.position.z + Random.Range(-horizontalOffsetRange, horizontalOffsetRange);

        Vector2 rayStart = new Vector2(randomX, randomY);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, raycastDistance, groundMask);

        if (hit.collider != null)
        {
            return new Vector3(hit.point.x, hit.point.y + 0.1f, randomZ);
        }

        return Vector3.zero; // No valid ground found
    }

    /// <summary>
    /// Choose collectible prefab based on category with weighted distribution
    /// </summary>
    private GameObject ChooseCollectiblePrefabForCategory(CollectibleCategory category)
    {
        GameObject[] prefabs = null;
        
        switch (category)
        {
            case CollectibleCategory.Points:
                prefabs = pointsCollectiblePrefabs;
                if (ProgressiveDifficultyManager.Instance != null)
                {
                    var (gemWeight, diamondWeight) = ProgressiveDifficultyManager.Instance.GetPointsCollectibleWeights();
                    return ChooseWeightedPrefab(prefabs, gemWeight, diamondWeight);
                }
                return ChooseWeightedPrefab(prefabs, gemSpawnWeight, diamondSpawnWeight);
                
            case CollectibleCategory.Buffs:
                prefabs = buffsCollectiblePrefabs;
                if (ProgressiveDifficultyManager.Instance != null)
                {
                    var (healthWeight, shieldWeight, speedWeight) = ProgressiveDifficultyManager.Instance.GetBuffsCollectibleWeights();
                    return ChooseWeightedPrefab(prefabs, healthWeight, speedWeight, shieldWeight);
                }
                return ChooseWeightedPrefab(prefabs, healthSpawnWeight, speedBoostSpawnWeight, shieldSpawnWeight);
        }
        
        return null;
    }

    /// <summary>
    /// Choose prefab based on weighted distribution (2 items for Points, 3 items for Buffs)
    /// </summary>
    private GameObject ChooseWeightedPrefab(GameObject[] prefabs, float weight1, float weight2, float weight3 = 0f)
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        
        // For Points (2 items)
        if (prefabs.Length == 2)
        {
            float totalWeight = weight1 + weight2;
            float randomValue = Random.Range(0f, totalWeight);
            
            if (randomValue < weight1)
                return prefabs[0]; // First item (Gem)
            else
                return prefabs[1]; // Second item (Diamond)
        }
        
        // For Buffs (3 items)
        if (prefabs.Length >= 3)
        {
            float totalWeight = weight1 + weight2 + weight3;
            float randomValue = Random.Range(0f, totalWeight);
            
            if (randomValue < weight1)
                return prefabs[0]; // First item
            else if (randomValue < weight1 + weight2)
                return prefabs[1]; // Second item
            else
                return prefabs[2]; // Third item
        }
        
        return null;
    }

    /// <summary>
    /// Clean up collectibles that are behind the player
    /// </summary>
    private void CleanupCollectiblesBehindPlayer()
    {
        if (player == null) return;
        
        float playerX = player.position.x;
        
        // Clean up null references first
        spawnedCollectibles.RemoveAll(collectible => collectible == null);
        
        // Remove collectibles that are too far behind
        for (int i = spawnedCollectibles.Count - 1; i >= 0; i--)
        {
            GameObject collectible = spawnedCollectibles[i];
            if (collectible != null)
            {
                float collectibleX = collectible.transform.position.x;
                if (collectibleX < playerX - cleanupDistance)
                {
                    spawnedCollectibles.RemoveAt(i);
                    Destroy(collectible);
                }
            }
        }
    }

    /// <summary>
    /// Validate that all required components are assigned
    /// </summary>
    private void ValidateSetup()
    {
        if (player == null)
        {
            Debug.LogError("CollectibleSpawner: Player not assigned!");
        }
        
        if (pointsCollectiblePrefabs == null || pointsCollectiblePrefabs.Length < 2)
        {
            Debug.LogWarning("CollectibleSpawner: Need exactly 2 points collectible prefabs (Gem, Diamond)!");
        }
        
        if (buffsCollectiblePrefabs == null || buffsCollectiblePrefabs.Length < 3)
        {
            Debug.LogWarning("CollectibleSpawner: Need exactly 3 buffs collectible prefabs (Health, Speed, Shield)!");
        }
        
        // Validate weight totals
        float pointsTotal = gemSpawnWeight + diamondSpawnWeight;
        float buffsTotal = healthSpawnWeight + speedBoostSpawnWeight + shieldSpawnWeight;
        
        if (Mathf.Abs(pointsTotal - 100f) > 0.1f)
        {
            Debug.LogWarning($"CollectibleSpawner: Points weights total {pointsTotal}%, should be 100%!");
        }
        
        if (Mathf.Abs(buffsTotal - 100f) > 0.1f)
        {
            Debug.LogWarning($"CollectibleSpawner: Buffs weights total {buffsTotal}%, should be 100%!");
        }
    }

    /// <summary>
    /// Update spawn intervals based on current difficulty
    /// </summary>
    private void UpdateSpawnIntervals()
    {
        if (ProgressiveDifficultyManager.Instance == null) return;
        
        int difficultyPhase = ProgressiveDifficultyManager.Instance.GetDifficultyPhase();
        
        // Calculate dynamic intervals based on difficulty phase
        float pointsInterval = basePointsSpawnInterval;
        float buffsInterval = baseBuffsSpawnInterval;
        
        // Increase intervals for each difficulty phase (harder = slower)
        for (int i = 0; i < difficultyPhase; i++)
        {
            pointsInterval *= spawnIntervalIncreaseRate;
            buffsInterval *= spawnIntervalIncreaseRate;
        }
        
        // Apply maximum limits
        currentPointsSpawnInterval = Mathf.Min(pointsInterval, maxPointsSpawnInterval);
        currentBuffsSpawnInterval = Mathf.Min(buffsInterval, maxBuffsSpawnInterval);
    }

    // ===== PUBLIC API METHODS =====

    /// <summary>
    /// Set base spawn intervals for both Points and Buffs
    /// </summary>
    public void SetSpawnIntervals(float pointsInterval, float buffsInterval)
    {
        basePointsSpawnInterval = Mathf.Max(1f, pointsInterval);
        baseBuffsSpawnInterval = Mathf.Max(1f, buffsInterval);
        
        // Update current intervals
        UpdateSpawnIntervals();
        
        // Reset timers
        pointsTimer = currentPointsSpawnInterval;
        buffsTimer = currentBuffsSpawnInterval;
    }

    /// <summary>
    /// Get current difficulty level
    /// </summary>
    public float GetCurrentDifficulty()
    {
        if (ProgressiveDifficultyManager.Instance != null)
        {
            return ProgressiveDifficultyManager.Instance.GetDifficulty();
        }
        return 0f;
    }

    /// <summary>
    /// Check if spawner is currently spawning
    /// </summary>
    public bool IsSpawning()
    {
        return spawning;
    }

    /// <summary>
    /// Start spawning collectibles
    /// </summary>
    public void StartSpawning()
    {
        spawning = true;
    }

    /// <summary>
    /// Stop spawning collectibles
    /// </summary>
    public void StopSpawning()
    {
        spawning = false;
    }

    /// <summary>
    /// Reset spawner to initial state
    /// </summary>
    public void ResetSpawner()
    {
        ProgressiveDifficultyManager.Instance.ResetDifficulty();
        
        // Clear all tracked collectibles
        foreach (GameObject collectible in spawnedCollectibles)
        {
            if (collectible != null)
            {
                Destroy(collectible);
            }
        }
        spawnedCollectibles.Clear();
        
        // Reset intervals and timers
        UpdateSpawnIntervals();
        pointsTimer = currentPointsSpawnInterval;
        buffsTimer = currentBuffsSpawnInterval;
    }
}