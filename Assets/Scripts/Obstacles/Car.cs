using UnityEngine;

/// <summary>
/// Car obstacle - falls onto road (Dynamic) then switches to scripted movement (Kinematic).
/// Sticks to road (Edge Collider 2D) and smoothly rotates with slopes.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Car : ObstacleBase
{
    [Header("Car Settings")]
    [Tooltip("Speed of the car moving towards player")]
    public float carSpeed = 5f;

    [Tooltip("Distance from player to start moving")]
    public float activationDistance = 5f;

    [Tooltip("How high above the car we start the raycast")]
    public float groundCheckHeight = 10f;

    [Tooltip("How far down the raycast should go")]
    public float groundCheckDistance = 50f;

    [Tooltip("Offset above the road surface (0 = touching road)")]
    public float hoverOffset = 0.3f;

    [Tooltip("How quickly the car aligns to slopes")]
    public float rotationSmooth = 8f;

    private Transform playerTransform;
    private bool isMoving = false;
    private bool hasLanded = false;
    private Vector2 targetPosition; // Store player position when car spawns
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; // start with gravity
        rb.gravityScale = 2f;                  // adjust fall speed
        rb.freezeRotation = true;              // avoid unwanted spins

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            // Store player position when car spawns - this is the only time we track it
            targetPosition = playerTransform.position;
        }

        // Add slight random speed variation
        carSpeed += Random.Range(-0.5f, 0.5f);
    }

    void FixedUpdate()
    {
        if (playerTransform == null || !hasLanded) return;

        if (!isMoving)
        {
            // Use current player position, not stored position
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= activationDistance)
            {
                isMoving = true;
                // Update target to current player position when activating
                targetPosition = playerTransform.position;
            }
        }

        if (isMoving && rb.bodyType == RigidbodyType2D.Kinematic)
        {
            // Move towards the stored target position (player position when car spawned)
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 newPos = rb.position + direction * carSpeed * Time.fixedDeltaTime;

            // Raycast down to snap to road
            Vector2 rayStart = new Vector2(newPos.x, transform.position.y + groundCheckHeight);
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, groundCheckDistance, LayerMask.GetMask("Ground"));

            if (hit.collider != null)
            {
                // Stick to road
                newPos.y = hit.point.y + hoverOffset;
                rb.MovePosition(newPos);

                // Smooth slope alignment
                float slopeAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
                float smoothAngle = Mathf.LerpAngle(rb.rotation, slopeAngle, rotationSmooth * Time.fixedDeltaTime);
                rb.MoveRotation(smoothAngle);
            }
            else
            {
                rb.MovePosition(newPos);
            }
        }

        // Destroy car if far behind player
        if (playerTransform != null && transform.position.x < playerTransform.position.x - 30f)
            Destroy(gameObject);
    }
    
    /// <summary>
    /// Override to specify car collision sound
    /// </summary>
    protected override string GetObstacleType()
    {
        return "car";
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // First time landing on ground → switch to Kinematic
        if (!hasLanded && collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            hasLanded = true;
        }

        // Collision with player
        if (IsPlayerCollider(collision.collider))
        {
            // Check for invincible mode cheat first
            var obstacleSpawner = FindFirstObjectByType<ObstacleSpawnerNew>();
            if (obstacleSpawner != null && obstacleSpawner.IsInvincibleModeEnabled())
            {
                Debug.Log("=== INVINCIBLE MODE: Car collision ignored (no damage/effects) ===");
                // Still destroy the car but skip damage and effects
                Destroy(gameObject);
                return;
            }
            
            HandlePlayerCollision();
        }
    }
}
