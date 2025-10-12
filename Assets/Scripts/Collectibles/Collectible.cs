using UnityEngine;

/// <summary>
/// Categories of collectibles for better organization
/// </summary>
public enum CollectibleCategory
{
    Points,     // Score-based collectibles (coins, gems, diamonds)
    Buffs       // Power-up collectibles (health, shield, speed)
}

/// <summary>
/// Types of collectibles with different effects
/// </summary>
public enum CollectibleType
{
    // POINTS CATEGORY
    Apple,           // Medium score bonus (50 points)
    Diamond,         // High score bonus (100 points)
    
    // BUFFS CATEGORY
    Health,          // Restore player health
    Shield,          // Temporary invincibility
    SpeedBoost       // Temporary speed increase
}

/// <summary>
/// Modular collectible system that handles different types of items
/// Each collectible can have unique effects based on its type
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    [Header("Collectible Settings")]
    [Tooltip("Category of collectible (Points or Buffs)")]
    public CollectibleCategory collectibleCategory = CollectibleCategory.Points;
    
    [Tooltip("Type of collectible determines its effect")]
    public CollectibleType collectibleType = CollectibleType.Apple;
    
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
        // Play collect sound using AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollectibleSound(collectibleType);
        }
        // Fallback to individual sound if AudioManager doesn't have the specific sound
        else if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);
        }

        // Spawn visual effect with difficulty-based intensity
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            ApplyDifficultyBasedVisualEffects(effect);
        }
    }

    /// <summary>
    /// Apply difficulty-based visual effects to the spawned effect
    /// </summary>
    private void ApplyDifficultyBasedVisualEffects(GameObject effect)
    {
        var difficultyManager = ProgressiveDifficultyManager.Instance;
        if (difficultyManager == null) return;

        float intensity = difficultyManager.GetVisualEffectIntensity(collectibleType);
        
        // Scale particle systems
        var particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.startSizeMultiplier *= intensity;
            main.startSpeedMultiplier *= intensity;
            
            // Increase emission rate for more intense effects
            var emission = ps.emission;
            emission.rateOverTimeMultiplier *= intensity;
        }
        
        // Scale light intensity if present
        var light = effect.GetComponentInChildren<Light>();
        if (light != null)
        {
            light.intensity *= intensity;
            light.range *= intensity;
        }
        
        // Scale sprite renderer alpha for glow effect
        var spriteRenderer = effect.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Min(1f, color.a * intensity);
            spriteRenderer.color = color;
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

        // Find the actual player GameObject with BikeController
        GameObject actualPlayer = FindPlayerWithBikeController(player);

        switch (collectibleType)
        {
            // POINTS CATEGORY
            case CollectibleType.Apple:
                // Medium score bonus with difficulty-adjusted multiplier
                if (scoreManager != null)
                {
                    float multiplier = difficultyManager != null ? difficultyManager.GetCollectibleValueMultiplier(collectibleType) : 1f;
                    int finalScore = Mathf.RoundToInt(scoreValue * 3f * multiplier); // 3x base + difficulty multiplier
                    scoreManager.AddScore(finalScore);
                    ShowCollectibleNotification($"+{finalScore} APPLE!", Color.red);
                }
                break;

            case CollectibleType.Diamond:
                // High score bonus with difficulty-adjusted multiplier
                if (scoreManager != null)
                {
                    float multiplier = difficultyManager != null ? difficultyManager.GetCollectibleValueMultiplier(collectibleType) : 1f;
                    int finalScore = Mathf.RoundToInt(scoreValue * 5f * multiplier); // 5x base + difficulty multiplier
                    scoreManager.AddScore(finalScore);
                    ShowCollectibleNotification($"+{finalScore} DIAMOND!", Color.cyan);
                }
                break;

            // BUFFS CATEGORY
            case CollectibleType.Health:
                // Restore player health
                if (gameManager != null)
                {
                    gameManager.RestoreHealth(healthRestore);
                    ShowCollectibleNotification("+1 HEALTH!", Color.green);
                }
                break;

            case CollectibleType.Shield:
                // Temporary invincibility with difficulty-adjusted duration
                if (actualPlayer != null)
                {
                    float adjustedDuration = powerupDuration;
                    if (difficultyManager != null)
                    {
                        adjustedDuration *= difficultyManager.GetBuffDurationMultiplier();
                    }
                    actualPlayer.SendMessage("ActivateShield", adjustedDuration, SendMessageOptions.DontRequireReceiver);
                    ShowCollectibleNotification("SHIELD ACTIVATED!", Color.blue);
                }
                break;

            case CollectibleType.SpeedBoost:
                // Temporary speed boost with difficulty-adjusted duration
                if (actualPlayer != null)
                {
                    float adjustedDuration = powerupDuration;
                    if (difficultyManager != null)
                    {
                        adjustedDuration *= difficultyManager.GetBuffDurationMultiplier();
                    }
                    actualPlayer.SendMessage("ActivateSpeedBoost", adjustedDuration, SendMessageOptions.DontRequireReceiver);
                    ShowCollectibleNotification("SPEED BOOST!", Color.red);
                }
                break;
        }
    }

    /// <summary>
    /// Find the actual player GameObject that has BikeController
    /// </summary>
    private GameObject FindPlayerWithBikeController(GameObject collisionObject)
    {
        // If the collision object is the main player, use it
        if (collisionObject.CompareTag("Player"))
        {
            return collisionObject;
        }
        
        // If it's a child object (wheel, frame), find the parent with Player tag
        Transform current = collisionObject.transform;
        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                return current.gameObject;
            }
            current = current.parent;
        }
        
        // Fallback: find by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<BikeController>() != null)
        {
            return player;
        }
        
        return null;
    }

    /// <summary>
    /// Show notification for collected item
    /// </summary>
    private void ShowCollectibleNotification(string message, Color color)
    {
        // Try to find HUDController to show notification
        var hudController = FindFirstObjectByType<HUDController>();
        if (hudController != null)
        {
            hudController.ShowCollectibleNotification(message, color);
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
