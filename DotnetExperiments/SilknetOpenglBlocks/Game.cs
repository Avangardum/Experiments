using Silk.NET.Input;
using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Game(WorldGenerator worldGenerator)
{
    private Dictionary<Vector3D<int>, Chunk> _chunks = [];

    public Chunk GetChunk(Vector3D<int> index)
    {
        if (_chunks.TryGetValue(index, out Chunk? chunk)) return chunk;
        chunk = worldGenerator.GenerateChunk(index);
        _chunks[index] = chunk;
        return chunk;
    }

    public Block BlockAt(int x, int y, int z) => BlockAt(new Vector3D<int>(x, y, z));

    public Block BlockAt(Vector3D<int> worldPos)
    {
        Vector3D<int> chunkIndex = Chunk.WorldPosToChunkIndex(worldPos);
        Vector3D<int> chunkPos = Chunk.WorldPosToChunkPos(worldPos);
        return GetChunk(chunkIndex)[chunkPos];
    }
}