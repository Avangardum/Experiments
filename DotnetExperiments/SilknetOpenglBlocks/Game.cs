using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace SilknetOpenglBlocks;

public sealed class Game
{
    private readonly IWindow _window;
    private readonly Camera _camera;
    private readonly Block[,,] _chunk = new Block[ChunkSize, ChunkSize, ChunkSize];
    private IInputContext _input = null!;
    
    public const int ChunkSize = 64;

    public Game(IWindow window, Camera camera)
    {
        _window = window;
        _camera = camera;
        
        window.Load += OnLoad;
    }
    
    private void OnLoad()
    {
        InitChunk();
    }
    
    public void InitChunk()
    {
        for (int x = 0; x < ChunkSize; x++)
        for (int y = 0; y < ChunkSize; y++)
        for (int z = 0; z < ChunkSize; z++)
        {
            _chunk[x, y, z] = y switch
            {
                < 16 => Block.Stone,
                16 => Block.Dirt,
                _ => Block.Air
            };
        }
        
        for (int y = 0; y < ChunkSize; y++)
        {
            _chunk[10, y, 10] = Block.Wood;
        }
    }

    public Block BlockAt(int x, int y, int z)
    {
        if (x is < 0 or >= ChunkSize) return Block.Air;
        if (y is < 0 or >= ChunkSize) return Block.Air;
        if (z is < 0 or >= ChunkSize) return Block.Air;
        return _chunk[x, y, z];
    }

    public Block BlockAt(Vector3D<int> position) => BlockAt(position.X, position.Y, position.Z);
}