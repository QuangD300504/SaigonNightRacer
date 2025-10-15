using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BikeController : MonoBehaviour
{
    [Header("Speed Settings")]
    public float baseSpeed = 0f; // Increased from 0f for better starting speed
    public float maxSpeed = 45f;
    
    [Header("Speed Boost Settings")]
    [Tooltip("Speed multiplier when speed boost is active")]
    public float speedBoostMultiplier = 1.5f;

    [Header("Boost Settings")]
    public float boostMultiplier = 1.8f;
    public float boostDuration = 1.5f;
    public float boostAccelMultiplier = 1.4f;
    public bool instantBoost = true;
    public float overspeedBleedRate = 6f;

    [Header("Boost Visual Effects")]
    [Tooltip("Animated flame GameObject for boost effect")]
    public GameObject boostFlame;
    
    [Tooltip("Boost cooldown time")]
    public float boostCooldown = 2f;
    
    [Header("Engine Sound Settings")]
    [Tooltip("Minimum speed to play engine rev sound")]
    public float engineRevThreshold = 5f; // Lowered from 15f
    
    [Tooltip("Volume for engine sounds")]
    [Range(0f, 1f)]
    public float engineVolume = 0.7f;

    [Header("Tuning (realistic feel)")]
    public float accelerationRate = 5f;
    public float decelerationRate = 3.5f;
    public float brakingRate = 9f;

    [Header("Drive / Wheelie Control")]
    public float wheelTorque = 150f;           
    public float antiWheelieStrength = 40f;    

    [Header("Slope Physics")]
    public float gravityMultiplier = 0.1f;     // Arcade-style: minimal slope impact
    public float slopeStability = 0.1f; // Arcade-style: very stable
    public float downhillBoost = 1.5f; // Arcade-style: more downhill boost

    [Header("Wheel References")]
    public Transform frontWheelTransform;
    public Transform backWheelTransform;
    
    [Header("WheelJoint2D References")]
    public WheelJoint2D backWheelJoint;

    [Header("Jump Settings")]
    public float jumpForce = 12f;
        public float groundCheckRadius = 1.0f;     
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
    
    // Boost system
    private float lastBoostTime = -999f;
    private bool canBoost = true;
    
    // Power-up states
    private bool isShielded = false;
    private bool isSpeedBoosted = false;
    private float shieldTimer = 0f;
    private float speedBoostTimer = 0f;
    
    // Mario-style invincibility system
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;
    private float invincibilityDuration = 2f; // 2 seconds of invincibility
    private float invincibilitySpeedReduction = 0.8f; // 20% speed reduction during invincibility
    private SpriteRenderer[] spriteRenderers; // For blinking effect
    
    // Engine sound management
    private bool isEnginePlaying = false;
    private bool isEngineRevPlaying = false;
    
    // Landing detection
    private bool wasInAir = false;

    public float CurrentSpeed => Mathf.Abs(currentSpeed);
    public float CurrentSignedSpeed => currentSpeed;

    void Start()
{
    rb = GetComponent<Rigidbody2D>();
    rb.centerOfMass = new Vector2(0.1f, -0.15f); // Slightly higher for better stability
    rb.linearDamping = 0.05f; // Reduced for more responsive movement
    rb.angularDamping = 2f; // Reduced for more responsive rotation

    playerController = GetComponent<PlayerController>();

    if (frontWheelTransform) frontWheelRB = frontWheelTransform.GetComponent<Rigidbody2D>();
    if (backWheelTransform) backWheelRB  = backWheelTransform.GetComponent<Rigidbody2D>();

    if (backWheelJoint != null)
    {
        backMotor = backWheelJoint.motor;
        backWheelJoint.useMotor = true;
    }

    currentSpeed = baseSpeed;
    
    // Initialize sprite renderers for blinking effect
    spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    
    // Initialize ground layer mask for terrain detection
    if (groundLayerMask == 0)
    {
        groundLayerMask = LayerMask.GetMask("Default");
    }
    
    // Reset engine sound flags only (AudioManager already reset by scene transition)
    isEnginePlaying = false;
    isEngineRevPlaying = false;
    
    // Start engine idle sound when entering Game scene
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.PlayEngineIdle();
        isEnginePlaying = true;
    }
}

