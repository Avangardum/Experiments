using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Direction
{
    public static readonly Direction Forward = new(nameof(Forward), new(0, 0, -1));
    public static readonly Direction Back = new(nameof(Back), new(0, 0, 1));
    public static readonly Direction Up = new(nameof(Up), new(0, 1, 0));
    public static readonly Direction Down = new(nameof(Down), new(0, -1, 0));
    public static readonly Direction Right = new(nameof(Right), new(1, 0, 0));
    public static readonly Direction Left = new(nameof(Left), new(-1, 0, 0));
    
    public static readonly IReadOnlyList<Direction> All = [Forward, Back, Up, Down, Right, Left];
    
    public string Name { get; }
    public Vector3D<int> IntUnitVector { get; }
    public Vector3D<float> FloatUnitVector { get; }
    
    private Direction(string name, Vector3D<int> intUnitVector)
    {
        Name = name;
        IntUnitVector = intUnitVector;
        FloatUnitVector = intUnitVector.As<float>();
    }

    public override string ToString() => Name;
}