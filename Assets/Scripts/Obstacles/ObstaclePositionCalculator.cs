using UnityEngine;

/// <summary>
/// Handles obstacle positioning logic - terrain height detection and slope calculation
/// Separated from ObstacleSpawner for better organization
/// </summary>
public class ObstaclePositionCalculator : MonoBehaviour
{
    [Header("Position Settings")]
    [Tooltip("Y position where ground obstacles spawn (fallback)")]
    public float groundSpawnY = -2f;
    
    [Tooltip("Height above player for meteorite spawning")]
    public float meteoriteSpawnHeight = 8f;
    
    [Tooltip("Offset above terrain for ground obstacles")]
    public float terrainOffset = 0.5f;
    
    [Tooltip("Distance ahead of player to spawn ground obstacles")]
    public float spawnDistanceAhead = 15f;
    
    [Tooltip("Left boundary of spawn area (relative to spawn point)")]
    public float spawnMinX = -10f;
    
    [Tooltip("Right boundary of spawn area (relative to spawn point)")]
    public float spawnMaxX = 10f;

    /// <summary>
    /// Calculate spawn position for ground obstacles (on terrain)
    /// </summary>
    public Vector3 CalculateGroundSpawnPosition(Transform playerTransform, float randomXOffset = 0f, bool isCar = false)
    {
        float playerX = playerTransform.position.x;
        float aheadX = playerX + spawnDistanceAhead;
        float obstacleX = aheadX + randomXOffset;
        
        float terrainHeight = GetTerrainHeightAtX(obstacleX);
        float spawnY;
        
        if (isCar)
        {
            // Cars spawn higher to fall down
            spawnY = terrainHeight > -100f ? terrainHeight + 5f : groundSpawnY + 5f;
        }
        else
        {
            // Other obstacles spawn on terrain
            spawnY = terrainHeight > -100f ? terrainHeight + terrainOffset : groundSpawnY;
        }
        
        return new Vector3(obstacleX, spawnY, 0f);
    }
    
    /// <summary>
    /// Calculate spawn position for meteorites (above player)
    /// </summary>
    public Vector3 CalculateMeteoriteSpawnPosition(Transform playerTransform, float randomXOffset = 0f)
    {
        float playerX = playerTransform.position.x;
        float playerY = playerTransform.position.y;
        float meteoriteX = playerX + randomXOffset;
        float meteoriteY = playerY + meteoriteSpawnHeight;
        
        return new Vector3(meteoriteX, meteoriteY, 0f);
    }
    
    /// <summary>
    /// Calculate rotation for obstacles to match terrain slope
    /// </summary>
    public Quaternion CalculateTerrainRotation(float xPosition)
    {
        float terrainSlope = GetTerrainSlopeAtX(xPosition);
        return Quaternion.Euler(0, 0, terrainSlope);
    }
    
    /// <summary>
    /// Get terrain height at specific X position using raycast
    /// </summary>
    private float GetTerrainHeightAtX(float x)
    {
        // Cast ray from above to find terrain surface
        Vector2 rayStart = new Vector2(x, 10f);
        Vector2 rayDirection = Vector2.down;
        float rayDistance = 20f;
        
        RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDirection, rayDistance);
        
        if (hit.collider != null)
        {
            return hit.point.y;
        }
        
        // Fallback: try multiple attempts with slight X variations
        for (int i = 0; i < 5; i++)
        {
            float offsetX = x + Random.Range(-1f, 1f);
            rayStart = new Vector2(offsetX, 10f);
            hit = Physics2D.Raycast(rayStart, rayDirection, rayDistance);
            
            if (hit.collider != null)
            {
                return hit.point.y;
            }
        }
        
        return groundSpawnY; // Ultimate fallback
    }
    
    /// <summary>
    /// Calculate terrain slope angle at specific X position
    /// </summary>
    private float GetTerrainSlopeAtX(float x)
    {
        float sampleDistance = 0.5f;
        float height1 = GetTerrainHeightAtX(x - sampleDistance);
        float height2 = GetTerrainHeightAtX(x + sampleDistance);
        
        float slope = Mathf.Atan2(height2 - height1, sampleDistance * 2f) * Mathf.Rad2Deg;
        return Mathf.Clamp(slope, -30f, 30f); // Limit slope to reasonable range
    }
}
