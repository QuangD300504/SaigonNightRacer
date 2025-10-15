using UnityEngine;

/// <summary>
/// Handles mesh generation operations for terrain chunks
/// Separated from TerrainChunk to reduce complexity
/// </summary>
public class TerrainMeshGenerator
{
    /// <summary>
    /// Generates mesh vertices, triangles, and UVs for terrain
    /// </summary>
    public static void GenerateMeshData(int columns, float xSpacing, float bottomDepth, 
        out Vector3[] vertices, out Vector2[] uvs, out int[] triangles)
    {
        vertices = new Vector3[columns * 2];
        uvs = new Vector2[columns * 2];
        triangles = new int[(columns - 1) * 6];

        // Generate top vertices
        for (int i = 0; i < columns; i++)
        {
            float x = i * xSpacing;
            vertices[i] = new Vector3(x, 0f, 0f); // Y will be set by terrain logic
            uvs[i] = new Vector2(i / (float)(columns - 1), 1f);
        }

        // Generate bottom vertices
        for (int i = columns - 1; i >= 0; i--)
        {
            int idx = columns + (columns - 1 - i);
            float x = i * xSpacing;
            vertices[idx] = new Vector3(x, -bottomDepth, 0f);
            uvs[idx] = new Vector2(i / (float)(columns - 1), 0f);
        }

        // Generate triangles
        int triIdx = 0;
        for (int i = 0; i < columns - 1; i++)
        {
            int topLeft = i;
            int topRight = i + 1;
            int bottomLeft = 2 * columns - 1 - i;
            int bottomRight = bottomLeft - 1;

            triangles[triIdx++] = topLeft;
            triangles[triIdx++] = topRight;
            triangles[triIdx++] = bottomLeft;

            triangles[triIdx++] = topRight;
            triangles[triIdx++] = bottomRight;
            triangles[triIdx++] = bottomLeft;
        }
    }

    /// <summary>
    /// Applies mesh data to a Unity mesh
    /// </summary>
    public static void ApplyMeshData(Mesh mesh, Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Generates collider path from mesh vertices
    /// </summary>
    public static Vector2[] GenerateColliderPath(Vector3[] vertices, int columns)
    {
        Vector2[] polyPath = new Vector2[columns * 2];
        
        // Top vertices
        for (int i = 0; i < columns; i++)
        {
            polyPath[i] = new Vector2(vertices[i].x, vertices[i].y);
        }
        
        // Bottom vertices (reversed)
        for (int i = 0; i < columns; i++)
        {
            polyPath[columns + i] = new Vector2(vertices[2 * columns - 1 - i].x, vertices[2 * columns - 1 - i].y);
        }
        
        return polyPath;
    }
}