void Update()
{
    HandlePowerUps();
    HandleMovementInput();
    CheckGrounded();
    HandleJumpInput();
    HandleRotationInput();
    HandleBoostCooldown();
    HandleEngineSounds();
}

void FixedUpdate()
{
    ApplyMotor();
    ApplySlopeGravity();
    ApplyAntiWheelie();
    ApplySlopeStabilization();
}

private void HandleMovementInput()
{
    bool isSlowingDown = playerController != null && playerController.slowDownAction != null
                       ? playerController.slowDownAction.IsPressed()
                       : false;

    // Use Inspector maxSpeed as base, then apply phase multiplier
    float effectiveMaxSpeed = maxSpeed;
    
    // Apply difficulty-based speed multiplier (this overrides Inspector maxSpeed)
    var difficultyManager = ProgressiveDifficultyManager.Instance;
    if (difficultyManager != null)
    {
        float speedMultiplier = difficultyManager.GetPlayerSpeedMultiplier();
        effectiveMaxSpeed = maxSpeed * speedMultiplier;
    }
    
    // Apply speed boost multiplier if active
    if (isSpeedBoosted)
    {
        effectiveMaxSpeed *= speedBoostMultiplier;
    }
    
    // Apply invincibility speed reduction if active
    if (isInvincible)
    {
        effectiveMaxSpeed *= invincibilitySpeedReduction;
    }
    
    // Apply hill physics (downhill boost and uphill reduction)
    float hillMultiplier = GetHillMultiplier();
    if (hillMultiplier != 1f)
    {
        effectiveMaxSpeed *= hillMultiplier;
    }
    
    // Apply boost multiplier if boosting
    if (instantBoost && IsBoosting())
    {
        effectiveMaxSpeed *= boostMultiplier;
    }

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

    if (!IsBoosting() && Mathf.Abs(currentSpeed) > effectiveMaxSpeed)
    {
        float cap = Mathf.Sign(currentSpeed) * effectiveMaxSpeed;
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

        // Only apply slope gravity if we're actually on a steep slope
        float slopeAngle = Vector2.Angle(Vector2.up, n);
        if (slopeAngle < 20f) return; // Arcade-style: only apply on very steep slopes (>20°)

        // Simplified slope gravity - more predictable
        Vector2 g = Physics2D.gravity * rb.mass * gravityMultiplier * slopeStability;
        Vector2 gParallel = g - Vector2.Dot(g, n) * n;
        
        // Apply force more smoothly
        rb.AddForce(gParallel * Time.fixedDeltaTime, ForceMode2D.Force);
    }

private void HandleJumpInput()
    {
        if (playerController == null || playerController.jumpAction == null) return;

        if (playerController.jumpAction.WasPressedThisFrame()
            && Time.time - lastJumpTime >= jumpCooldown
            && (frontGrounded || backGrounded)) // ✅ require only one wheel
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
        bool wasGrounded = frontGrounded || backGrounded;
        frontGrounded = backGrounded = false;
        frontNormal = backNormal = null;

        if (frontWheelTransform)
        {
            // Use multiple raycasts for better slope detection
            Vector2[] rayDirections = {
                Vector2.down,
                Vector2.down + Vector2.left * 0.3f,
                Vector2.down + Vector2.right * 0.3f
            };
            
            foreach (Vector2 direction in rayDirections)
            {
                var hit = Physics2D.Raycast(frontWheelTransform.position, direction, groundCheckRadius * 3f, groundLayerMask);
                if (hit.collider) 
                {
                    frontGrounded = true;
                    frontNormal = hit.normal;
                    break;
                }
            }
        }

        if (backWheelTransform)
        {
            // Use multiple raycasts for better slope detection
            Vector2[] rayDirections = {
                Vector2.down,
                Vector2.down + Vector2.left * 0.3f,
                Vector2.down + Vector2.right * 0.3f
            };
            
            foreach (Vector2 direction in rayDirections)
            {
                var hit = Physics2D.Raycast(backWheelTransform.position, direction, groundCheckRadius * 3f, groundLayerMask);
                if (hit.collider) 
                {
                    backGrounded = true;
                    backNormal = hit.normal;
                    break;
                }
            }
        }
        
        // Check for landing (was in air, now grounded)
        bool isCurrentlyGrounded = frontGrounded || backGrounded;
        if (!wasGrounded && isCurrentlyGrounded && wasInAir)
        {
            // Player just landed - play landing sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJumpLandingSound();
            }
        }
        
        // Update air state
        wasInAir = !isCurrentlyGrounded;
    }

