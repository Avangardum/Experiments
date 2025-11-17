using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Chunk(Vector3D<int> index)
{
    public Vector3D<int> Index => index;
    
    public const int Size = 64;
    public const int Volume = Size * Size * Size;
    
    private Block[,,] _blocks = new Block[Size, Size, Size];
    
    public Block this[int x, int y, int z]
    {
        get => _blocks[x, y, z];
        set => _blocks[x, y, z] = value;
    }
    
    public Block this[Vector3D<int> pos]
    {
        get => _blocks[pos.X, pos.Y, pos.Z];
        set => _blocks[pos.X, pos.Y, pos.Z] = value;
    }
    
    public void ForEachBlock(Action<Block, Vector3D<int>> func)
    {
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        for (int z = 0; z < Size; z++)
        {
            func(_blocks[x, y, z], new Vector3D<int>(x, y, z));
        }
    }
    
    public void ForEachVisibleBlock(Action<Block, Vector3D<int>> func)
    {
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        for (int z = 0; z < Size; z++)
        {
            Block block = _blocks[x, y, z];
            if (!block.IsVisible()) continue;
            func(block, new Vector3D<int>(x, y, z));
        }
    }
    
    public static Vector3D<int> WorldPosToChunkIndex(Vector3D<int> worldPos)
    {
        Vector3D<int> index = worldPos / Size;
        if (worldPos.X < 0) index.X--;
        if (worldPos.Y < 0) index.Y--;
        if (worldPos.Z < 0) index.Z--;
        return index;
    }
    
    public static Vector3D<int> WorldPosToChunkIndex(Vector3D<float> worldPos) =>
        WorldPosToChunkIndex(worldPos.As<int>());
    
    public static Vector3D<int> WorldPosToChunkPos(Vector3D<int> worldPos)
    {
        Vector3D<int> chunkPos = worldPos.Remainder(Size);
        if (chunkPos.X < 0) chunkPos.X += Size;
        if (chunkPos.Y < 0) chunkPos.Y += Size;
        if (chunkPos.Z < 0) chunkPos.Z += Size;
        return chunkPos;
    }
}