using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed record ChunkMesh
{
    public required Vector3D<int> Index { get; init; }
    
    public required IReadOnlyList<float> Vertices { get; init; }
}