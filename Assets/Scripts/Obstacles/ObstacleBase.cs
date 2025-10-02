using UnityEngine;

/// <summary>
/// Base class for all obstacles that can hit the player
/// Provides common collision detection and knockback functionality
/// </summary>
public abstract class ObstacleBase : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Tooltip("Damage dealt to player on collision")]
    public int damage = 1;
    
    [Tooltip("Points awarded for avoiding this obstacle")]
    public int avoidPoints = 5;
    
    [Tooltip("Visual effect prefab to spawn on impact")]
    public GameObject impactVfxPrefab;

    /// <summary>
    /// Handle player collision - called by derived classes
    /// </summary>
    protected void HandlePlayerCollision()
    {
        // Apply damage and knockback through GameManager
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.PlayerHit();
        }
        
        // Spawn impact VFX if available
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
        }
        
        // Destroy the obstacle
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Check if collider belongs to player (direct tag or child of Player)
    /// </summary>
    protected bool IsPlayerCollider(Collider2D other)
    {
        return other.CompareTag("Player") || 
               (other.transform.parent != null && other.transform.parent.CompareTag("Player"));
    }
}
