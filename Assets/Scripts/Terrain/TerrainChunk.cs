using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(RoadGenerator))]
public class TerrainChunk : MonoBehaviour
{
    Mesh mesh;
    PolygonCollider2D poly;
    RoadGenerator roadGenerator;

    // parameters set per-chunk
    public int pointsPerChunk = 50;
    public float xSpacing = 0.5f;
    public float amplitude = 1.5f;
    public float frequency = 0.8f;
    public float bottomDepth = 8f;
    public int seed = 0;
    [Tooltip("How many leading vertices to blend with previous chunk height for a smoother seam")]
    public int seamBlendPoints = 4;
    [Tooltip("Optional low-friction material to reduce sticking on seams")]
    public PhysicsMaterial2D physicsMaterial;

    void Awake()
    {
        EnsureComponents();
    }

    void EnsureComponents()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null)
        {
            mesh = new Mesh();
            mf.sharedMesh = mesh;
        }
        else
        {
            mesh = mf.sharedMesh;
        }

        if (poly == null) poly = GetComponent<PolygonCollider2D>();
        if (physicsMaterial != null && poly != null) poly.sharedMaterial = physicsMaterial;
        
        if (roadGenerator == null) roadGenerator = GetComponent<RoadGenerator>();
    }

    // Generate with seam smoothing passed in
    public float Generate(int chunkIndex, float startHeight, bool useSeam, float seamSmooth, int localSeed = 0)
    {
        EnsureComponents();
        int columns = pointsPerChunk;

        // Generate mesh structure
        TerrainMeshGenerator.GenerateMeshData(columns, xSpacing, bottomDepth, 
            out Vector3[] vertices, out Vector2[] uvs, out int[] triangles);

        // Generate terrain heights
        float[] heights = TerrainGenerator.GenerateTerrainHeights(
            chunkIndex, columns, frequency, amplitude, localSeed, 
            startHeight, useSeam, seamSmooth, seamBlendPoints);

        // Apply heights to vertices
        TerrainGenerator.ApplyTerrainHeights(vertices, heights, columns);

        // Apply mesh data
        TerrainMeshGenerator.ApplyMeshData(mesh, vertices, uvs, triangles);

        // Generate and apply collider
        Vector2[] polyPath = TerrainMeshGenerator.GenerateColliderPath(vertices, columns);
        if (poly != null)
        {
            poly.pathCount = 1;
            poly.SetPath(0, polyPath);
            poly.CreateMesh(false, false);
        }

        // Generate road overlay
        Vector3[] topVerts = new Vector3[columns];
        for (int i = 0; i < columns; i++)
        {
            topVerts[i] = vertices[i];
        }
        if (roadGenerator != null)
        {
            roadGenerator.GenerateRoad(topVerts, columns);
        }

        return heights[columns - 1];
    }

    // Generate variant that enforces end seam continuity (used when inserting chunk on the left)
    public float GenerateBackward(int chunkIndex, float endHeight, float seamSmooth, int localSeed = 0)
    {
        EnsureComponents();
        int columns = pointsPerChunk;

        // Generate mesh structure
        TerrainMeshGenerator.GenerateMeshData(columns, xSpacing, bottomDepth, 
            out Vector3[] vertices, out Vector2[] uvs, out int[] triangles);

        // Generate terrain heights (backward variant)
        float[] heights = TerrainGenerator.GenerateTerrainHeightsBackward(
            chunkIndex, columns, frequency, amplitude, localSeed, 
            endHeight, seamSmooth, seamBlendPoints);

        // Apply heights to vertices
        TerrainGenerator.ApplyTerrainHeights(vertices, heights, columns);

        // Apply mesh data
        TerrainMeshGenerator.ApplyMeshData(mesh, vertices, uvs, triangles);

        // Generate and apply collider
        Vector2[] polyPath = TerrainMeshGenerator.GenerateColliderPath(vertices, columns);
        if (poly != null)
        {
            poly.pathCount = 1;
            poly.SetPath(0, polyPath);
            poly.CreateMesh(false, false);
        }

        // Generate road overlay
        Vector3[] topVerts = new Vector3[columns];
        for (int i = 0; i < columns; i++)
        {
            topVerts[i] = vertices[i];
        }
        if (roadGenerator != null)
        {
            roadGenerator.GenerateRoad(topVerts, columns);
        }

        return heights[columns - 1];
    }

    public float GetFirstTopHeight()
    {
        EnsureComponents();
        var v = mesh != null ? mesh.vertices : null;
        return v != null && v.Length > 0 ? v[0].y : 0f;
    }

    public float GetLastTopHeight()
    {
        int columns = pointsPerChunk;
        EnsureComponents();
        var v = mesh != null ? mesh.vertices : null;
        return v != null && v.Length >= columns ? v[columns - 1].y : 0f;
    }
}
