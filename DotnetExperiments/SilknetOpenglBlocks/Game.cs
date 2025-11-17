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
        
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        for (int z = 0; z < Chunk.Size; z++)
        {
            int worldY = index.Y * Chunk.Size + y;
            chunk[x, y, z] = worldY switch
            {
                < 16 => Block.Stone,
                16 => Block.Dirt,
                _ => Block.Air
            };
        }
        
        for (int y = 0; y < Chunk.Size; y++)
        {
            chunk[10, y, 10] = Block.Wood;
        }
        
        return chunk;
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