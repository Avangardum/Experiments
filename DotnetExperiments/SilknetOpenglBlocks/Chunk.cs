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
    
    public Matrix4X4<float> ModelMatrix => Matrix4X4.CreateTranslation(index.As<float>() * Size);

    public static Vector3D<int> PosToChunkIndex(Vector3D<float> position)
    {
        Vector3D<int> index = position.As<int>() / Size;
        if (position.X < 0) index.X--;
        if (position.Y < 0) index.Y--;
        if (position.Z < 0) index.Z--;
        return index;
    }
}