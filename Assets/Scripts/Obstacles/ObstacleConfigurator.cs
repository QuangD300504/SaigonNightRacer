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
        
        // Configure based on obstacle type
        if (obstacle.GetComponent<Car>() != null)
        {
            ConfigureCar(obstacle);
        }
        else if (obstacle.GetComponent<Pedestrian>() != null)
        {
            ConfigurePedestrian(obstacle);
        }
        // Traffic cones and meteorites don't need special configuration
    }
    
    /// <summary>
    /// Configure car obstacle
    /// </summary>
    private void ConfigureCar(GameObject obstacle)
    {
        var car = obstacle.GetComponent<Car>();
        if (car != null)
        {
            // Car automatically moves towards player - no configuration needed
        }
    }
    
    /// <summary>
    /// Configure pedestrian obstacle
    /// </summary>
    private void ConfigurePedestrian(GameObject obstacle)
    {
        var pedestrian = obstacle.GetComponent<Pedestrian>();
        if (pedestrian != null)
        {
            pedestrian.direction = Random.Range(0, 2) == 0 ? -1 : 1;
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
