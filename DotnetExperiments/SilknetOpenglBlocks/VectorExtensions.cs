using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public static class VectorExtensions 
{
    public static Vector3D<TOut> Select<TIn, TOut>(this Vector3D<TIn> vector, Func<TIn, TOut> func)
        where TIn : unmanaged, IFormattable, IEquatable<TIn>, IComparable<TIn>
        where TOut : unmanaged, IFormattable, IEquatable<TOut>, IComparable<TOut>
    {
        return new Vector3D<TOut>(func(vector.X), func(vector.Y), func(vector.Z));
    }
    
    public static Vector3D<int> RoundToInt(this Vector3D<float> value) => value.Select(x => (int)MathF.Round(x));
    
    public static IEnumerable<T> ToEnumerable<T>(this Vector3D<T> value)
        where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        return [value.X, value.Y, value.Z];
    }
}