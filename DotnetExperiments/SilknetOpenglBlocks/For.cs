using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public static class For
{
    public static void XyzInclusive(Vector3D<int> start, Vector3D<int> end, Action<Vector3D<int>> func)
    {
        for (int x = start.X; x <= end.X; x++)
        for (int y = start.Y; y <= end.Y; y++)
        for (int z = start.Z; z <= end.Z; z++)
            func(new Vector3D<int>(x, y, z));
    }
    
    public static void XyzExclusive(Vector3D<int> start, Vector3D<int> end, Action<Vector3D<int>> func) =>
        XyzInclusive(start, end - Vector3D<int>.One, func);
}