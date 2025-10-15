using UnityEngine;

/// <summary>
/// Traffic cone obstacle - static obstacle with immediate collision response
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TrafficCone : ObstacleBase
{
    private bool hasHitPlayer = false;
    
    void Start()
    {
        // Ensure collider is NOT a trigger for immediate collision detection
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = false; // Changed to false for immediate collision
        }
        
        // Remove any Rigidbody2D that might exist
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            DestroyImmediate(rb);
        }
    }
    
    // Handle immediate collision with player (like car)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHitPlayer) return;
        
        if (IsPlayerCollider(collision.collider))
        {
            hasHitPlayer = true;
            
            // Check for invincible mode cheat first
            var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
            if (obstacleSpawner != null && obstacleSpawner.IsInvincibleModeEnabled())
            {
                Debug.Log("=== INVINCIBLE MODE: Traffic cone collision ignored (no damage/effects) ===");
                // Still destroy the cone but skip damage and effects
                Destroy(gameObject);
                return;
            }
            
            // Add visual feedback
            StartCoroutine(FlashEffect());
            
            // Handle collision (damage, destroy)
            HandlePlayerCollision();
        }
    }
    
    /// <summary>
    /// Override to specify traffic cone collision sound
    /// </summary>
    protected override string GetObstacleType()
    {
        return "cone";
    }
    
    /// <summary>
    /// Quick flash effect before destroying
    /// </summary>
    private System.Collections.IEnumerator FlashEffect()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }
}
