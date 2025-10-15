using UnityEngine;

/// <summary>
/// Handles terrain generation logic using Perlin noise
/// Separated from TerrainChunk to focus on terrain algorithms
/// </summary>
public class TerrainGenerator
{
    /// <summary>
    /// Generates terrain heights using Perlin noise with seam blending
    /// </summary>
    public static float[] GenerateTerrainHeights(int chunkIndex, int columns, float frequency, 
        float amplitude, int seed, float startHeight, bool useSeam, float seamSmooth, int seamBlendPoints)
    {
        float[] heights = new float[columns];
        System.Random rnd = new System.Random(seed + chunkIndex);

        for (int i = 0; i < columns; i++)
        {
            float n = Mathf.PerlinNoise((chunkIndex * columns + i) * frequency * 0.1f + seed * 0.01f, 0f);
            float y = (n - 0.5f) * 2f * amplitude;

            // Apply seam blending
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

            heights[i] = y;
        }

        return heights;
    }

    /// <summary>
    /// Generates terrain heights for backward generation (end seam continuity)
    /// </summary>
    public static float[] GenerateTerrainHeightsBackward(int chunkIndex, int columns, float frequency, 
        float amplitude, int seed, float endHeight, float seamSmooth, int seamBlendPoints)
    {
        float[] heights = new float[columns];
        System.Random rnd = new System.Random(seed + chunkIndex);

        for (int i = 0; i < columns; i++)
        {
            float n = Mathf.PerlinNoise((chunkIndex * columns + i) * frequency * 0.1f + seed * 0.01f, 0f);
            float y = (n - 0.5f) * 2f * amplitude;

            // Apply end seam blending
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

            heights[i] = y;
        }

        return heights;
    }

    /// <summary>
    /// Applies terrain heights to mesh vertices
    /// </summary>
    public static void ApplyTerrainHeights(Vector3[] vertices, float[] heights, int columns)
    {
        for (int i = 0; i < columns; i++)
        {
            vertices[i] = new Vector3(vertices[i].x, heights[i], vertices[i].z);
        }
    }
}
