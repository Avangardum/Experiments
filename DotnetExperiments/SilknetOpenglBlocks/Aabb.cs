using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public readonly record struct Aabb(Vector3D<float> Start, Vector3D<float> End)
{
    public IReadOnlyList<Vector3D<float>> Corners =>
    [
        new(Start.X, Start.Y, Start.Z),
        new(  End.X, Start.Y, Start.Z),
        new(  End.X, Start.Y,   End.Z),
        new(Start.X, Start.Y,   End.Z),
        new(Start.X,   End.Y, Start.Z),
        new(  End.X,   End.Y, Start.Z),
        new(  End.X,   End.Y,   End.Z),
        new(Start.X,   End.Y,   End.Z)
    ];
}