using UnityEngine;

/// <summary>
/// Types of collectibles with different effects
/// </summary>
public enum CollectibleType
{
    Score,           // Basic score bonus
    HighValueScore,  // Large score bonus (diamonds, gems)
    Health,          // Restore player health
    Shield,          // Temporary invincibility
    SpeedBoost      // Temporary speed increase
}

/// <summary>
/// Modular collectible system that handles different types of items
/// Each collectible can have unique effects based on its type
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    [Header("Collectible Settings")]
    [Tooltip("Type of collectible determines its effect")]
    public CollectibleType collectibleType = CollectibleType.Score;
    
    [Tooltip("Score points awarded when collected")]
    public int scoreValue = 10;
    
    [Tooltip("Health restored (for Health type collectibles)")]
    public int healthRestore = 1;
    
    [Tooltip("Duration of power-up effects in seconds")]
    public float powerupDuration = 3f;
    
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundMask = 64; // Ground layer (layer 6)
    
    [Header("Effects")]
    [Tooltip("Sound to play when collected")]
    public AudioClip collectSound;
    
    [Tooltip("Visual effect to spawn when collected")]
    public GameObject collectEffect;
    
    [Tooltip("Volume of collect sound")]
    [Range(0f, 1f)]
    public float collectVolume = 0.7f;

    // Private variables
    private Vector3 basePosition;
    private Quaternion targetRotation;
    private bool isCollected = false;

    void Start()
    {
        // Find the ground directly below at start
        StickToGround();
        
        // Set base position
        basePosition = transform.position;
    }

    void Update()
    {
        if (isCollected) return;
        
        // No custom animations - let the sprite's own animation handle it
    }

    /// <summary>
    /// Stick the collectible to the road surface using raycast
    /// </summary>
    private void StickToGround()
    {
        // Raycast down from above to find ground
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 5f, Vector2.down, 15f, groundMask);

        if (hit.collider != null)
        {
            // Stick to road surface
            transform.position = new Vector3(hit.point.x, hit.point.y + 0.05f, transform.position.z);

            // Rotate to match slope normal
            float slopeAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
            targetRotation = Quaternion.Euler(0, 0, slopeAngle);
            transform.rotation = targetRotation;
        }
        else
        {
            Debug.LogWarning($"Collectible {gameObject.name} couldn't find ground to stick to!");
        }
    }

    /// <summary>
    /// Handle collision with player
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        // Check if it's the player or any part of the bike
        if (other.CompareTag("Player") || 
            other.name.Contains("FrontWheel") || 
            other.name.Contains("BackWheel") || 
            other.name.Contains("Frame"))
        {
            CollectItem(other.gameObject);
        }
    }

    /// <summary>
    /// Collect the item and apply its effect
    /// </summary>
    private void CollectItem(GameObject player)
    {
        if (isCollected) return;
        isCollected = true;

        // Play collection effects
        PlayCollectionEffects();

        // Apply the collectible's specific effect
        ApplyCollectibleEffect(player);

        // Start collection animation
        StartCoroutine(CollectionAnimation());
    }

    /// <summary>
    /// Play visual and audio effects when collected
    /// </summary>
    private void PlayCollectionEffects()
    {
        // Play collect sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);
        }

        // Spawn visual effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Apply the specific effect based on collectible type
    /// </summary>
    private void ApplyCollectibleEffect(GameObject player)
    {
        var scoreManager = FindFirstObjectByType<ScoreManager>();
        var gameManager = GameManager.Instance;
        var difficultyManager = ProgressiveDifficultyManager.Instance;

        switch (collectibleType)
        {
            case CollectibleType.Score:
                // Basic score bonus with difficulty multiplier
                if (scoreManager != null)
                {
                    float multiplier = difficultyManager != null ? difficultyManager.GetScoreMultiplier() : 1f;
                    int finalScore = Mathf.RoundToInt(scoreValue * multiplier);
                    scoreManager.AddScore(finalScore);
                }
                break;

            case CollectibleType.HighValueScore:
                // Large score bonus with difficulty multiplier
                if (scoreManager != null)
                {
                    float multiplier = difficultyManager != null ? difficultyManager.GetScoreMultiplier() : 1f;
                    int finalScore = Mathf.RoundToInt(scoreValue * 5f * multiplier); // 5x base + difficulty multiplier
                    scoreManager.AddScore(finalScore);
                }
                break;

            case CollectibleType.Health:
                // Restore player health
                if (gameManager != null)
                {
                    gameManager.RestoreHealth(healthRestore);
                }
                break;

            case CollectibleType.Shield:
                // Temporary invincibility
                player.SendMessage("ActivateShield", powerupDuration, SendMessageOptions.DontRequireReceiver);
                break;

            case CollectibleType.SpeedBoost:
                // Temporary speed boost
                player.SendMessage("ActivateSpeedBoost", powerupDuration, SendMessageOptions.DontRequireReceiver);
                break;
        }
    }

    /// <summary>
    /// Play collection animation before destroying
    /// </summary>
    private System.Collections.IEnumerator CollectionAnimation()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Scale down and fade out
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            
            // Optional: Add a small upward movement
            transform.position += Vector3.up * Time.deltaTime * 2f;
            
            yield return null;
        }
        
        // Destroy the collectible
        Destroy(gameObject);
    }

    /// <summary>
    /// Manually set the score value (useful for different collectible types)
    /// </summary>
    public void SetScoreValue(int value)
    {
        scoreValue = value;
    }

    /// <summary>
    /// Check if this collectible has been collected
    /// </summary>
    public bool IsCollected()
    {
        return isCollected;
    }
}
