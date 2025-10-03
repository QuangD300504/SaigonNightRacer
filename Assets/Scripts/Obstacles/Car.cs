using UnityEngine;

/// <summary>
/// Car obstacle - moving vehicle that can hit the player
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Car : ObstacleBase
{
    [Header("Car Settings")]
    
    [Tooltip("Speed of the car moving towards player")]
    public float carSpeed = 3f;
    
    [Tooltip("Direction: 1 = right (towards player), -1 = left (away from player)")]
    public int direction = 1;
    
    
    [Tooltip("Distance from player to start moving")]
    public float activationDistance = 20f;
    
    [Tooltip("Height above terrain to maintain")]
    public float terrainOffset = 0.5f;
    
    [Tooltip("Layer mask for terrain detection")]
    public LayerMask terrainLayerMask = -1;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private bool isMoving = false;
    private bool hasLanded = false;
    private float spawnTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Configure Rigidbody2D for falling and terrain following
        rb.gravityScale = 3f; // Even stronger gravity for faster falling
        rb.linearDamping = 2f; // Less damping to allow falling
        rb.angularDamping = 5f; // Prevent excessive spinning
        
        // Ensure car starts falling immediately
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        // Find player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Add slight random speed variation
        carSpeed += Random.Range(-0.5f, 0.5f);
        
        // Record spawn time
        spawnTime = Time.time;
    }

    void Update()
    {
        if (playerTransform == null) return;
        
        // Check if car has landed on terrain
        if (!hasLanded)
        {
            CheckLanding();
            
            // Fallback: if car has been falling for too long, force it down
            if (Time.time - spawnTime > 5f && transform.position.y > -5f)
            {
                rb.gravityScale = 5f; // Force fall
            }
        }
        
        // Only start moving after landing
        if (hasLanded)
        {
            // Check if car should start moving
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            if (!isMoving && distanceToPlayer <= activationDistance)
            {
                isMoving = true;
            }
            
            if (isMoving)
            {
                // Move towards player using physics
                Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
                
                // Only move horizontally towards player (cars run on ground)
                Vector2 horizontalDirection = new Vector2(directionToPlayer.x, 0f).normalized;
                Vector2 moveDirection = horizontalDirection * direction; // Apply direction multiplier
                
                // Apply force to move towards player
                rb.AddForce(moveDirection * carSpeed * 100f * Time.deltaTime);
                
                // Keep car on terrain using physics (simplified)
                float terrainHeight = GetTerrainHeightAtX(transform.position.x);
                float targetY = terrainHeight + terrainOffset;
                
                // Only apply downward force if above terrain (to stick to ground)
                if (transform.position.y > targetY + 0.5f)
                {
                    float forceNeeded = (transform.position.y - targetY) * 20f;
                    rb.AddForce(Vector2.down * forceNeeded);
                }
            }
        }
        
        // Destroy when off screen or too far behind player
        if (transform.position.x < playerTransform.position.x - 30f) 
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            // Handle collision (damage, knockback, destroy)
            HandlePlayerCollision();
        }
    }
    
    /// <summary>
    /// Get terrain height at specific X position using raycast
    /// </summary>
    private float GetTerrainHeightAtX(float x)
    {
        // Cast ray from above to find terrain surface
        Vector2 rayStart = new Vector2(x, transform.position.y + 15f);
        Vector2 rayDirection = Vector2.down;
        float rayDistance = 30f;
        
        // Try with default layer mask first
        RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDirection, rayDistance);
        
        if (hit.collider != null)
        {
            return hit.point.y;
        }
        
        // Try with terrain layer mask
        hit = Physics2D.Raycast(rayStart, rayDirection, rayDistance, terrainLayerMask);
        
        if (hit.collider != null)
        {
            return hit.point.y;
        }
        
        // Fallback: try multiple attempts with slight X variations
        for (int i = 0; i < 5; i++)
        {
            float offsetX = x + Random.Range(-1f, 1f);
            rayStart = new Vector2(offsetX, transform.position.y + 15f);
            hit = Physics2D.Raycast(rayStart, rayDirection, rayDistance);
            
            if (hit.collider != null)
            {
                return hit.point.y;
            }
        }
        
        return transform.position.y; // Keep current height if no terrain found
    }
    
    /// <summary>
    /// Check if car has landed on terrain
    /// </summary>
    private void CheckLanding()
    {
        // Check if car is close to terrain height
        float terrainHeight = GetTerrainHeightAtX(transform.position.x);
        float distanceToTerrain = Mathf.Abs(transform.position.y - terrainHeight);
        
        
        // Consider landed if very close to terrain and not moving much vertically
        if (distanceToTerrain < 0.5f && Mathf.Abs(rb.linearVelocity.y) < 0.5f)
        {
            hasLanded = true;
            
            // Disable gravity after landing to prevent bouncing
            rb.gravityScale = 0f;
            
            // Position car properly on terrain
            Vector3 pos = transform.position;
            pos.y = terrainHeight + terrainOffset;
            transform.position = pos;
            
            // Stop vertical movement completely
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            
        }
    }
}
