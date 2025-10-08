using UnityEngine;

/// <summary>
/// Smart meteorite obstacle with dynamic targeting and visual effects
/// Tracks player movement and adjusts trajectory for challenging gameplay
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MeteoriteObstacle : MonoBehaviour
{
    [Header("Meteorite Settings")]
    [Tooltip("Damage dealt to player on impact")]
    public int damage = 3;
    
    [Tooltip("Time before meteor is destroyed if stuck")]
    public float destroyDelay = 8f;
    
    [Tooltip("Fall speed")]
    public float fallSpeed = 5f;
    
    [Header("Smart Targeting")]
    [Tooltip("Enable smart targeting (aims ahead of player)")]
    public bool enableSmartTargeting = true;
    
    [Tooltip("How far ahead to aim (in seconds)")]
    public float predictionTime = 1.0f;
    
    [Tooltip("Maximum horizontal tracking speed")]
    public float maxTrackingSpeed = 2f;
    
    [Tooltip("Tracking accuracy (0 = perfect, 1 = random)")]
    [Range(0f, 1f)]
    public float trackingAccuracy = 0.3f;
    
    [Header("Visual Effects")]
    [Tooltip("Enable dynamic rotation based on movement direction")]
    public bool enableDynamicRotation = true;
    
    [Tooltip("Enable shadow projection")]
    public bool enableShadow = true;
    
    [Tooltip("Shadow prefab to instantiate")]
    public GameObject shadowPrefab;
    
    [Tooltip("Shadow offset below meteorite")]
    public float shadowOffset = 0.5f;
    
    [Header("Effects")]
    [Tooltip("VFX prefab to spawn on impact")]
    public GameObject impactVfxPrefab;

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
    private Vector2 lastVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        hasImpacted = false;
        spawnTime = Time.time;
        
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
                // Continuously update target position as player moves
                CalculateSmartTarget();
                
                // Smart targeting: move towards predicted player position
                Vector2 currentPos = transform.position;
                Vector2 directionToTarget = (targetPosition - currentPos).normalized;
                
                // Calculate horizontal velocity towards target (subtle tracking)
                float distanceToTarget = Mathf.Abs(targetPosition.x - currentPos.x);
                float horizontalSpeed = Mathf.Min(maxTrackingSpeed * 0.3f, distanceToTarget / (predictionTime * 2f));
                
                // Limit direction changes to prevent dramatic turns
                Vector2 desiredVelocity = new Vector2(directionToTarget.x * horizontalSpeed, -fallSpeed);
                Vector2 currentVelocity = rb.linearVelocity;
                
                // Gradually adjust velocity instead of instant changes
                float maxChange = maxTrackingSpeed * 0.1f * Time.deltaTime;
                Vector2 velocityChange = desiredVelocity - currentVelocity;
                velocityChange.x = Mathf.Clamp(velocityChange.x, -maxChange, maxChange);
                
                Vector2 horizontalVelocity = currentVelocity + velocityChange;
                horizontalVelocity.y = -fallSpeed; // Always fall at constant speed
                
                // Add some randomness for tracking accuracy
                if (trackingAccuracy > 0f)
                {
                    float randomOffset = Random.Range(-trackingAccuracy, trackingAccuracy) * maxTrackingSpeed;
                    horizontalVelocity.x += randomOffset;
                }
                
                rb.linearVelocity = horizontalVelocity;
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
            // Use GameManager to handle player damage and knockback
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
            // Use GameManager to handle player damage and knockback
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
    /// Check if collider belongs to player (direct tag or child of Player)
    /// </summary>
    private bool IsPlayerCollider(Collider2D other)
    {
        return other.CompareTag("Player") || 
               (other.transform.parent != null && other.transform.parent.CompareTag("Player"));
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
        
        // Only log occasionally to avoid spam
        if (Random.Range(0f, 1f) < 0.1f) // 10% chance to log
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
            shadowInstance = Instantiate(shadowPrefab, transform);
            shadowInstance.transform.localPosition = new Vector3(0f, -shadowOffset, 0f);
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
        
        // Update shadow position
        if (shadowInstance != null)
        {
            // Keep shadow below meteorite
            shadowInstance.transform.localPosition = new Vector3(0f, -shadowOffset, 0f);
            
            // Scale shadow based on height (closer to ground = larger shadow)
            float height = transform.position.y;
            float shadowScale = Mathf.Lerp(1.5f, 0.5f, Mathf.Clamp01(height / 10f));
            shadowInstance.transform.localScale = Vector3.one * shadowScale;
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
