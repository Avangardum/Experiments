using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class ThreeDimensionalStorageBenchmark
{
    // | Method                       | Mean      | Error     | StdDev    | Median    |
    // |----------------------------- |----------:|----------:|----------:|----------:|
    // | ReadFromDictionary           | 5.3688 ns | 0.0423 ns | 0.0396 ns | 5.3695 ns |
    // | ReadFrom3dArray              | 0.1979 ns | 0.0061 ns | 0.0057 ns | 0.1979 ns |
    // | ReadFromJaggedArray          | 0.1835 ns | 0.0036 ns | 0.0032 ns | 0.1827 ns |
    // | ReadFromFlatArray            | 0.1916 ns | 0.0031 ns | 0.0029 ns | 0.1916 ns |
    // | ReadFromFlatArrayPrecomputed | 0.0056 ns | 0.0043 ns | 0.0038 ns | 0.0039 ns |
    
    public static int Size { get; set; } = 100;
    
    private Dictionary<Vector3, float> _dictionary = Enumerable.Range(0, Size * Size * Size)
        .Select(it => new Vector3(it / (Size * Size), it / Size % Size, it % Size))
        .ToDictionary(it => it, _ => 0f);
    private float[,,] _3dArray = new float[Size, Size, Size];
    private float[][][] _jaggedArray = Enumerable.Range(0, Size)
        .Select(_ => Enumerable.Range(0, Size).Select(_ => new float[Size]).ToArray()).ToArray();
    private float[] _flatArray = new float[Size * Size * Size];
    
    public static void Main()
    {
        BenchmarkRunner.Run<ThreeDimensionalStorageBenchmark>();
    }
    
    [Benchmark]
    public float ReadFromDictionary() => _dictionary[new Vector3(42, 42, 42)];

    [Benchmark]
    public float ReadFrom3dArray() => _3dArray[42, 42, 42];
    
    [Benchmark]
    public float ReadFromJaggedArray() => _jaggedArray[42][42][42];
    
    [Benchmark]
    public float ReadFromFlatArray() => _flatArray[42 * Size * Size + 42 * Size + 42];
    
    [Benchmark]
    public float ReadFromFlatArrayPrecomputed() => _flatArray[424242];
}