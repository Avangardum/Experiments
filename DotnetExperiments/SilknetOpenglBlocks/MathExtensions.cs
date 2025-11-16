using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public static class MathExtensions
{
    public static Vector3D<int> Remainder(this Vector3D<int> dividend, int divisor) =>
        new(dividend.X % divisor, dividend.Y % divisor, dividend.Z % divisor);
}