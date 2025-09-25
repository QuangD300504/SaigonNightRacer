using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float lateralSpeed = 5f;    // A/D left-right (used only if BikeController missing)
    public float jumpForce = 12f;
    // Movement speed is driven by BikeController; no base speed or auto increase here

    [Header("Input Actions")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction slowDownAction;
    public InputAction boostAction;

    Rigidbody2D rb;
    bool grounded = true;
    float currentSpeed;
    BikeController bikeController;
    Vector2 moveInput;
    
    [Header("Terrain Effects")]
    private float speedModifier = 1f;
    private float jumpModifier = 1f;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = 0f;
        bikeController = GetComponent<BikeController>();
    }

    void OnEnable() {
        moveAction.Enable();
        jumpAction.Enable();
        slowDownAction.Enable();
        boostAction.Enable();
    }

    void OnDisable() {
        moveAction.Disable();
        jumpAction.Disable();
        slowDownAction.Disable();
        boostAction.Disable();
    }

    void Update() {
        // Get input values
        moveInput = moveAction.ReadValue<Vector2>();
        bool jumpPressed = jumpAction.WasPressedThisFrame();


        // left/right movement
        if (bikeController != null)
        {
            // use BikeController signed speed for proper direction changes and coasting
            float vx = bikeController.CurrentSignedSpeed;
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }
        else
        {
            Vector2 vel = rb.linearVelocity;
            vel.x = moveInput.x * lateralSpeed;
            rb.linearVelocity = new Vector2(vel.x, rb.linearVelocity.y);
        }

        // jump
        if (jumpPressed && grounded) {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * jumpModifier);
            grounded = false;
        }

        // Movement speed source now comes from BikeController if present
        if (bikeController != null) currentSpeed = bikeController.CurrentSpeed;
        else currentSpeed = Mathf.Abs(rb.linearVelocity.x);

        // boost (press Shift)
        if (boostAction.WasPressedThisFrame() && bikeController != null) bikeController.ActivateBoost();
        
        // send currentSpeed to GameManager so spawner can use it (with terrain modifier)
        GameManager.Instance.SetWorldSpeed(currentSpeed * speedModifier);
    }

    void OnCollisionEnter2D(Collision2D c) {
        if (c.collider.CompareTag("Ground")) grounded = true;
    }
    
    // Terrain effect methods
    public void ApplySpeedModifier(float modifier)
    {
        speedModifier = modifier;
    }
    
    public void ApplyJumpModifier(float modifier)
    {
        jumpModifier = modifier;
    }
}
