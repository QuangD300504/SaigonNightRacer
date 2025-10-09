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
    
    [Tooltip("Collectible prefabs to spawn")]
    public GameObject[] collectiblePrefabs;
    
    [Tooltip("Rare collectible prefabs (diamonds, hearts, power-ups)")]
    public GameObject[] rareCollectiblePrefabs;
    
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundMask = 64; // Ground layer (layer 6)

    [Header("Spawn Settings")]
    [Tooltip("Time between spawn attempts")]
    public float spawnInterval = 2f;
    
    [Tooltip("Minimum spawn interval")]
    public float minSpawnInterval = 0.7f;
    
    [Tooltip("Maximum spawn interval")]
    public float maxSpawnInterval = 3f;
    
    [Tooltip("Minimum distance ahead of player to spawn")]
    public float minSpawnDistanceAhead = 10f;
    
    [Tooltip("Maximum distance ahead of player to spawn")]
    public float maxSpawnDistanceAhead = 20f;
    
    [Tooltip("Horizontal offset range for spawn position")]
    public float horizontalOffsetRange = 3f;
    
    [Tooltip("Height above ground to start raycast")]
    public float spawnHeightOffset = 10f;
    
    [Tooltip("Distance to search for ground")]
    public float raycastDistance = 30f;

    [Header("Cleanup Settings")]
    [Tooltip("Distance behind player to clean up collectibles")]
    public float cleanupDistance = 25f;

    [Header("Spawn Patterns")]
    [Tooltip("Chance to spawn collectibles in clusters")]
    [Range(0f, 1f)]
    public float clusterSpawnChance = 0.3f;
    
    [Tooltip("Minimum collectibles in a cluster")]
    public int minClusterSize = 2;
    
    [Tooltip("Maximum collectibles in a cluster")]
    public int maxClusterSize = 4;
    
    [Tooltip("Distance between collectibles in a cluster")]
    public float clusterSpacing = 1.5f;

    // Private variables
    private float timer;
    private bool spawning = true;
    private System.Collections.Generic.List<GameObject> spawnedCollectibles = new System.Collections.Generic.List<GameObject>();

    void Start()
    {
        // Initialize timer
        timer = Random.Range(minSpawnInterval, maxSpawnInterval);
        
        // Validate setup
        ValidateSetup();
    }

    void Update()
    {
        if (!spawning || player == null || collectiblePrefabs == null || collectiblePrefabs.Length == 0) return;

        // Clean up collectibles behind player
        CleanupCollectiblesBehindPlayer();

        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            // Check if we should spawn a collectible
            bool shouldSpawn = ProgressiveDifficultyManager.Instance.ShouldSpawnCollectible();
            
            if (shouldSpawn)
            {
                // Decide whether to spawn single or cluster
                if (Random.value < clusterSpawnChance)
                {
                    SpawnCollectibleCluster();
                }
                else
                {
                    SpawnSingleCollectible();
                }
            }

            // Reset timer
            timer = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    /// <summary>
    /// Spawn a single collectible
    /// </summary>
    private void SpawnSingleCollectible()
    {
        Vector3 spawnPos = CalculateSpawnPosition();
        if (spawnPos == Vector3.zero) return;

        GameObject prefabToSpawn = ChooseCollectiblePrefab();
        if (prefabToSpawn != null)
        {
            GameObject spawnedCollectible = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            spawnedCollectibles.Add(spawnedCollectible);
        }
    }

    /// <summary>
    /// Spawn a cluster of collectibles
    /// </summary>
    private void SpawnCollectibleCluster()
    {
        Vector3 baseSpawnPos = CalculateSpawnPosition();
        if (baseSpawnPos == Vector3.zero) return;

        int clusterCount = Random.Range(minClusterSize, maxClusterSize + 1);
        
        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 clusterPos = baseSpawnPos + Vector3.right * (i * clusterSpacing);
            GameObject prefabToSpawn = ChooseCollectiblePrefab();
            if (prefabToSpawn != null)
            {
                GameObject spawnedCollectible = Instantiate(prefabToSpawn, clusterPos, Quaternion.identity);
                spawnedCollectibles.Add(spawnedCollectible);
            }
        }
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
    /// Choose which collectible prefab to spawn based on difficulty
    /// </summary>
    private GameObject ChooseCollectiblePrefab()
    {
        if (collectiblePrefabs == null || collectiblePrefabs.Length == 0)
            return null;

        if (ProgressiveDifficultyManager.Instance != null)
        {
            return ProgressiveDifficultyManager.Instance.GetCollectiblePrefab(collectiblePrefabs, rareCollectiblePrefabs);
        }

        // Fallback: random selection from common prefabs
        return collectiblePrefabs[Random.Range(0, collectiblePrefabs.Length)];
    }

    /// <summary>
    /// Calculate spawn position ahead of player
    /// </summary>
    private Vector3 CalculateSpawnPosition()
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
    /// Validate that all required components are assigned
    /// </summary>
    private void ValidateSetup()
    {
        if (player == null)
        {
            Debug.LogError("CollectibleSpawner: Player not assigned!");
        }
        
        if (collectiblePrefabs == null || collectiblePrefabs.Length == 0)
        {
            Debug.LogError("CollectibleSpawner: No collectible prefabs assigned!");
        }
    }

    // ===== PUBLIC API METHODS =====

    /// <summary>
    /// Set spawn interval
    /// </summary>
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Clamp(interval, minSpawnInterval, maxSpawnInterval);
        timer = spawnInterval;
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
        
        timer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}