private void ApplyAntiWheelie()
    {
        if (backGrounded && !frontGrounded)
        {
            // More responsive anti-wheelie with angle consideration
            float angle = Vector2.SignedAngle(Vector2.up, transform.up);
            float antiWheelieForce = antiWheelieStrength * (1f + Mathf.Abs(angle) / 45f);
            rb.AddTorque(-antiWheelieForce * Time.fixedDeltaTime, ForceMode2D.Force);
        }
    }
    
    /// <summary>
    /// Apply slope stabilization to prevent sliding and improve control
    /// </summary>
    private void ApplySlopeStabilization()
    {
        if (!(frontGrounded || backGrounded)) return;
        
        Vector2 n;
        if (frontGrounded && frontNormal.HasValue) n = frontNormal.Value.normalized;
        else if (backGrounded && backNormal.HasValue) n = backNormal.Value.normalized;
        else return;
        
        // Calculate slope angle
        float slopeAngle = Vector2.Angle(Vector2.up, n);
        if (slopeAngle < 20f) return; // Arcade-style: only stabilize on very steep slopes
        
        // Apply very light stabilization force to prevent sliding
        Vector2 velocity = rb.linearVelocity;
        Vector2 perpendicularVelocity = Vector2.Dot(velocity, Vector2.Perpendicular(n)) * Vector2.Perpendicular(n);
        
        // Apply minimal counter-force to reduce sliding
        float stabilizationForce = slopeStability * perpendicularVelocity.magnitude * 0.1f; // Much lighter
        rb.AddForce(-perpendicularVelocity * stabilizationForce, ForceMode2D.Force);
        
        // Apply very light upward force to maintain contact with slope
        Vector2 upwardForce = n * slopeStability * 0.1f; // Much lighter
        rb.AddForce(upwardForce, ForceMode2D.Force);
    }
    
    /// <summary>
    /// Calculate hill speed multiplier based on slope angle and direction (uphill reduction + downhill boost)
    /// </summary>
    private float GetHillMultiplier()
    {
        if (!(frontGrounded || backGrounded)) return 1f;
        
        Vector2 n;
        if (frontGrounded && frontNormal.HasValue) n = frontNormal.Value.normalized;
        else if (backGrounded && backNormal.HasValue) n = backNormal.Value.normalized;
        else return 1f;
        
        // Calculate slope angle
        float slopeAngle = Vector2.Angle(Vector2.up, n);
        if (slopeAngle < 8f) return 1f; // No effect on gentle slopes
        
        // Check if we're going downhill or uphill
        Vector2 slopeDirection = Vector2.Perpendicular(n).normalized;
        float movementDirection = Mathf.Sign(currentSpeed);
        
        // Calculate steepness factor (0-1 based on 8-40 degree slopes)
        float steepness = Mathf.Clamp01((slopeAngle - 8f) / 32f);
        
        // If slope direction matches movement direction, we're going downhill
        if (Mathf.Sign(slopeDirection.x) == movementDirection)
        {
            // Downhill boost
            return 1f + (downhillBoost - 1f) * steepness;
        }
        else
        {
            // Uphill reduction (reduce max speed going uphill)
            float uphillReduction = 0.3f; // Reduce max speed by up to 30% on steep uphills
            return 1f - (uphillReduction * steepness);
        }
    }
    
