using UnityEngine;

/// <summary>
/// Meteorite obstacle - falling meteor that can hit the player
/// Adapted from original Meteorite.cs for obstacle system
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
    
    [Header("Effects")]
    [Tooltip("VFX prefab to spawn on impact")]
    public GameObject impactVfxPrefab;

    private Rigidbody2D rb;
    private bool hasImpacted = false;
    private float spawnTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        hasImpacted = false;
        spawnTime = Time.time;
        
        // Reset physics state
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        // Set up auto-destroy timer
        CancelInvoke(nameof(ForceDestroy));
        Invoke(nameof(ForceDestroy), destroyDelay);
    }

    void Update()
    {
        // Make meteor fall down
        if (!hasImpacted)
        {
            rb.linearVelocity = new Vector2(0f, -fallSpeed);
        }
        
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
}
