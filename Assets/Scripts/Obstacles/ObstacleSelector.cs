using UnityEngine;

/// <summary>
/// Handles obstacle type selection based on probabilities
/// Separated from ObstacleSpawner for better organization
/// </summary>
public class ObstacleSelector : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    [Tooltip("Traffic cone prefab")]
    public GameObject trafficConePrefab;
    
    [Tooltip("Car prefab")]
    public GameObject carPrefab;
    
    [Tooltip("Pedestrian prefab")]
    public GameObject pedestrianPrefab;
    
    [Tooltip("Meteorite prefab")]
    public GameObject meteoritePrefab;

    [Header("Obstacle Probabilities")]
    [Tooltip("Probability of spawning traffic cone (0-1)")]
    [Range(0f, 1f)]
    public float trafficConeChance = 0.4f;
    
    [Tooltip("Probability of spawning car (0-1)")]
    [Range(0f, 1f)]
    public float carChance = 0.2f;
    
    [Tooltip("Probability of spawning pedestrian (0-1)")]
    [Range(0f, 1f)]
    public float pedestrianChance = 0.2f;
    
    [Tooltip("Probability of spawning meteorite (0-1)")]
    [Range(0f, 1f)]
    public float meteoriteChance = 0.2f;

    /// <summary>
    /// Choose obstacle type based on probabilities
    /// </summary>
    public GameObject ChooseObstacleType()
    {
        float random = Random.Range(0f, 1f);
        float cumulative = 0f;
        
        // Normalize probabilities to ensure they add up to 1
        float totalChance = trafficConeChance + carChance + pedestrianChance + meteoriteChance;
        float normalizedTrafficCone = trafficConeChance / totalChance;
        float normalizedCar = carChance / totalChance;
        float normalizedPedestrian = pedestrianChance / totalChance;
        float normalizedMeteorite = meteoriteChance / totalChance;
        
        cumulative += normalizedTrafficCone;
        if (random < cumulative)
        {
            return trafficConePrefab;
        }
        
        cumulative += normalizedCar;
        if (random < cumulative)
        {
            return carPrefab;
        }
        
        cumulative += normalizedPedestrian;
        if (random < cumulative)
        {
            return pedestrianPrefab;
        }
        
        cumulative += normalizedMeteorite;
        if (random < cumulative)
        {
            return meteoritePrefab;
        }
        
        // Fallback to traffic cone
        return trafficConePrefab;
    }
    
    /// <summary>
    /// Check if obstacle type is a meteorite
    /// </summary>
    public bool IsMeteorite(GameObject obstaclePrefab)
    {
        return obstaclePrefab != null && obstaclePrefab.GetComponent<MeteoriteObstacle>() != null;
    }
    
    /// <summary>
    /// Check if obstacle type is a traffic cone
    /// </summary>
    public bool IsTrafficCone(GameObject obstaclePrefab)
    {
        return obstaclePrefab != null && obstaclePrefab.GetComponent<TrafficCone>() != null;
    }
}
