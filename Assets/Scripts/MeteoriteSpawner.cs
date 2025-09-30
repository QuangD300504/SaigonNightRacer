using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns meteorites with warning shadows
/// Handles difficulty scaling and burst spawning
/// </summary>
public class MeteoriteSpawner : MonoBehaviour
{
    [Header("Spawn Area")]
    [Tooltip("Left boundary of spawn area (relative to camera)")]
    public float spawnMinX = -8f;
    
    [Tooltip("Right boundary of spawn area (relative to camera)")]
    public float spawnMaxX = 8f;
    
    [Tooltip("Height where meteorites spawn")]
    public float spawnY = 12f;
    
    [Tooltip("Ground level for shadow positioning (relative to camera)")]
    public float groundY = -2f;

    [Header("Camera Tracking")]
    [Tooltip("Camera to track for spawn positioning")]
    public Camera targetCamera;

    [Header("Spawn Timing")]
    [Tooltip("Time between spawn cycles")]
    public float spawnInterval = 0.6f;
    
    [Tooltip("Number of meteorites per spawn cycle")]
    public int burstCount = 1;
    
    [Tooltip("Random offset between meteors in burst")]
    public float burstSpread = 0.2f;

    [Header("Warning System")]
    [Tooltip("Time between shadow warning and meteorite spawn")]
    public float warningTime = 0.7f;
    
    [Tooltip("Shadow prefab for warning")]
    public GameObject shadowPrefab;

    [Header("Difficulty Scaling")]
    [Tooltip("Minimum spawn interval (fastest possible)")]
    public float spawnIntervalMin = 0.15f;
    
    [Tooltip("Rate of difficulty increase (multiplier per cycle)")]
    public float difficultyIncreaseRate = 0.995f;

    [Header("Pools & References")]
    [Tooltip("Pool for meteorite objects")]
    public SimplePool meteorPool;

    private bool spawning = true;

    void Start()
    {
        // Auto-find camera if not assigned
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        if (targetCamera == null)
        {
            Debug.LogError("MeteoriteSpawner: No camera found! Please assign a camera.");
            return;
        }
        
        StartCoroutine(SpawnLoop());
    }

    /// <summary>
    /// Main spawning loop
    /// </summary>
    IEnumerator SpawnLoop()
    {
        while (spawning)
        {
            // Spawn burst of meteorites
            for (int i = 0; i < burstCount; i++)
            {
                // Get camera position for relative spawning
                float cameraX = targetCamera.transform.position.x;
                float cameraY = targetCamera.transform.position.y;
                Vector2 targetPos = new Vector2(cameraX + Random.Range(spawnMinX, spawnMaxX), cameraY + groundY);
                float xOffset = Random.Range(-burstSpread, burstSpread);
                StartCoroutine(SpawnOneWithWarning(targetPos, xOffset));
            }

            // Increase difficulty over time
            spawnInterval = Mathf.Max(spawnIntervalMin, spawnInterval * difficultyIncreaseRate);
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// Spawn a single meteorite with warning shadow
    /// </summary>
    IEnumerator SpawnOneWithWarning(Vector2 groundPos, float xOffset)
    {
        Vector3 finalPos = new Vector3(groundPos.x + xOffset, groundPos.y, 0f);
        
        // Spawn warning shadow
        if (shadowPrefab != null)
        {
            var shadow = Instantiate(shadowPrefab, finalPos, Quaternion.identity);
            Destroy(shadow, warningTime);
        }

        // Wait for warning period
        yield return new WaitForSeconds(warningTime);

        // Spawn meteorite (use camera-relative positioning)
        float cameraX = targetCamera.transform.position.x;
        float cameraY = targetCamera.transform.position.y;
        Vector3 spawnPos = new Vector3(finalPos.x, cameraY + spawnY, 0f);
        GameObject meteor = meteorPool.Get(spawnPos, Quaternion.identity);
        
        // Configure meteorite
        var meteorite = meteor.GetComponent<Meteorite>();
        if (meteorite != null)
        {
            // Randomize damage
            meteorite.damage = Random.Range(10, 30);
            meteorite.pool = meteorPool;
        }

        // Add slight horizontal velocity for more interesting movement
        var rb = meteor.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(Random.Range(-1f, 1f), 0f);
        }
        
        // Ensure meteor is active and ready
        if (meteor != null)
        {
            meteor.SetActive(true);
        }
    }

    /// <summary>
    /// Stop spawning meteorites
    /// </summary>
    public void Stop()
    {
        spawning = false;
    }

    /// <summary>
    /// Start spawning meteorites
    /// </summary>
    public void StartSpawning()
    {
        if (!spawning)
        {
            spawning = true;
            StartCoroutine(SpawnLoop());
        }
    }

    /// <summary>
    /// Reset difficulty to initial values
    /// </summary>
    public void ResetDifficulty()
    {
        spawnInterval = 0.6f;
    }
}
