using System.Collections.Concurrent;
using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class WorldGenerator
{
    private const int Seed = 12082036;
    private static readonly Vector3D<int> MinChunkIndex = new(-30, -5, -30);
    private static readonly Vector3D<int> MaxChunkIndex = new(30, 5, 30);
    
    private AwaitableDictionary<Vector3D<int>, Chunk> _chunkAwaitableDictionary = new();

    public WorldGenerator()
    {
        Task.Run(GenerateWorld);
    }
    
    private void GenerateWorld()
    {
        // The world is generated in indexed boxes of chunks. Box with a given index is a set of chunks with the given
        // distance from the zero chunk (in chunks, with diagonal movement considered 1 step as well), in other words,
        // a chunk belongs to a box with index equal to the highest absolute value of the chunk's index vector.
        // So the ring 0 is just the zero indexed chunk, the ring 1 is all its neighbors (including diagonal),
        // and so on.
        int maxBoxIndex = new [] { MinChunkIndex.X, MinChunkIndex.Y, MinChunkIndex.Z, MaxChunkIndex.X, MaxChunkIndex.Y,
            MaxChunkIndex.Z }.Max(Math.Abs);
        for (int boxIndex = 0; boxIndex <= maxBoxIndex; boxIndex++)
        for (int x = -boxIndex; x <= boxIndex; x++)
        for (int y = -boxIndex; y <= boxIndex; y++)
        for (int z = -boxIndex; z <= boxIndex; z++)
        {
            Vector3D<int> chunkIndex = new(x, y, z);
            int maxAbsIndex = chunkIndex.ToEnumerable().Max(Math.Abs);
            if (maxAbsIndex != boxIndex) continue;
            _chunkAwaitableDictionary[chunkIndex] = GenerateChunk(chunkIndex);
        }
    }
    
    public Chunk GetChunk(Vector3D<int> index)
    {
        if (IsChunkIndexOutOfBounds(index)) return new Chunk(index);
        return _chunkAwaitableDictionary[index];
    }
    
    public bool IsChunkIndexOutOfBounds(Vector3D<int> index)
    {
        return
            index.X < MinChunkIndex.X || index.X > MaxChunkIndex.X ||
            index.Y < MinChunkIndex.Y || index.Y > MaxChunkIndex.Y ||
            index.Z < MinChunkIndex.Z || index.Z > MaxChunkIndex.Z;
    }
    
    public bool IsChunkGenerated(Vector3D<int> index)
    {
        return IsChunkIndexOutOfBounds(index) || _chunkAwaitableDictionary.HasKey(index);
    }

    private Chunk GenerateChunk(Vector3D<int> index)
    {
        int chunkSeed = Seed + index.X * 42 + index.Y * 322 + index.Z;
        Random random = new(chunkSeed);
        Chunk chunk = new(index);
        int[,] surfaceHeights = GetSurfaceHeights(chunk); 
        
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        for (int z = 0; z < Chunk.Size; z++)
        {
            Vector3D<int> chunkPos = new(x, y, z);
            Vector3D<int> worldPos = chunk.ChunkPosToWorldPos(chunkPos);
            chunk[chunkPos] = (worldPos.Y - surfaceHeights[chunkPos.X, chunkPos.Z]) switch
            {
                > 0 => Block.Air,
                0 => Block.Dirt,
                < 0 => Block.Stone
            };
        }
        
        MaybeGenerateTree(chunk, random, surfaceHeights);
        
        return chunk;
    }
    
    private void MaybeGenerateTree(Chunk chunk, Random random, int[,] surfaceHeights)
    {
        const int treeHeight = 9;
        bool shouldTry = random.NextSingle() < 0.5f;
        if (!shouldTry) return;
        int treeX = random.Next(5, 11);
        int treeZ = random.Next(5, 11);
        int treeMinWorldY = surfaceHeights[treeX, treeZ] + 1;
        int treeMaxWorldY = treeMinWorldY + treeHeight - 1;
        int treeMinChunkY = Chunk.WorldPosToChunkPos(treeMinWorldY);
        int treeMaxChunkY = Chunk.WorldPosToChunkPos(treeMaxWorldY);
        int treeBottomChunkYIndex = Chunk.WorldPosToChunkIndex(treeMinWorldY);
        int treeTopChunkYIndex = Chunk.WorldPosToChunkIndex(treeMaxWorldY);
        if (treeBottomChunkYIndex != chunk.Index.Y || treeTopChunkYIndex != chunk.Index.Y ) return;
        const int trunkBlocksWithoutLeafs = 4;
        
        for (int x = treeX - 2; x <= treeX + 2; x++)
        for (int y = treeMinChunkY + trunkBlocksWithoutLeafs; y <= treeMaxChunkY; y++)
        for (int z = treeZ - 2; z <= treeZ + 2; z++)
        {
            chunk[x, y, z] = Block.Leafs;
        }
        
        for (int y = treeMinChunkY; y < treeMaxChunkY; y++)
        {
            chunk[treeX, y, treeZ] = Block.Wood;
        }
    }
    
    private int[,] GetSurfaceHeights(Chunk chunk)
    {
        FastNoiseLite noise = new(Seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        int[,] heights = new int[Chunk.Size, Chunk.Size];
        for (int xChunkPos = 0; xChunkPos < Chunk.Size; xChunkPos++)
        for (int zChunkPos = 0; zChunkPos < Chunk.Size; zChunkPos++)
        {
            int xWorldPos = chunk.Index.X * Chunk.Size + xChunkPos;
            int zWorldPos = chunk.Index.Z * Chunk.Size + zChunkPos;
            const int minHeight = 0;
            const int maxHeight = 63;
            float rawNoiseValue = noise.GetNoise(xWorldPos, zWorldPos);
            heights[xChunkPos, zChunkPos] =
                RangeConverter.ConvertFromFloatRangeToInclusiveIntRange(rawNoiseValue, -1f, 1f, minHeight, maxHeight);
        }
        return heights;
    }
}