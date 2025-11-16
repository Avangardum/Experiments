using Silk.NET.Input;
using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Game
{
    public Chunk Chunk { get; } = new();
    private IInputContext _input = null!;
    
    public Game()
    {
        InitChunk();
    }

    private void InitChunk()
    {
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        for (int z = 0; z < Chunk.Size; z++)
        {
            Chunk[x, y, z] = y switch
            {
                < 16 => Block.Stone,
                16 => Block.Dirt,
                _ => Block.Air
            };
        }
        
        for (int y = 0; y < Chunk.Size; y++)
        {
            Chunk[10, y, 10] = Block.Wood;
        }
    }

    public Block BlockAt(int x, int y, int z)
    {
        if (x is < 0 or >= Chunk.Size) return Block.Air;
        if (y is < 0 or >= Chunk.Size) return Block.Air;
        if (z is < 0 or >= Chunk.Size) return Block.Air;
        return Chunk[x, y, z];
    }

    public Block BlockAt(Vector3D<int> position) => BlockAt(position.X, position.Y, position.Z);
}