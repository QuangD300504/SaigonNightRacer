using UnityEngine;

/// <summary>
/// Setup guide for obstacle system
/// This script provides instructions and validation for setting up obstacles
/// </summary>
public class ObstacleSetupGuide : MonoBehaviour
{
    [Header("Setup Instructions")]
    [TextArea(10, 20)]
    public string setupInstructions = @"
OBSTACLE SETUP GUIDE (MODULAR SYSTEM):

1. CREATE PREFABS:
   - Create prefabs for each obstacle type:
     * Traffic Cone (with TrafficCone.cs script)
     * Car (with Car.cs script) 
     * Pedestrian (with Pedestrian.cs script)
     * Meteorite (with MeteoriteObstacle.cs script)

2. SETUP PREFAB COMPONENTS:
   Each prefab needs:
   - SpriteRenderer (with sprite)
   - Collider2D (set as Trigger)
   - Rigidbody2D (for physics)
   - Appropriate obstacle script

3. CONFIGURE MODULAR OBSTACLE SYSTEM:
   - Add ObstacleSpawner script to a GameObject
   - Add these 4 component scripts to the SAME GameObject:
     * ObstaclePositionCalculator (handles terrain positioning)
     * ObstacleSelector (handles obstacle type selection)
     * ObstacleCleanupManager (handles cleanup and clumping)
     * ObstacleConfigurator (handles obstacle setup)
   - Configure each component's settings
   - Assign all prefabs to ObstacleSelector

4. COLLISION SETUP:
   - Ensure Player has 'Player' tag
   - Set up collision layers if needed
   - Test collision detection

5. TESTING:
   - Run the game and verify obstacles spawn
   - Check collision detection works
   - Adjust spawn rates and probabilities as needed
";

    [Header("Validation")]
    [SerializeField] private ObstaclePositionCalculator positionCalculator;
    [SerializeField] private ObstacleSelector obstacleSelector;
    [SerializeField] private ObstacleCleanupManager cleanupManager;
    [SerializeField] private ObstacleConfigurator obstacleConfigurator;
    
    void Start()
    {
        ValidateSetup();
    }
    
    void ValidateSetup()
    {
        Debug.Log("=== OBSTACLE SETUP VALIDATION (MODULAR SYSTEM) ===");
        
        // Check component assignments
        if (positionCalculator == null)
            Debug.LogError("❌ ObstaclePositionCalculator not assigned!");
        else
            Debug.Log("✅ ObstaclePositionCalculator assigned");
            
        if (obstacleSelector == null)
            Debug.LogError("❌ ObstacleSelector not assigned!");
        else
        {
            Debug.Log("✅ ObstacleSelector assigned");
            
            // Check prefab assignments
            if (obstacleSelector.trafficConePrefab == null)
                Debug.LogWarning("⚠️ Traffic Cone prefab not assigned");
            else
                Debug.Log("✅ Traffic Cone prefab assigned");
                
            if (obstacleSelector.carPrefab == null)
                Debug.LogWarning("⚠️ Car prefab not assigned");
            else
                Debug.Log("✅ Car prefab assigned");
                
            if (obstacleSelector.pedestrianPrefab == null)
                Debug.LogWarning("⚠️ Pedestrian prefab not assigned");
            else
                Debug.Log("✅ Pedestrian prefab assigned");
                
            if (obstacleSelector.meteoritePrefab == null)
                Debug.LogWarning("⚠️ Meteorite prefab not assigned");
            else
                Debug.Log("✅ Meteorite prefab assigned");
            
            // Check probabilities
            float totalChance = obstacleSelector.trafficConeChance + obstacleSelector.carChance + 
                              obstacleSelector.pedestrianChance + obstacleSelector.meteoriteChance;
            
            if (totalChance <= 0f)
            {
                Debug.LogError("❌ All obstacle chances are 0! No obstacles will spawn!");
            }
            else
            {
                Debug.Log($"✅ Total obstacle chance: {totalChance:P1}");
            }
        }
        
        if (cleanupManager == null)
            Debug.LogError("❌ ObstacleCleanupManager not assigned!");
        else
            Debug.Log("✅ ObstacleCleanupManager assigned");
            
        if (obstacleConfigurator == null)
            Debug.LogError("❌ ObstacleConfigurator not assigned!");
        else
            Debug.Log("✅ ObstacleConfigurator assigned");
        
        Debug.Log("=== VALIDATION COMPLETE ===");
    }
    
    [ContextMenu("Run Validation")]
    void RunValidation()
    {
        ValidateSetup();
    }
}
