using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BikeController : MonoBehaviour
{
    [Header("Speed Settings")]
	public float baseSpeed = 0f;        // Starting speed at rest
	public float maxSpeed = 22f;        // Max cap (higher top speed)

    [Header("Boost Settings")]
    public float boostMultiplier = 1.8f;
    public float boostDuration = 1.5f;
    private bool isBoosting = false;
	public float boostAccelMultiplier = 1.4f;
	public bool instantBoost = true;          // instantly scale current speed on boost press
	public float overspeedBleedRate = 6f;     // rate to bleed back to normal caps after boost

	[Header("Tuning (realistic feel)")]
	public float accelerationRate = 5f; // m/s^2 toward max while moving (0-22 in ~4.4s)
	public float decelerationRate = 3.5f; // m/s^2 toward 0 when coasting (longer glide)
	public float brakingRate = 9f;     // m/s^2 toward 0 when braking (firm but not instant)

    private float currentSpeed; // signed horizontal speed
    private float targetSpeed;  // signed target speed
    private Rigidbody2D rb;
	private PlayerController playerController; // to reuse bound slowDownAction
	private float speedLogTimer;

    public float CurrentSpeed => Mathf.Abs(currentSpeed);
    public float CurrentSignedSpeed => currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
		playerController = GetComponent<PlayerController>();
        currentSpeed = baseSpeed;
    }

    void Update()
    {
		// Determine if slow down is pressed via PlayerController input action (fallback to keys)
		bool isSlowingDown = false;
		if (playerController != null && playerController.slowDownAction != null)
			isSlowingDown = playerController.slowDownAction.IsPressed();
		else
			isSlowingDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // If PlayerController exists, tie desired speed to horizontal input with acceleration/deceleration
		if (playerController != null && playerController.moveAction != null)
		{
			Vector2 move = playerController.moveAction.ReadValue<Vector2>();
            bool isMoving = Mathf.Abs(move.x) > 0.01f;
            float desiredDir = isMoving ? Mathf.Sign(move.x) : 0f;

			// Apply boost multiplier to maxSpeed if boosting
			float effectiveMaxSpeed = isBoosting ? maxSpeed * boostMultiplier : maxSpeed;

			targetSpeed = isSlowingDown ? 0f : (isMoving ? desiredDir * effectiveMaxSpeed : 0f);

            // Use stronger rate when changing direction to pass through zero before accelerating opposite
			float rate;
            if (isSlowingDown) rate = brakingRate;
            else if (Mathf.Abs(targetSpeed) < 0.01f) rate = decelerationRate;
            else if (Mathf.Sign(targetSpeed) != Mathf.Sign(currentSpeed) && Mathf.Abs(currentSpeed) > 0.01f) rate = brakingRate;
			else rate = accelerationRate * (isBoosting ? boostAccelMultiplier : 1f);

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
		}
		else
		{
			float effectiveMaxSpeed = isBoosting ? maxSpeed *  boostMultiplier : maxSpeed;
			targetSpeed = isSlowingDown ? 0f : effectiveMaxSpeed;
			float rate = isSlowingDown ? brakingRate : accelerationRate * (isBoosting ? boostAccelMultiplier : 1f);
			currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
		}

		// If boost ended and we're above the normal cap, bleed back smoothly toward the cap
		if (!isBoosting)
		{
			float desiredCap;
			if (playerController != null && playerController.moveAction != null)
			{
				Vector2 moveNow = playerController.moveAction.ReadValue<Vector2>();
				bool movingNow = Mathf.Abs(moveNow.x) > 0.01f;
				desiredCap = movingNow ? maxSpeed : 0f;
			}
			else
			{
				desiredCap = maxSpeed;
			}

			float capMag = Mathf.Abs(desiredCap);
			if (Mathf.Abs(currentSpeed) > capMag + 0.01f)
			{
				float targetCapSigned = Mathf.Sign(currentSpeed) * desiredCap;
				currentSpeed = Mathf.MoveTowards(currentSpeed, targetCapSigned, overspeedBleedRate * Time.deltaTime);
			}
		}

        // Periodic speed log (once per second)
		speedLogTimer += Time.deltaTime;
		if (speedLogTimer >= 1f)
		{
            Debug.Log($"[BikeController] Current speed: {Mathf.Abs(currentSpeed):F2}" + (isBoosting ? " (BOOST)" : ""));
			speedLogTimer = 0f;
		}
    }

    void FixedUpdate()
    {
        // Only control forward X velocity if no PlayerController (to avoid overriding lateral input)
		if (playerController == null)
		{
            rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
		}
    }

    // Trigger boost externally (from PlayerController)
    public void ActivateBoost()
    {
        if (isBoosting) return;
        if (instantBoost)
            currentSpeed *= boostMultiplier; // scale current signed speed instantly
        StartCoroutine(BoostRoutine());
    }

    private System.Collections.IEnumerator BoostRoutine()
    {
        isBoosting = true;
        yield return new WaitForSeconds(boostDuration);
        isBoosting = false;
    }
}
