using UnityEngine;

/// <summary>
/// Handles configuration of individual obstacles after spawning
/// Separated from ObstacleSpawner for better organization
/// </summary>
public class ObstacleConfigurator : MonoBehaviour
{
    /// <summary>
    /// Configure obstacle based on its type
    /// </summary>
    public void ConfigureObstacle(GameObject obstacle)
    {
        if (obstacle == null) return;
        
        // Get speed multiplier from spawner
        float speedMultiplier = GetSpeedMultiplier();
        
        // Configure based on obstacle type
        if (obstacle.GetComponent<Car>() != null)
        {
            ConfigureCar(obstacle, speedMultiplier);
        }
        else if (obstacle.GetComponent<MeteoriteObstacle>() != null)
        {
            ConfigureMeteorite(obstacle, speedMultiplier);
        }
        // Traffic cones don't need special configuration
    }
    
    /// <summary>
    /// Get current speed multiplier from spawner
    /// </summary>
    private float GetSpeedMultiplier()
    {
        var spawner = FindFirstObjectByType<ObstacleSpawnerNew>();
        return spawner != null ? spawner.GetCurrentSpeedMultiplier() : 1f;
    }
    
    /// <summary>
    /// Configure car obstacle
    /// </summary>
    private void ConfigureCar(GameObject obstacle, float speedMultiplier)
    {
        var car = obstacle.GetComponent<Car>();
        if (car != null)
        {
            float originalSpeed = car.carSpeed;
            car.carSpeed *= speedMultiplier;
            Debug.Log($"Car configured - Speed: {originalSpeed:F1} → {car.carSpeed:F1} ({speedMultiplier:F1}x multiplier)");
        }
    }
    
    /// <summary>
    /// Configure meteorite obstacle
    /// </summary>
    private void ConfigureMeteorite(GameObject obstacle, float speedMultiplier)
    {
        var meteorite = obstacle.GetComponent<MeteoriteObstacle>();
        if (meteorite != null)
        {
            float originalSpeed = meteorite.fallSpeed;
            meteorite.fallSpeed *= speedMultiplier;
            Debug.Log($"Meteorite configured - Fall Speed: {originalSpeed:F1} → {meteorite.fallSpeed:F1} ({speedMultiplier:F1}x multiplier)");
        }
    }
    
    
    
    /// <summary>
    /// Setup physics for obstacle (Rigidbody2D only)
    /// </summary>
    public void SetupObstaclePhysics(GameObject obstacle, bool isStatic = false)
    {
        if (obstacle == null) return;
        
        // Obstacles handle their own movement - no external movement components needed
    }
}
