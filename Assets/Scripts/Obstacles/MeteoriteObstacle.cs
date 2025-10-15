using UnityEngine;

/// <summary>
/// Smart meteorite obstacle with dynamic targeting and visual effects
/// Tracks player movement and adjusts trajectory for challenging gameplay
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MeteoriteObstacle : ObstacleBase
{
    [Header("Meteorite Settings")]
    [Tooltip("Time before meteor is destroyed if stuck")]
    public float destroyDelay = 8f;
    
    [Tooltip("Fall speed")]
    public float fallSpeed = 5f;
    
    [Header("Smart Targeting")]
    [Tooltip("Enable smart targeting (aims ahead of player)")]
    public bool enableSmartTargeting = true;
    
    [Tooltip("How far ahead to aim (in seconds)")]
    public float predictionTime = 0.5f;
    
    [Tooltip("Maximum horizontal tracking speed")]
    public float maxTrackingSpeed = 1f;
    
    [Tooltip("Tracking accuracy (0 = perfect, 1 = random)")]
    [Range(0f, 1f)]
    public float trackingAccuracy = 0.2f;
    
    [Header("Visual Effects")]
    [Tooltip("Enable dynamic rotation based on movement direction")]
    public bool enableDynamicRotation = true;
    
    [Tooltip("Enable shadow projection")]
    public bool enableShadow = true;
    
    [Tooltip("Shadow prefab to instantiate")]
    public GameObject shadowPrefab;
    
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundMask = 64; // Ground layer (layer 6)

    private Rigidbody2D rb;
    private bool hasImpacted = false;
    private float spawnTime;
    
    // Smart targeting variables
    private Transform playerTransform;
    private Vector2 targetPosition;
    private Vector2 currentVelocity;
    
    // Visual effect variables
    private GameObject shadowInstance;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        hasImpacted = false;
        spawnTime = Time.time;
        
        // Apply difficulty-based scaling
        ApplyDifficultyScaling();
        
        // Reset physics state
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        // Find player for smart targeting
        if (enableSmartTargeting)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                CalculateSmartTarget();
            }
        }
        
        // Create shadow if enabled
        if (enableShadow && shadowPrefab != null)
        {
            CreateShadow();
        }
        
        // Set up auto-destroy timer
        CancelInvoke(nameof(ForceDestroy));
        Invoke(nameof(ForceDestroy), destroyDelay);
    }

    void Update()
    {
        if (!hasImpacted)
        {
            if (enableSmartTargeting && playerTransform != null)
            {
                // Calculate target once at spawn, then fall straight down
                if (targetPosition == Vector2.zero)
                {
                    CalculateSmartTarget();
                }
                
                // Simple straight-line fall with minimal horizontal adjustment
                Vector2 currentPos = transform.position;
                float horizontalDistance = targetPosition.x - currentPos.x;
                
                // Very subtle horizontal movement - mostly straight down
                float horizontalSpeed = Mathf.Clamp(horizontalDistance * 0.1f, -maxTrackingSpeed, maxTrackingSpeed);
                
                // Add small amount of randomness for natural look
                if (trackingAccuracy > 0f)
                {
                    horizontalSpeed += Random.Range(-trackingAccuracy * 0.5f, trackingAccuracy * 0.5f);
                }
                
                rb.linearVelocity = new Vector2(horizontalSpeed, -fallSpeed);
            }
            else
            {
                // Fall straight down (original behavior)
                rb.linearVelocity = new Vector2(0f, -fallSpeed);
            }
        }
        
        // Update visual effects
        UpdateVisualEffects();
        
        // Destroy when off screen
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        // Check if we hit the player
        if (collision.collider != null && IsPlayerCollider(collision.collider))
        {
            // Check for invincible mode cheat first
            var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
            if (obstacleSpawner != null && obstacleSpawner.IsInvincibleModeEnabled())
            {
                Debug.Log("=== INVINCIBLE MODE: Meteorite collision ignored (no damage/effects) ===");
                // Still destroy the meteorite but skip damage and effects
                Destroy(gameObject);
                return;
            }
            
            // Play collision sound
            PlayMeteoriteCollisionSound();
            
            // Use GameManager to handle player damage (Mario-style invincibility)
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.PlayerHit();
            }
        }

        // Spawn impact VFX
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
        }

        // Destroy the meteor
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        // Check if we hit the player
        if (IsPlayerCollider(other))
        {
            // Check for invincible mode cheat first
            var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
            if (obstacleSpawner != null && obstacleSpawner.IsInvincibleModeEnabled())
            {
                Debug.Log("=== INVINCIBLE MODE: Meteorite collision ignored (no damage/effects) ===");
                // Still destroy the meteorite but skip damage and effects
                Destroy(gameObject);
                return;
            }
            
            // Play collision sound
            PlayMeteoriteCollisionSound();
            
            // Use GameManager to handle player damage (Mario-style invincibility)
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.PlayerHit();
            }
        }

        // Spawn impact VFX
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
        }

        // Destroy the meteor
        Destroy(gameObject);
    }

    /// <summary>
    /// Force destroy if meteor gets stuck
    /// </summary>
    void ForceDestroy()
    {
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Play meteorite collision sound
    /// </summary>
    private void PlayMeteoriteCollisionSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollisionSound("meteorite");
        }
    }
    
    /// <summary>
    /// Override to specify meteorite collision sound
    /// </summary>
    protected override string GetObstacleType()
    {
        return "meteorite";
    }
    
    /// <summary>
    /// Calculate smart target position based on player movement prediction
    /// </summary>
    private void CalculateSmartTarget()
    {
        if (playerTransform == null) return;
        
        // Get player's current position and velocity
        Vector2 playerPos = playerTransform.position;
        Rigidbody2D playerRB = playerTransform.GetComponent<Rigidbody2D>();
        
        Vector2 playerVelocity = Vector2.zero;
        if (playerRB != null)
        {
            playerVelocity = playerRB.linearVelocity;
        }
        
        // Calculate time to impact (distance / fall speed)
        float distanceToGround = transform.position.y - (-2f); // Assuming ground is at y = -2
        float timeToImpact = distanceToGround / fallSpeed;
        
        // Predict where player will be when meteor hits (more aggressive prediction)
        Vector2 predictedPlayerPos = playerPos + (playerVelocity * timeToImpact);
        
        // Add extra prediction time for smarter aiming
        predictedPlayerPos += playerVelocity * predictionTime;
        
        // Don't clamp bounds - let meteorite track player anywhere
        // This allows meteorites to track fast-moving players properly
        targetPosition = predictedPlayerPos;
        
        // Only log occasionally to avoid spam (2% chance)
        if (Random.value < 0.02f)
        {
            Debug.Log($"Meteorite targeting: Player at {playerPos}, Predicted at {predictedPlayerPos}, Time to impact: {timeToImpact:F2}s");
        }
    }
    
    /// <summary>
    /// Create shadow instance
    /// </summary>
    private void CreateShadow()
    {
        if (shadowPrefab != null)
        {
            // Create shadow as a separate object (not child of meteorite)
            shadowInstance = Instantiate(shadowPrefab);
            
            // Position shadow on ground using raycast
            UpdateShadowPosition();
            
            // Make shadow more oval-shaped and properly sized
            shadowInstance.transform.localScale = new Vector3(1.2f, 0.8f, 1f); // Oval shape
        }
    }
    
    /// <summary>
    /// Update shadow position on ground
    /// </summary>
    private void UpdateShadowPosition()
    {
        if (shadowInstance == null) return;
        
        // Raycast from meteorite position down to find ground
        Vector2 rayStart = transform.position;
        Vector2 rayDirection = Vector2.down;
        float rayDistance = 20f; // Should be enough to reach ground
        
        RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDirection, rayDistance, groundMask);
        
        if (hit.collider != null)
        {
            // Position shadow on ground surface
            Vector3 groundPosition = new Vector3(hit.point.x, hit.point.y + 0.1f, transform.position.z);
            shadowInstance.transform.position = groundPosition;
            
            // Scale shadow based on height (closer to ground = larger shadow)
            float height = transform.position.y - hit.point.y;
            float shadowScale = Mathf.Lerp(1.0f, 0.6f, Mathf.Clamp01(height / 8f));
            
            // Maintain oval shape while scaling
            shadowInstance.transform.localScale = new Vector3(1.2f * shadowScale, 0.8f * shadowScale, 1f);
        }
        else
        {
            // Fallback: position shadow below meteorite if no ground found
            shadowInstance.transform.position = new Vector3(transform.position.x, transform.position.y - 2f, transform.position.z);
        }
    }
    
    /// <summary>
    /// Update visual effects (rotation and shadow)
    /// </summary>
    private void UpdateVisualEffects()
    {
        // Update rotation based on movement direction
        if (enableDynamicRotation && spriteRenderer != null)
        {
            Vector2 velocity = rb.linearVelocity;
            if (velocity.magnitude > 0.1f)
            {
                // Calculate angle based on movement direction
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                // Adjust angle so meteorite points in direction of movement
                angle += 90f; // Add 90 degrees to make it point forward
                spriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
        
        // Update shadow position on ground
        if (shadowInstance != null)
        {
            UpdateShadowPosition();
        }
    }
    
    /// <summary>
    /// Apply difficulty-based scaling to meteorite
    /// </summary>
    private void ApplyDifficultyScaling()
    {
        var difficultyManager = ProgressiveDifficultyManager.Instance;
        if (difficultyManager != null)
        {
            float scaleMultiplier = difficultyManager.GetObstacleScaleMultiplier();
            // Use the prefab's original scale (4x4x4) as the base, then apply difficulty multiplier
            Vector3 baseScale = new Vector3(4f, 4f, 4f); // Prefab's intended base size
            transform.localScale = baseScale * scaleMultiplier;
            
            // Also scale the collider to match visual size
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                // Note: Collider2D scaling is handled automatically by transform.localScale
                // But we might want to adjust physics properties based on scale
                var rigidbody = GetComponent<Rigidbody2D>();
                if (rigidbody != null)
                {
                    // Scale mass proportionally to volume (scale^3)
                    rigidbody.mass = rigidbody.mass * (scaleMultiplier * scaleMultiplier * scaleMultiplier);
                }
            }
        }
    }
    
    /// <summary>
    /// Clean up shadow when meteorite is destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (shadowInstance != null)
        {
            Destroy(shadowInstance);
        }
    }
}
