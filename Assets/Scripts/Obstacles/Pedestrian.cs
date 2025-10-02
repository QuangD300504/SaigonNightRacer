using UnityEngine;

/// <summary>
/// Pedestrian obstacle - person crossing the street
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Pedestrian : ObstacleBase
{
    [Header("Pedestrian Settings")]
    
    [Tooltip("Walking speed")]
    public float walkSpeed = 1f;
    
    [Tooltip("Direction: 1 = right, -1 = left")]
    public int direction = 1;
    
    
    [Tooltip("Time before pedestrian starts moving")]
    public float startDelay = 0.5f;

    private Rigidbody2D rb;
    private bool hasStarted = false;
    private float timer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Randomize walking speed slightly
        walkSpeed += Random.Range(-0.3f, 0.3f);
        
        // Randomize start delay
        startDelay += Random.Range(0f, 1f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= startDelay && !hasStarted)
        {
            hasStarted = true;
        }
        
        if (hasStarted)
        {
            // Move horizontally (crossing the street)
            transform.Translate(Vector3.right * walkSpeed * direction * Time.deltaTime);
        }
        
        // Destroy when off screen
        if (transform.position.x < -20f || transform.position.x > 20f) 
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
}
