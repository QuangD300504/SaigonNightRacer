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
        // Check for invincible mode cheat first
        var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
        if (obstacleSpawner != null && obstacleSpawner.IsInvincibleModeEnabled())
        {
            Debug.Log("=== INVINCIBLE MODE: Player collision ignored (no damage/effects) ===");
            // Still destroy the obstacle but skip damage and effects
            Destroy(gameObject);
            return;
        }
        
        // Play collision sound based on obstacle type
        PlayCollisionSound();
        
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
    /// Play collision sound based on obstacle type
    /// </summary>
    protected virtual void PlayCollisionSound()
    {
        if (AudioManager.Instance != null)
        {
            // Get obstacle type from class name
            string obstacleType = GetObstacleType();
            AudioManager.Instance.PlayCollisionSound(obstacleType);
        }
    }
    
    /// <summary>
    /// Get obstacle type string for sound selection
    /// Override in derived classes for specific sounds
    /// </summary>
    protected virtual string GetObstacleType()
    {
        return gameObject.name.ToLower();
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
