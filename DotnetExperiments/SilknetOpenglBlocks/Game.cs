using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Game(WorldGenerator worldGenerator)
{
    private Dictionary<Vector3D<int>, Chunk> _chunks = [];

    public event Action<Vector3D<int>>? BlockUpdated;
    
    public Chunk GetChunk(Vector3D<int> index)
    {
        if (_chunks.TryGetValue(index, out Chunk? chunk)) return chunk;
        chunk = worldGenerator.GenerateChunk(index);
        _chunks[index] = chunk;
        return chunk;
    }

    public Block GetBlock(Vector3D<int> worldPos)
    {
        Vector3D<int> chunkIndex = Chunk.WorldPosToChunkIndex(worldPos);
        Vector3D<int> chunkPos = Chunk.WorldPosToChunkPos(worldPos);
        return GetChunk(chunkIndex)[chunkPos];
    }

    public Block GetBlock(int x, int y, int z) => GetBlock(new Vector3D<int>(x, y, z));

    public Block GetBlock(float x, float y, float z) => GetBlock(new Vector3D<float>(x, y, z));
    
    public Block GetBlock(Vector3D<float> worldPos) => GetBlock(worldPos.Select(x => (int)MathF.Round(x)));
    
    public void SetBlock(Vector3D<int> worldPos, Block block)
    {
        Vector3D<int> chunkIndex = Chunk.WorldPosToChunkIndex(worldPos);
        Vector3D<int> chunkPos = Chunk.WorldPosToChunkPos(worldPos);
        GetChunk(chunkIndex)[chunkPos] = block;
        BlockUpdated?.Invoke(worldPos);
    }
    
    public void SetBlock(int x, int y, int z, Block block) => SetBlock(new Vector3D<int>(x, y, z), block);

    public void SetBlock(float x, float y, float z, Block block) => SetBlock(new Vector3D<float>(x, y, z), block);
    
    public void SetBlock(Vector3D<float> worldPos, Block block) =>
        SetBlock(worldPos.Select(x => (int)MathF.Round(x)), block);
}