using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    [Header("Road Settings")]
    [Tooltip("Material for the road overlay mesh")]
    public Material roadMaterial;
    [Tooltip("Height offset above dirt terrain for road")]
    public float roadHeightOffset = 0.05f;
    [Tooltip("Thickness of the visible road strip (also collider depth)")]
    public float roadThickness = 0.3f;
    [Tooltip("Layer for road colliders (should match BikeController groundLayerMask)")]
    public int roadLayer = 0; // Default layer

    private Mesh roadMesh;
    private GameObject roadObject;
    private EdgeCollider2D edgeCollider;

    void Awake()
    {
        EnsureRoad();
    }

    void EnsureRoad()
    {
        roadObject = transform.Find("Road")?.gameObject;
        if (roadObject == null)
        {
            roadObject = new GameObject("Road");
            roadObject.transform.SetParent(transform, false);
            roadObject.layer = roadLayer; 
            roadObject.tag = "Ground"; 

            var mf = roadObject.AddComponent<MeshFilter>();
            var mr = roadObject.AddComponent<MeshRenderer>();
            mr.material = roadMaterial;

            // EdgeCollider for center line only
            edgeCollider = roadObject.AddComponent<EdgeCollider2D>();
            edgeCollider.isTrigger = false;

            roadMesh = new Mesh();
            mf.sharedMesh = roadMesh;
        }
        else
        {
            roadMesh = roadObject.GetComponent<MeshFilter>().sharedMesh;
            edgeCollider = roadObject.GetComponent<EdgeCollider2D>();
        }
    }

    public void GenerateRoad(Vector3[] topVerts, int columns)
    {
        if (roadMaterial == null)
        {
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
            float y = topVerts[i].y + roadHeightOffset;

            // top edge
            roadVerts[i] = new Vector3(x, y, 0f);
            uvs[i] = new Vector2(i / (float)(columns - 1), 1f);

            // bottom edge
            roadVerts[columns + i] = new Vector3(x, y - roadThickness, 0f);
            uvs[columns + i] = new Vector2(i / (float)(columns - 1), 0f);
        }

        // Triangles
        int triIdx = 0;
        for (int i = 0; i < columns - 1; i++)
        {
            int topL = i;
            int topR = i + 1;
            int botL = columns + i;
            int botR = columns + i + 1;

            tris[triIdx++] = topL;
            tris[triIdx++] = topR;
            tris[triIdx++] = botL;

            tris[triIdx++] = topR;
            tris[triIdx++] = botR;
            tris[triIdx++] = botL;
        }

        // Apply mesh
        roadMesh.Clear();
        roadMesh.vertices = roadVerts;
        roadMesh.uv = uvs;
        roadMesh.triangles = tris;
        roadMesh.RecalculateNormals();

        // Apply edge collider path (center line only)
        if (edgeCollider != null)
        {
            Vector2[] centerLinePoints = new Vector2[columns];

            // Calculate center line points (middle of the road)
            for (int i = 0; i < columns; i++)
            {
                float centerY = topVerts[i].y + roadHeightOffset - (roadThickness * 0.5f);
                centerLinePoints[i] = new Vector2(topVerts[i].x, centerY);
            }

            edgeCollider.points = centerLinePoints;
        }
    }
}
