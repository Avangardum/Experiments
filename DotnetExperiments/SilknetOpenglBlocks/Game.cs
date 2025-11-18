using Silk.NET.Input;
using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Game
{
    private Dictionary<Vector3D<int>, Chunk> _chunks = [];

    public Chunk GetChunk(Vector3D<int> index)
    {
        if (_chunks.TryGetValue(index, out Chunk? chunk)) return chunk;
        chunk = GenerateChunk(index);
        _chunks[index] = chunk;
        return chunk;
    }

    private Chunk GenerateChunk(Vector3D<int> index)
    {
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
        
        return chunk;
    }
    
    private int[,] GetSurfaceHeights(Chunk chunk)
    {
        FastNoiseLite noise = new();
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

    public Block BlockAt(int x, int y, int z) => BlockAt(new Vector3D<int>(x, y, z));

    public Block BlockAt(Vector3D<int> worldPos)
    {
        Vector3D<int> chunkIndex = Chunk.WorldPosToChunkIndex(worldPos);
        Vector3D<int> posInChunk = Chunk.WorldPosToChunkPos(worldPos);
        if (posInChunk.X < 0) posInChunk.X += Chunk.Size;
        if (posInChunk.Y < 0) posInChunk.Y += Chunk.Size;
        if (posInChunk.Z < 0) posInChunk.Z += Chunk.Size;
        return GetChunk(chunkIndex)[posInChunk];
    }
}