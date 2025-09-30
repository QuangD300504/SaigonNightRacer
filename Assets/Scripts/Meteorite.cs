using UnityEngine;

/// <summary>
/// Meteorite behavior - handles falling, damage, and collision
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Meteorite : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage dealt to player on impact")]
    public int damage = 20;
    
    [Tooltip("Time before meteor is forced to recycle if stuck")]
    public float destroyDelay = 5f;
    
    [Header("Effects")]
    [Tooltip("VFX prefab to spawn on impact")]
    public GameObject impactVfxPrefab;
    
    [Tooltip("Reference to the pool this meteor belongs to")]
    public SimplePool pool;

    private Rigidbody2D rb;
    private bool hasImpacted = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        hasImpacted = false;
        
        // Reset physics state
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        // Set up auto-destroy timer
        CancelInvoke(nameof(ForceRecycle));
        Invoke(nameof(ForceRecycle), destroyDelay);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        // Check if we hit the player (with null safety)
        if (collision.collider != null && collision.collider.CompareTag("Player"))
        {
            // Use GameManager to handle player damage
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.PlayerHit();
                Debug.Log($"Meteorite hit player! Lives remaining: {gameManager.lives}");
            }
        }

        // Spawn impact VFX
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
        }

        // Return to pool
        Recycle();
    }

    /// <summary>
    /// Return meteor to pool or destroy it
    /// </summary>
    void Recycle()
    {
        CancelInvoke(nameof(ForceRecycle));
        
        if (pool != null)
        {
            pool.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Force recycle if meteor gets stuck
    /// </summary>
    void ForceRecycle()
    {
        // Only log if we haven't already impacted and it's been a reasonable time
        if (!hasImpacted && Time.time - Time.time >= 3f)
        {
            Debug.Log("Meteorite force recycled - was stuck!");
        }
        Recycle();
    }
}
