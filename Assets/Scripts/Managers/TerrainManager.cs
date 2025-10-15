using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class TerrainManager : MonoBehaviour
{
    [Header("Chunk Settings")]
    public TerrainChunk chunkPrefab;
    public int numChunks = 5;
    public float seamSmooth = 0.25f;
    public int seed = 0;

    [Header("Placement")]
    public Transform chunkParent;
    public Transform player;

    private Queue<TerrainChunk> activeChunks = new Queue<TerrainChunk>();
    private float lastHeight = 0f;
    private float chunkWidth;
    private int generatedChunkCount = 0; // for global offset continuity
    private float rightmostX = 0f; // world-space x of last chunk
    public float recyclePadding = 0f; // extra distance past the chunk end before recycle

    void Start()
    {
        if (chunkParent == null) chunkParent = transform;
        if (player == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) player = playerGo.transform;
        }
        InitializeChunks();
    }

    void InitializeChunks()
    {
        // clear any children
        foreach (Transform child in chunkParent)
        {
            if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject);
        }
        activeChunks.Clear();
        lastHeight = 0f;
        generatedChunkCount = 0;

        // create initial chunks
        for (int i = 0; i < numChunks; i++)
        {
            TerrainChunk chunk = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity, chunkParent);
            if (i == 0) chunkWidth = (chunk.pointsPerChunk - 1) * chunk.xSpacing;
            chunk.transform.localPosition = new Vector3(i * chunkWidth, 0f, 0f);

            lastHeight = chunk.Generate(
                generatedChunkCount,   // global index for continuity
                lastHeight,
                i > 0,
                seamSmooth,
                seed
            );
            generatedChunkCount++;

            activeChunks.Enqueue(chunk);
        }
        // compute rightmost world x after init
        TerrainChunk last = null;
        foreach (var c in activeChunks) last = c;
        if (last != null) rightmostX = last.transform.position.x;
    }

    [ContextMenu("Regenerate Terrain")]
    void RegenerateTerrainContextMenu()
    {
        if (chunkParent == null) chunkParent = transform;
        InitializeChunks();
    }

    [ContextMenu("Ungenerate Terrain (Clear)")]
    public void UngenerateTerrain()
    {
        if (chunkParent == null) chunkParent = transform;
        foreach (Transform child in chunkParent)
        {
            if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject);
        }
        activeChunks.Clear();
        lastHeight = 0f;
        generatedChunkCount = 0;
        rightmostX = 0f;
    }

    void Update()
    {
        if (player == null || activeChunks.Count == 0) return;

        // Recycle only when player has passed chunk[1] (keep chunk[0] behind as a buffer)
        while (activeChunks.Count >= 2)
        {
            TerrainChunk[] arr = activeChunks.ToArray();
            TerrainChunk first = arr[0];
            TerrainChunk second = arr[1];
            float triggerX = second.transform.position.x + chunkWidth + recyclePadding;
            if (player.position.x > triggerX)
            {
                RecycleChunk();
            }
            else break;
        }

        // Backward recycle: when player steps back into the previous chunk (chunk[0]),
        // move the farthest-right chunk to the left to keep a buffer behind.
        while (activeChunks.Count >= 2)
        {
            TerrainChunk first = activeChunks.Peek();
            float firstEndX = first.transform.position.x + chunkWidth - recyclePadding; // near the right edge of chunk[0]
            if (player.position.x < firstEndX)
            {
                RecycleChunkBackward();
            }
            else break;
        }
    }

    void RecycleChunk()
    {
        TerrainChunk oldChunk = activeChunks.Dequeue();
        // place after current rightmost in world space
        float newWorldX = rightmostX + chunkWidth;
        Vector3 local = oldChunk.transform.localPosition;
        // convert desired world x to local x
        float parentWorldX = oldChunk.transform.parent != null ? oldChunk.transform.parent.position.x : 0f;
        float newLocalX = newWorldX - parentWorldX;
        oldChunk.transform.localPosition = new Vector3(newLocalX, 0f, 0f);

        lastHeight = oldChunk.Generate(
            generatedChunkCount,
            lastHeight,
            true,
            seamSmooth,
            seed
        );
        generatedChunkCount++;

        activeChunks.Enqueue(oldChunk);
        rightmostX = oldChunk.transform.position.x; // update rightmost end
    }

    void RecycleChunkBackward()
    {
        // take last chunk and move it before the first
        TerrainChunk[] arr = activeChunks.ToArray();
        TerrainChunk last = arr[arr.Length - 1];
        // rebuild queue without the last element
        var list = new List<TerrainChunk>(arr);
        list.RemoveAt(list.Count - 1);
        activeChunks = new Queue<TerrainChunk>(list);

        TerrainChunk first = activeChunks.Peek();
        float newWorldX = first.transform.position.x - chunkWidth;
        float parentWorldX = last.transform.parent != null ? last.transform.parent.position.x : 0f;
        float newLocalX = newWorldX - parentWorldX;
        last.transform.localPosition = new Vector3(newLocalX, 0f, 0f);

        // generate ensuring continuity at the end of this moved chunk with the start of first
        float targetEnd = first.GetFirstTopHeight();
        last.GenerateBackward(generatedChunkCount, targetEnd, seamSmooth, seed);
        generatedChunkCount++;

        // push moved chunk to front of queue
        list = new List<TerrainChunk>(activeChunks);
        list.Insert(0, last);
        activeChunks = new Queue<TerrainChunk>(list);

        // update rightmostX and lastHeight based on new tail chunk
        TerrainChunk tail = null;
        foreach (var c in activeChunks) tail = c;
        if (tail != null)
        {
            rightmostX = tail.transform.position.x;
            lastHeight = tail.GetLastTopHeight();
        }
    }
}
