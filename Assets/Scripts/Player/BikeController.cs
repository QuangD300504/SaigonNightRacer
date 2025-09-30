using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BikeController : MonoBehaviour
{
    [Header("Speed Settings")]
    public float baseSpeed = 0f;
    public float maxSpeed = 22f;

    [Header("Boost Settings")]
    public float boostMultiplier = 1.8f;
    public float boostDuration = 1.5f;
    public float boostAccelMultiplier = 1.4f;
    public bool instantBoost = true;
    public float overspeedBleedRate = 6f;

    [Header("Tuning (realistic feel)")]
    public float accelerationRate = 5f;
    public float decelerationRate = 3.5f;
    public float brakingRate = 9f;

    [Header("Drive / Wheelie Control")]
    public float wheelTorque = 150f;           
    public float antiWheelieStrength = 40f;    

    [Header("Slope Physics")]
    public float gravityMultiplier = 1.2f;     

    [Header("Wheel References")]
    public Transform frontWheelTransform;
    public Transform backWheelTransform;
    
    [Header("WheelJoint2D References")]
    public WheelJoint2D backWheelJoint;

    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float groundCheckRadius = 0.1f;     
    public LayerMask groundLayerMask;          
    public float jumpCooldown = 0.15f;

    [Header("Rotation Controls")]
    public float rotationTorque = 5f; // how strong the spin is

    private Rigidbody2D rb;
    private PlayerController playerController;
    private Rigidbody2D frontWheelRB, backWheelRB;
    private JointMotor2D backMotor;

    private float currentSpeed, targetSpeed;
    private bool frontGrounded, backGrounded;
    private float lastJumpTime = -999f;

    private Vector2? frontNormal, backNormal;
    private bool boosting;

    public float CurrentSpeed => Mathf.Abs(currentSpeed);
    public float CurrentSignedSpeed => currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.centerOfMass = new Vector2(0.12f, -0.18f);   
        rb.linearDamping = 0.05f;                       
        rb.angularDamping = 3f;                         

        playerController = GetComponent<PlayerController>();

        if (frontWheelTransform) frontWheelRB = frontWheelTransform.GetComponent<Rigidbody2D>();
        if (backWheelTransform)  backWheelRB  = backWheelTransform.GetComponent<Rigidbody2D>();

        if (backWheelJoint != null)
        {
            backMotor = backWheelJoint.motor;
            backWheelJoint.useMotor = true;
        }

        currentSpeed = baseSpeed;
    }

    void Update()
    {
        HandleMovementInput();
        CheckGrounded();
        HandleJumpInput();
        HandleRotationInput();
    }

    void FixedUpdate()
    {
        ApplyMotor();
        ApplySlopeGravity();
        ApplyAntiWheelie();
    }

    private void HandleMovementInput()
    {
        bool isSlowingDown = playerController != null && playerController.slowDownAction != null
                           ? playerController.slowDownAction.IsPressed()
                           : false;

        float effectiveMaxSpeed = instantBoost && IsBoosting() ? maxSpeed * boostMultiplier : maxSpeed;

        if (playerController != null && playerController.moveAction != null)
        {
            Vector2 move = playerController.moveAction.ReadValue<Vector2>();
            bool isMoving = Mathf.Abs(move.x) > 0.01f;
            float desiredDir = isMoving ? Mathf.Sign(move.x) : 0f;

            targetSpeed = isSlowingDown ? 0f : (isMoving ? desiredDir * effectiveMaxSpeed : 0f);

            float rate;
            if (isSlowingDown) rate = brakingRate;
            else if (Mathf.Abs(targetSpeed) < 0.01f) rate = decelerationRate;
            else if (Mathf.Sign(targetSpeed) != Mathf.Sign(currentSpeed) && Mathf.Abs(currentSpeed) > 0.01f) 
                rate = brakingRate;
            else 
                rate = accelerationRate * (IsBoosting() ? boostAccelMultiplier : 1f);

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
        }

        if (!IsBoosting() && Mathf.Abs(currentSpeed) > maxSpeed)
        {
            float cap = Mathf.Sign(currentSpeed) * maxSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, cap, overspeedBleedRate * Time.deltaTime);
        }
    }

    private void ApplyMotor()
    {
        if (backWheelJoint == null || backWheelRB == null) return;

        float targetAngularSpeed = -currentSpeed * 200f;
        backMotor.motorSpeed = targetAngularSpeed;
        backMotor.maxMotorTorque = wheelTorque;
        backWheelJoint.motor = backMotor;
    }

    private void ApplySlopeGravity()
    {
        if (!(frontGrounded || backGrounded)) return;

        Vector2 n;
        if (frontGrounded && frontNormal.HasValue) n = frontNormal.Value.normalized;
        else if (backGrounded && backNormal.HasValue) n = backNormal.Value.normalized;
        else return;

        Vector2 g = Physics2D.gravity * rb.mass * gravityMultiplier;
        Vector2 gParallel = g - Vector2.Dot(g, n) * n;
        rb.AddForce(gParallel, ForceMode2D.Force);
    }

    private void HandleJumpInput()
    {
        if (playerController == null || playerController.jumpAction == null) return;

        if (playerController.jumpAction.WasPressedThisFrame()
            && Time.time - lastJumpTime >= jumpCooldown
            && frontGrounded && backGrounded) // ✅ require both
        {
            // Apply jump force to main bike body
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            
            // Apply jump force to wheels for proper bike jump
            if (frontWheelRB != null)
            {
                frontWheelRB.AddForce(Vector2.up * jumpForce * 0.5f, ForceMode2D.Impulse);
            }
            
            if (backWheelRB != null)
            {
                backWheelRB.AddForce(Vector2.up * jumpForce * 0.5f, ForceMode2D.Impulse);
            }
            
            lastJumpTime = Time.time;
        }
    }

    private void CheckGrounded()
    {
        frontGrounded = backGrounded = false;
        frontNormal = backNormal = null;

        if (frontWheelTransform)
        {
            frontGrounded = Physics2D.OverlapCircle(frontWheelTransform.position, groundCheckRadius, groundLayerMask);
            var hit = Physics2D.Raycast(frontWheelTransform.position, Vector2.down, groundCheckRadius * 2f, groundLayerMask);
            if (hit.collider) frontNormal = hit.normal;
        }

        if (backWheelTransform)
        {
            backGrounded = Physics2D.OverlapCircle(backWheelTransform.position, groundCheckRadius, groundLayerMask);
            var hit = Physics2D.Raycast(backWheelTransform.position, Vector2.down, groundCheckRadius * 2f, groundLayerMask);
            if (hit.collider) backNormal = hit.normal;
        }
    }

    private void ApplyAntiWheelie()
    {
        if (backGrounded && !frontGrounded)
            rb.AddTorque(-antiWheelieStrength * Time.fixedDeltaTime, ForceMode2D.Force);
    }

    public void ActivateBoost()
    {
        if (IsBoosting()) return;
        if (instantBoost) 
            currentSpeed = Mathf.Clamp(currentSpeed * boostMultiplier, -maxSpeed * boostMultiplier, maxSpeed * boostMultiplier);
        StartCoroutine(BoostRoutine());
    }

    private System.Collections.IEnumerator BoostRoutine()
    {
        boosting = true;
        yield return new WaitForSeconds(boostDuration);
        boosting = false;
    }

    private bool IsBoosting() => boosting;

    public void SetWheelFriction(float backDrag, float frontDrag)
    {
        if (backWheelRB) backWheelRB.linearDamping = backDrag;
        if (frontWheelRB) frontWheelRB.linearDamping = frontDrag;
    }

    private void HandleRotationInput()
    {
        // Only allow rotation when bike is in the air (not grounded)
        bool isInAir = !(frontGrounded || backGrounded);
        
        if (!isInAir) return; // Don't rotate when grounded
        
        // Debug logging
        if (playerController != null)
        {
            bool qPressed = playerController.rotateLeftAction != null && playerController.rotateLeftAction.IsPressed();
            bool ePressed = playerController.rotateRightAction != null && playerController.rotateRightAction.IsPressed();
            
            if (qPressed || ePressed)
            {
                Debug.Log($"Rotation input detected - Q: {qPressed}, E: {ePressed}, InAir: {isInAir}");
            }
        }
        
        // Spin backward (Q) - using Input System
        if (playerController != null && playerController.rotateLeftAction != null && playerController.rotateLeftAction.IsPressed())
        {
            rb.AddTorque(rotationTorque);
            Debug.Log($"Applying left torque: {rotationTorque}");
        }

        // Spin forward (E) - using Input System
        if (playerController != null && playerController.rotateRightAction != null && playerController.rotateRightAction.IsPressed())
        {
            rb.AddTorque(-rotationTorque);
            Debug.Log($"Applying right torque: {-rotationTorque}");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (frontWheelTransform) Gizmos.DrawWireSphere(frontWheelTransform.position, groundCheckRadius);
        if (backWheelTransform) Gizmos.DrawWireSphere(backWheelTransform.position, groundCheckRadius);
    }
}
