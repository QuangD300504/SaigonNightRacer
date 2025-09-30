using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple object pool for performance optimization
/// Reuses GameObjects instead of constantly creating/destroying them
/// </summary>
public class SimplePool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("Prefab to pool")]
    public GameObject prefab;
    
    [Tooltip("Initial number of objects to create")]
    public int initialSize = 10;
    
    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        // Pre-create objects for the pool
        for (int i = 0; i < initialSize; i++)
        {
            var obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// Get an object from the pool
    /// </summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj;
        
        if (pool.Count > 0)
        {
            // Reuse existing object
            obj = pool.Dequeue();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
            }
            else
            {
                // Object was destroyed, create new one
                obj = Instantiate(prefab, position, rotation);
            }
        }
        else
        {
            // Create new object if pool is empty
            obj = Instantiate(prefab, position, rotation);
        }
        
        return obj;
    }

    /// <summary>
    /// Return an object to the pool
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            pool.Enqueue(obj);
        }
    }
}
