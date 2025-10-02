using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles cleanup of obstacles behind player and clumping prevention
/// Separated from ObstacleSpawner for better organization
/// </summary>
public class ObstacleCleanupManager : MonoBehaviour
{
    [Header("Cleanup Settings")]
    [Tooltip("Distance behind player to clean up obstacles")]
    public float cleanupDistance = 20f;
    
    [Tooltip("Minimum distance between obstacles to prevent clumping")]
    public float minObstacleDistance = 12f;

    private List<GameObject> spawnedObstacles = new List<GameObject>();

    /// <summary>
    /// Add obstacle to tracking list
    /// </summary>
    public void TrackObstacle(GameObject obstacle)
    {
        if (obstacle != null)
        {
            spawnedObstacles.Add(obstacle);
        }
    }
    
    /// <summary>
    /// Remove obstacle from tracking list
    /// </summary>
    public void UntrackObstacle(GameObject obstacle)
    {
        spawnedObstacles.Remove(obstacle);
    }
    
    /// <summary>
    /// Clean up obstacles that are behind the player
    /// </summary>
    public void CleanupObstaclesBehindPlayer(Transform playerTransform)
    {
        if (playerTransform == null) return;
        
        float playerX = playerTransform.position.x;
        
        // Clean up null references first
        spawnedObstacles.RemoveAll(obs => obs == null);
        
        // Remove obstacles that are too far behind
        for (int i = spawnedObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obstacle = spawnedObstacles[i];
            if (obstacle != null)
            {
                float obstacleX = obstacle.transform.position.x;
                if (obstacleX < playerX - cleanupDistance)
                {
                    spawnedObstacles.RemoveAt(i);
                    Destroy(obstacle);
                }
            }
        }
    }
    
    /// <summary>
    /// Check if position is too close to existing obstacles
    /// </summary>
    public bool IsTooCloseToExistingObstacle(Vector3 position, GameObject newObstaclePrefab = null)
    {
        // Clean up null references first
        spawnedObstacles.RemoveAll(obs => obs == null);
        
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
            {
                float distance = Vector3.Distance(position, obstacle.transform.position);
                
                // Use different distance requirements based on obstacle types
                float requiredDistance = GetRequiredDistance(newObstaclePrefab, obstacle);
                
                if (distance < requiredDistance)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get required distance between two obstacle types
    /// </summary>
    private float GetRequiredDistance(GameObject newObstacle, GameObject existingObstacle)
    {
        // Cars need more space because they fall down and move
        bool newIsCar = newObstacle != null && newObstacle.GetComponent<Car>() != null;
        bool existingIsCar = existingObstacle.GetComponent<Car>() != null;
        
        if (newIsCar || existingIsCar)
        {
            return minObstacleDistance * 1.5f; // Cars need 50% more space
        }
        
        return minObstacleDistance; // Default distance for static obstacles
    }
    
    /// <summary>
    /// Get count of currently tracked obstacles
    /// </summary>
    public int GetObstacleCount()
    {
        // Clean up null references first
        spawnedObstacles.RemoveAll(obs => obs == null);
        return spawnedObstacles.Count;
    }
    
    /// <summary>
    /// Clear all tracked obstacles (for game reset)
    /// </summary>
    public void ClearAllObstacles()
    {
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        spawnedObstacles.Clear();
    }
}
