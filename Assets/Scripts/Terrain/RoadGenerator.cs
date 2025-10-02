using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    [Header("Road Settings")]
    [Tooltip("Material for the road overlay mesh")]
    public Material roadMaterial;
    [Tooltip("Height offset above dirt terrain for road")]
    public float roadHeightOffset = 0.05f;
    [Tooltip("Thickness of the road strip")]
    public float roadThickness = 0.3f;
    [Tooltip("Layer for road colliders (should match BikeController groundLayerMask)")]
    public int roadLayer = 0; // Default layer

    private Mesh roadMesh;
    private GameObject roadObject;

    void Awake()
    {
        EnsureRoad();
    }

    /// <summary>
    /// Ensures the road GameObject and components exist
    /// </summary>
    void EnsureRoad()
    {
        roadObject = transform.Find("Road")?.gameObject;
        if (roadObject == null)
        {
            roadObject = new GameObject("Road");
            roadObject.transform.SetParent(transform, false);
            roadObject.layer = roadLayer; // Set to specified layer
            
            var mf = roadObject.AddComponent<MeshFilter>();
            var mr = roadObject.AddComponent<MeshRenderer>();
            mr.material = roadMaterial;
            
            // Add collider for road detection - use EdgeCollider2D for 2D physics
            var ec = roadObject.AddComponent<EdgeCollider2D>();
            ec.isTrigger = false; // Solid collision
            
            roadMesh = new Mesh();
            mf.sharedMesh = roadMesh;
        }
        else
        {
            roadMesh = roadObject.GetComponent<MeshFilter>().sharedMesh;
        }
    }

    /// <summary>
    /// Generates a road mesh that follows the terrain shape
    /// </summary>
    /// <param name="topVerts">Top vertices of the terrain</param>
    /// <param name="columns">Number of terrain columns</param>
    public void GenerateRoad(Vector3[] topVerts, int columns)
    {
        if (roadMaterial == null) 
        {
            Debug.LogWarning("Road material not assigned. Skipping road generation.");
            return;
        }
        
        EnsureRoad();

        Vector3[] roadVerts = new Vector3[columns * 2];
        Vector2[] uvs = new Vector2[columns * 2];
        int[] tris = new int[(columns - 1) * 6];

        // Generate road vertices
        for (int i = 0; i < columns; i++)
        {
            float x = topVerts[i].x;
            float y = topVerts[i].y + roadHeightOffset; // slight offset above dirt

            // top edge
            roadVerts[i] = new Vector3(x, y, 0f);
            uvs[i] = new Vector2(i / (float)(columns - 1), 1f);

            // bottom edge (make thin strip)
            roadVerts[columns + i] = new Vector3(x, y - roadThickness, 0f);
            uvs[columns + i] = new Vector2(i / (float)(columns - 1), 0f);
        }

        // Generate triangles
        int triIdx = 0;
        for (int i = 0; i < columns - 1; i++)
        {
            int topL = i;
            int topR = i + 1;
            int botL = columns + i;
            int botR = columns + i + 1;

            // First triangle
            tris[triIdx++] = topL;
            tris[triIdx++] = topR;
            tris[triIdx++] = botL;

            // Second triangle
            tris[triIdx++] = topR;
            tris[triIdx++] = botR;
            tris[triIdx++] = botL;
        }

        // Apply mesh data
        roadMesh.Clear();
        roadMesh.vertices = roadVerts;
        roadMesh.uv = uvs;
        roadMesh.triangles = tris;
        roadMesh.RecalculateNormals();
        
        // Update edge collider for 2D physics
        var edgeCollider = roadObject.GetComponent<EdgeCollider2D>();
        if (edgeCollider != null)
        {
            // Create edge points from the top vertices of the road
            Vector2[] edgePoints = new Vector2[columns];
            for (int i = 0; i < columns; i++)
            {
                edgePoints[i] = new Vector2(roadVerts[i].x, roadVerts[i].y);
            }
            edgeCollider.points = edgePoints;
        }
        else
        {
            // EdgeCollider2D not found - this shouldn't happen
        }
    }

    /// <summary>
    /// Updates the road material
    /// </summary>
    /// <param name="newMaterial">New road material to use</param>
    public void SetRoadMaterial(Material newMaterial)
    {
        roadMaterial = newMaterial;
        if (roadObject != null)
        {
            var mr = roadObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = roadMaterial;
            }
        }
    }

    /// <summary>
    /// Updates road generation parameters
    /// </summary>
    /// <param name="heightOffset">New height offset above terrain</param>
    /// <param name="thickness">New road thickness</param>
    public void UpdateRoadParameters(float heightOffset, float thickness)
    {
        roadHeightOffset = heightOffset;
        roadThickness = thickness;
    }

    /// <summary>
    /// Destroys the road GameObject
    /// </summary>
    public void DestroyRoad()
    {
        if (roadObject != null)
        {
            DestroyImmediate(roadObject);
            roadObject = null;
            roadMesh = null;
        }
    }
}
