using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Chunk
{
    public const int Size = 64;
    public const int Volume = Size * Size * Size;
    
    private Block[,,] _blocks = new Block[Size, Size, Size];
    
    public Block this[int x, int y, int z]
    {
        get => _blocks[x, y, z];
        set => _blocks[x, y, z] = value;
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
}