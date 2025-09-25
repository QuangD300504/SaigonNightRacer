using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
public class TerrainChunk : MonoBehaviour
{
    Mesh mesh;
    PolygonCollider2D poly;

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
    }

    // Generate with seam smoothing passed in
    public float Generate(int chunkIndex, float startHeight, bool useSeam, float seamSmooth, int localSeed = 0)
    {
        EnsureComponents();
        System.Random rnd = new System.Random(localSeed + chunkIndex);

        int columns = pointsPerChunk;
        Vector3[] verts = new Vector3[columns * 2];
        Vector2[] uvs = new Vector2[columns * 2];
        int[] tris = new int[(columns - 1) * 6];

        float lastY = 0f;
        for (int i = 0; i < columns; i++)
        {
            float x = i * xSpacing;
            float n = Mathf.PerlinNoise((chunkIndex * columns + i) * frequency * 0.1f + seed * 0.01f, 0f);
            float y = (n - 0.5f) * 2f * amplitude;

            // Enforce perfect continuity at the seam, then blend for a few vertices
            if (useSeam)
            {
                if (i == 0)
                {
                    y = startHeight; // exact continuity
                }
                else if (i < seamBlendPoints)
                {
                    float t = 1f - (i / Mathf.Max(1f, (float)seamBlendPoints));
                    y = Mathf.Lerp(y, startHeight, seamSmooth * t);
                }
            }

            verts[i] = new Vector3(x, y, 0f);
            uvs[i] = new Vector2(i / (float)(columns - 1), 1f);
            lastY = y;
        }

        // bottom verts
        for (int i = columns - 1; i >= 0; i--)
        {
            int idx = columns + (columns - 1 - i);
            float x = i * xSpacing;
            float y = -bottomDepth;
            verts[idx] = new Vector3(x, y, 0f);
            uvs[idx] = new Vector2(i / (float)(columns - 1), 0f);
        }

        // triangles
        int triIdx = 0;
        for (int i = 0; i < columns - 1; i++)
        {
            int topLeft = i;
            int topRight = i + 1;
            int bottomLeft = 2 * columns - 1 - i;
            int bottomRight = bottomLeft - 1;

            tris[triIdx++] = topLeft;
            tris[triIdx++] = topRight;
            tris[triIdx++] = bottomLeft;

            tris[triIdx++] = topRight;
            tris[triIdx++] = bottomRight;
            tris[triIdx++] = bottomLeft;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // collider path
        Vector2[] polyPath = new Vector2[columns * 2];
        for (int i = 0; i < columns; i++) polyPath[i] = new Vector2(verts[i].x, verts[i].y);
        for (int i = 0; i < columns; i++) polyPath[columns + i] = new Vector2(verts[2 * columns - 1 - i].x, verts[2 * columns - 1 - i].y);

        if (poly != null)
        {
            poly.pathCount = 1;
            poly.SetPath(0, polyPath);
            poly.CreateMesh(false, false);
        }

        return lastY;
    }

    // Generate variant that enforces end seam continuity (used when inserting chunk on the left)
    public float GenerateBackward(int chunkIndex, float endHeight, float seamSmooth, int localSeed = 0)
    {
        EnsureComponents();
        int columns = pointsPerChunk;
        Vector3[] verts = new Vector3[columns * 2];
        Vector2[] uvs = new Vector2[columns * 2];
        int[] tris = new int[(columns - 1) * 6];

        for (int i = 0; i < columns; i++)
        {
            float x = i * xSpacing;
            float n = Mathf.PerlinNoise((chunkIndex * columns + i) * frequency * 0.1f + seed * 0.01f, 0f);
            float y = (n - 0.5f) * 2f * amplitude;

            // blend last vertices towards target endHeight
            int lastIndex = columns - 1;
            if (i == lastIndex)
            {
                y = endHeight; // exact at end
            }
            else if (i > lastIndex - seamBlendPoints)
            {
                float t = (i - (lastIndex - seamBlendPoints)) / Mathf.Max(1f, (float)seamBlendPoints);
                y = Mathf.Lerp(y, endHeight, seamSmooth * t);
            }

            verts[i] = new Vector3(x, y, 0f);
            uvs[i] = new Vector2(i / (float)(columns - 1), 1f);
        }

        // bottom verts
        for (int i = columns - 1; i >= 0; i--)
        {
            int idx = columns + (columns - 1 - i);
            float x = i * xSpacing;
            float y = -bottomDepth;
            verts[idx] = new Vector3(x, y, 0f);
            uvs[idx] = new Vector2(i / (float)(columns - 1), 0f);
        }

        // triangles
        int triIdx = 0;
        for (int i = 0; i < columns - 1; i++)
        {
            int topLeft = i;
            int topRight = i + 1;
            int bottomLeft = 2 * columns - 1 - i;
            int bottomRight = bottomLeft - 1;

            tris[triIdx++] = topLeft;
            tris[triIdx++] = topRight;
            tris[triIdx++] = bottomLeft;

            tris[triIdx++] = topRight;
            tris[triIdx++] = bottomRight;
            tris[triIdx++] = bottomLeft;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // collider path
        Vector2[] polyPath = new Vector2[columns * 2];
        for (int i = 0; i < columns; i++) polyPath[i] = new Vector2(verts[i].x, verts[i].y);
        for (int i = 0; i < columns; i++) polyPath[columns + i] = new Vector2(verts[2 * columns - 1 - i].x, verts[2 * columns - 1 - i].y);

        if (poly != null)
        {
            poly.pathCount = 1;
            poly.SetPath(0, polyPath);
            poly.CreateMesh(false, false);
        }

        return verts[columns - 1].y;
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
