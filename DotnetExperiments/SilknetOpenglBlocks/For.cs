using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public static class For
{
    public delegate void BreakableLoopBody<in T>(T value, Action breakLoop);
    
    public static void XyzInclusive(Vector3D<int> start, Vector3D<int> end, BreakableLoopBody<Vector3D<int>> body)
    {
        for (int x = start.X; x <= end.X; x++)
        for (int y = start.Y; y <= end.Y; y++)
        for (int z = start.Z; z <= end.Z; z++)
        {
            bool shouldBreak = false;
            body(new Vector3D<int>(x, y, z), () => shouldBreak = true);
            if (shouldBreak) return;
        }
    }
    
    public static void XyzInclusive(Vector3D<int> start, Vector3D<int> end, Action<Vector3D<int>> body) =>
        XyzInclusive(start, end, (value, _) => body(value));
    
    public static void XyzExclusive(Vector3D<int> start, Vector3D<int> end, Action<Vector3D<int>> body) =>
        XyzInclusive(start, end - Vector3D<int>.One, body);
    
    public static void XyzExclusive(Vector3D<int> start, Vector3D<int> end, BreakableLoopBody<Vector3D<int>> body) =>
        XyzInclusive(start, end - Vector3D<int>.One, body);
}