public void ActivateBoost()
{
    // Check if boost is available
    if (!canBoost || IsBoosting()) return;
    
    // Get difficulty-adjusted boost values
    var difficultyManager = ProgressiveDifficultyManager.Instance;
    float currentBoostMultiplier = difficultyManager != null ? difficultyManager.GetBoostMultiplier() : boostMultiplier;
    float currentBoostDuration = difficultyManager != null ? difficultyManager.GetBoostDuration() : boostDuration;
    
    // Apply boost effect
    if (instantBoost) 
        currentSpeed = Mathf.Clamp(currentSpeed * currentBoostMultiplier, -maxSpeed * currentBoostMultiplier, maxSpeed * currentBoostMultiplier);
    
    // Start boost routine with difficulty-adjusted duration
    StartCoroutine(BoostRoutine(currentBoostDuration));
    
    // Play visual and audio effects
    PlayBoostEffects();
    
    // Set cooldown
    lastBoostTime = Time.time;
    canBoost = false;
}

private System.Collections.IEnumerator BoostRoutine(float duration)
{
    boosting = true;
    
    // Enable flame effect
    if (boostFlame != null) boostFlame.SetActive(true);
    
    yield return new WaitForSeconds(duration);
    
    // Disable flame effect with smooth fade
    boosting = false;
    if (boostFlame != null)
    {
        var flameFade = boostFlame.GetComponent<FlameFade>();
        if (flameFade != null)
        {
            flameFade.FadeOut(0.5f); // Fade out over 0.5 seconds
        }
        else
        {
            boostFlame.SetActive(false); // Fallback if no FlameFade component
        }
    }
}

/// <summary>
/// Check if boost is available
/// </summary>
public bool CanBoost()
{
    return canBoost && !IsBoosting();
}

/// <summary>
/// Handle boost cooldown system
/// </summary>
private void HandleBoostCooldown()
{
    // Get difficulty-adjusted cooldown
    var difficultyManager = ProgressiveDifficultyManager.Instance;
    float currentBoostCooldown = difficultyManager != null ? difficultyManager.GetBoostCooldown() : boostCooldown;
    
    if (!canBoost && Time.time - lastBoostTime >= currentBoostCooldown)
    {
        canBoost = true;
    }
}

/// <summary>
/// Play boost visual and audio effects
/// </summary>
private void PlayBoostEffects()
{
    // Play boost sound
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.PlayBoostSound();
    }
    
    // Screen shake removed to prevent background glitching
}


/// <summary>
/// Get boost cooldown progress (0-1)
/// </summary>
public float GetBoostCooldownProgress()
{
    if (canBoost) return 1f;
    
    // Get difficulty-adjusted cooldown
    var difficultyManager = ProgressiveDifficultyManager.Instance;
    float currentBoostCooldown = difficultyManager != null ? difficultyManager.GetBoostCooldown() : boostCooldown;
    
    return Mathf.Clamp01((Time.time - lastBoostTime) / currentBoostCooldown);
}

/// <summary>
/// Check if player is currently boosting
/// </summary>
public bool IsBoosting() => boosting;

public void SetWheelFriction(float backDrag, float frontDrag)
    {
        if (backWheelRB) backWheelRB.linearDamping = backDrag;
        if (frontWheelRB) frontWheelRB.linearDamping = frontDrag;
    }

private void HandleRotationInput()
    {
        // Allow rotation both in air and on ground
        // bool isInAir = !(frontGrounded || backGrounded);
        
        // if (!isInAir) return; // Don't rotate when grounded
        
        // Spin backward (Q) - using Input System
        if (playerController != null && playerController.rotateLeftAction != null && playerController.rotateLeftAction.IsPressed())
        {
            rb.AddTorque(rotationTorque);
        }

        // Spin forward (E) - using Input System
        if (playerController != null && playerController.rotateRightAction != null && playerController.rotateRightAction.IsPressed())
        {
            rb.AddTorque(-rotationTorque);
        }
    }

