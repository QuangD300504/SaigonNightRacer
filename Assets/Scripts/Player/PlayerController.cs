using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float lateralSpeed = 5f;    // A/D left-right
    public float jumpForce = 12f;
    public float baseSpeed = 6f;       // used for display
    public float speedIncreaseRate = 0.02f; // per second
    public float boostMultiplier = 2f;
    public float boostDuration = 1.2f;

    [Header("Input Actions")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction slowDownAction;
    public InputAction boostAction;

    Rigidbody2D rb;
    bool grounded = true;
    float currentSpeed;
    Vector2 moveInput;
    
    [Header("Terrain Effects")]
    private float speedModifier = 1f;
    private float jumpModifier = 1f;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = baseSpeed;
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
        bool slowDownPressed = slowDownAction.IsPressed();
        bool boostPressed = boostAction.WasPressedThisFrame();

        // left/right movement
        Vector2 vel = rb.linearVelocity;
        vel.x = moveInput.x * lateralSpeed;
        rb.linearVelocity = new Vector2(vel.x, rb.linearVelocity.y);

        // jump
        if (jumpPressed && grounded) {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * jumpModifier);
            grounded = false;
        }

        // slow down
        if (slowDownPressed) {
            currentSpeed = Mathf.Max(1f, currentSpeed - 10f * Time.deltaTime);
        } else {
            // gradually increase
            currentSpeed += speedIncreaseRate * Time.deltaTime;
        }

        // boost
        if (boostPressed) {
            StartCoroutine(DoBoost());
        }

        // send currentSpeed to GameManager so spawner can use it (with terrain modifier)
        GameManager.Instance.SetWorldSpeed(currentSpeed * speedModifier);
    }

    System.Collections.IEnumerator DoBoost() {
        float old = currentSpeed;
        currentSpeed *= boostMultiplier;
        yield return new WaitForSeconds(boostDuration);
        currentSpeed = old;
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
