using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // Movement speed is driven by BikeController

    [Header("Input Actions")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction slowDownAction;
    public InputAction boostAction;
    public InputAction rotateLeftAction;
    public InputAction rotateRightAction;

    Rigidbody2D rb;
    float currentSpeed;
    BikeController bikeController;
    Vector2 moveInput;
    
    [Header("Terrain Effects")]
    private float speedModifier = 1f;

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
        rotateLeftAction.Enable();
        rotateRightAction.Enable();
    }

    void OnDisable() {
        moveAction.Disable();
        jumpAction.Disable();
        slowDownAction.Disable();
        boostAction.Disable();
        rotateLeftAction.Disable();
        rotateRightAction.Disable();
    }

    void Update() {
        // Get input values
        moveInput = moveAction.ReadValue<Vector2>();

        // BikeController handles all movement, jump, and boost logic
        // We only need to track speed for GameManager
        currentSpeed = bikeController.CurrentSpeed;

        // boost (press Shift)
        if (boostAction.WasPressedThisFrame()) 
        {
            bikeController.ActivateBoost();
        }
        
        // send currentSpeed to GameManager so spawner can use it (with terrain modifier)
        var gm = GameManager.Instance;
        if (gm != null) gm.SetWorldSpeed(currentSpeed * speedModifier);
    }

    // Terrain effect methods
    public void ApplySpeedModifier(float modifier)
    {
        speedModifier = modifier;
    }
}