void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (frontWheelTransform) Gizmos.DrawWireSphere(frontWheelTransform.position, groundCheckRadius);
        if (backWheelTransform) Gizmos.DrawWireSphere(backWheelTransform.position, groundCheckRadius);
    }
    
    // ===== POWER-UP METHODS =====
    
    /// <summary>
    /// Activate shield power-up (temporary invincibility)
    /// </summary>
    public void ActivateShield(float duration)
    {
        if (isShielded) return; // Don't stack shields
        
        isShielded = true;
        shieldTimer = duration;
        
        // Optional: Add visual effect here
        // You could spawn a shield particle effect or change the player's appearance
    }
    
    /// <summary>
    /// Activate speed boost power-up (temporary speed increase)
    /// </summary>
    public void ActivateSpeedBoost(float duration)
    {
        if (isSpeedBoosted) return; // Don't stack speed boosts
        
        isSpeedBoosted = true;
        speedBoostTimer = duration;
        
        // Optional: Add visual effect here
        // You could add a speed trail or change the player's appearance
    }
    
    /// <summary>
    /// Check if player is currently shielded (for damage prevention)
    /// </summary>
    public bool IsShielded()
    {
        return isShielded;
    }
    
    /// <summary>
    /// Check if player is currently speed boosted
    /// </summary>
    public bool IsSpeedBoosted()
    {
        return isSpeedBoosted;
    }
    
    /// <summary>
    /// Activate Mario-style invincibility (blinking + speed reduction)
    /// </summary>
    public void ActivateInvincibility()
    {
        if (isInvincible) return; // Don't stack invincibility
        
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
        
        // Start blinking effect
        StartCoroutine(BlinkingEffect());
    }
    
    /// <summary>
    /// Check if player is currently invincible
    /// </summary>
    public bool IsInvincible()
    {
        return isInvincible;
    }
    
    /// <summary>
    /// Blinking effect during invincibility
    /// </summary>
    private System.Collections.IEnumerator BlinkingEffect()
    {
        float blinkInterval = 0.1f; // Blink every 0.1 seconds
        
        while (isInvincible)
        {
            // Toggle visibility of all sprite renderers
            foreach (var renderer in spriteRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = !renderer.enabled;
                }
            }
            
            yield return new WaitForSeconds(blinkInterval);
        }
        
        // Ensure all sprites are visible when invincibility ends
        foreach (var renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }
    
    /// <summary>
    /// Handle power-up timers
    /// </summary>
    private void HandlePowerUps()
    {
        // Handle shield timer
        if (isShielded)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f)
            {
                isShielded = false;
                shieldTimer = 0f;
            }
        }
        
        // Handle speed boost timer
        if (isSpeedBoosted)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f)
            {
                isSpeedBoosted = false;
                speedBoostTimer = 0f;
            }
        }
        
        // Handle invincibility timer
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
                invincibilityTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// Handle engine sound based on current speed
    /// </summary>
    private void HandleEngineSounds()
    {
        if (AudioManager.Instance == null) return;
        
        float currentSpeedAbs = Mathf.Abs(currentSpeed);
        bool isMoving = currentSpeedAbs > 0.1f;
        
        // Determine which engine sound to play
        if (isMoving)
        {
            if (currentSpeedAbs >= engineRevThreshold)
            {
                // Play rev sound for high speed
                if (!isEngineRevPlaying)
                {
                    AudioManager.Instance.PlayEngineRev();
                    isEngineRevPlaying = true;
                    isEnginePlaying = false;
                }
            }
            else
            {
                // Play idle sound for low speed movement
                if (!isEnginePlaying)
                {
                    AudioManager.Instance.PlayEngineIdle();
                    isEnginePlaying = true;
                    isEngineRevPlaying = false;
                }
            }
        }
        else
        {
            // Player is stationary - play idle sound
            if (!isEnginePlaying)
            {
                AudioManager.Instance.PlayEngineIdle();
                isEnginePlaying = true;
                isEngineRevPlaying = false;
            }
        }
    }
}
