using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Chunk(Vector3D<int> index)
{
    public Vector3D<int> Index => index;
    public Vector3D<int> Origin => Index * Size;
    
    public const int Size = 16;
    public static readonly int Log2Size = 4;
    public static readonly int WorldPosToChunkPosBitMask = Size - 1;
    public const int Volume = Size * Size * Size;
    public static readonly Vector3D<int> SizeVector = Vector3D<int>.One * Size;
    
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
    
    public void ForEachChunkPos(Action<Vector3D<int>> func) => For.XyzExclusive(Vector3D<int>.Zero, SizeVector, func);
    
    public static Vector3D<int> WorldPosToChunkIndex(Vector3D<int> worldPos) => worldPos.Select(WorldPosToChunkIndex);
    
    public static Vector3D<int> WorldPosToChunkIndex(Vector3D<float> worldPos) => worldPos.Select(WorldPosToChunkIndex);
    
    public static int WorldPosToChunkIndex(int worldPos) => worldPos >> Log2Size;

    public static int WorldPosToChunkIndex(float worldPos) => WorldPosToChunkIndex((int)MathF.Round(worldPos));
    
    public static Vector3D<int> WorldPosToChunkPos(Vector3D<int> worldPos) => worldPos.Select(WorldPosToChunkPos);
    
    public static int WorldPosToChunkPos(int worldPos) => worldPos & WorldPosToChunkPosBitMask;

    public Vector3D<int> ChunkPosToWorldPos(Vector3D<int> chunkPos) => Origin + chunkPos;
    
    public Aabb Aabb
    {
        get
        {
            Vector3D<float> start = Origin.As<float>() - Vector3D<float>.One / 2;
            Vector3D<float> end = start + Vector3D<float>.One * Size;
            return new Aabb(start, end);
        }
    }
}