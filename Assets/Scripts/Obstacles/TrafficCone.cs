using UnityEngine;

/// <summary>
/// Traffic cone obstacle - completely static obstacle
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TrafficCone : ObstacleBase
{
    
    void Start()
    {
        // Ensure collider is set as trigger for player detection
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        
        // Remove any Rigidbody2D that might exist
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            DestroyImmediate(rb);
        }
    }
    
    // Handle player detection
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            // Add visual effect before destroying
            StartCoroutine(DestroyWithEffect());
        }
    }
    
    /// <summary>
    /// Destroy the cone with a visual effect
    /// </summary>
    private System.Collections.IEnumerator DestroyWithEffect()
    {
        // Disable collider to prevent multiple hits
        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        
        // Add some visual feedback
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Flash effect
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
        
        // Handle collision (damage, knockback, destroy)
        HandlePlayerCollision();
    }
}
