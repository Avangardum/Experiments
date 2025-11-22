using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class WorldGenerator
{
    public const int Seed = 12082036;

    public Chunk GenerateChunk(Vector3D<int> index)
    {
        int chunkSeed = Seed + index.X * 42 + index.Y * 322 + index.Z;
        Random random = new(chunkSeed);
        Chunk chunk = new(index);
        int[,] surfaceHeights = GetSurfaceHeights(chunk); 
        
        chunk.ForEachChunkPos((Vector3D<int> chunkPos) =>
        {
            Vector3D<int> worldPos = chunk.ChunkPosToWorldPos(chunkPos);
            chunk[chunkPos] = (worldPos.Y - surfaceHeights[chunkPos.X, chunkPos.Z]) switch
            {
                > 0 => Block.Air,
                0 => Block.Dirt,
                < 0 => Block.Stone
            };
        });
        
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