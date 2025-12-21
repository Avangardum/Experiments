using System.Collections.Concurrent;
using System.Collections.Immutable;
using Silk.NET.Maths;
using static SilknetOpenglBlocks.Const;

namespace SilknetOpenglBlocks;

public sealed class ChunkMesher(Game game) // TODO remove dependency on Game
{
    private record Request
    (
        IReadOnlyDictionary<Vector3D<int>, Chunk> ChunksToMeshAndTheirNeighbors,
        IReadOnlyList<Vector3D<int>> ChunksToMeshIndices
    );
    
    private readonly ConcurrentQueue<Request> _requests = [];
    private readonly ConcurrentQueue<ChunkMesh> _generatedMeshes = [];
    
    private static readonly ImmutableList<(Direction, ImmutableList<Vector3D<float>>)> DirectionsAndFaceVertices =
    [
        (
            Direction.Back,
            [new(-0.5f, -0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(0.5f, -0.5f, 0.5f)]
        ),
        (
            Direction.Forward,
            [new(0.5f, -0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f)]
        ),
        (
            Direction.Right,
            [new(0.5f, -0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, -0.5f, -0.5f)]
        ),
        (
            Direction.Left,
            [new(-0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, 0.5f), new(-0.5f, -0.5f, 0.5f)]
        ),
        (
            Direction.Up,
            [new(-0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, 0.5f)]
        ),
        (
            Direction.Down,
            [new(-0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, -0.5f)]
        )
    ];
    
    public void RequestMeshing
    (
        IReadOnlyDictionary<Vector3D<int>, Chunk> chunksToMeshAndTheirNeighbors,
        IReadOnlyList<Vector3D<int>> chunksToMeshIndices
    )
    {
        foreach (var index in chunksToMeshIndices)
        {
            Chunk chunk = chunksToMeshAndTheirNeighbors[index];
            ChunkMesh mesh = GenerateChunkMesh(chunk);
            _generatedMeshes.Enqueue(mesh);
        }
    }
    
    public IReadOnlyList<ChunkMesh> TakeGeneratedMeshes()
    {
        List<ChunkMesh> result = [];
        while (!_generatedMeshes.IsEmpty)
        {
            if (_generatedMeshes.TryDequeue(out ChunkMesh? mesh)) result.Add(mesh);
        }
        return result;
    }
    
    private ChunkMesh GenerateChunkMesh(Chunk chunk)
    {
        List<float> vertices = [];
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        for (int z = 0; z < Chunk.Size; z++)
        {
            Block block = chunk[x, y, z];
            if (!block.IsVisible()) continue;
            Vector3D<int> chunkPos = new(x, y, z);
            for (int i = 0; i < DirectionsAndFaceVertices.Count; i++)
            {
                (Direction direction, IReadOnlyList<Vector3D<float>> faceVertices) = DirectionsAndFaceVertices[i];
                GenerateBlockFace(block, chunkPos, direction, faceVertices);
            }
        }
        return new ChunkMesh { Index = chunk.Index, Vertices = vertices };
        
        void GenerateBlockFace
        (
            Block block,
            Vector3D<int> chunkPos,
            Direction direction,
            IReadOnlyList<Vector3D<float>> vertexPositions
        )
        {
            Vector3D<int> worldPos = chunk.ChunkPosToWorldPos(chunkPos);
            if (GetNeighborBlock(chunkPos, worldPos, direction).IsOpaque()) return;
            for (int i = 0; i < vertexPositions.Count; i++)
            {
                Vector3D<float> vertexPos = vertexPositions[i];
                vertices.Add(worldPos.X + vertexPos.X);
                vertices.Add(worldPos.Y + vertexPos.Y);
                vertices.Add(worldPos.Z + vertexPos.Z);
                (float u, float v) = GetVertexUv(block, i);
                vertices.Add(u);
                vertices.Add(v);
                vertices.Add(direction.GetLightLevel());
            }
        }
        
        Block GetNeighborBlock(Vector3D<int> chunkPos, Vector3D<int> worldPos, Direction direction)
        {
            Vector3D<int> neighborSameChunkPos = chunkPos + direction.IntUnitVector;
            return Chunk.IsValidChunkPos(neighborSameChunkPos) ?
                chunk[neighborSameChunkPos] :
                game.GetBlock(worldPos + direction.IntUnitVector); 
        }
    }
    
    private (float U, float V) GetVertexUv(Block block, int vertexIndex)
    {
        const int spriteSheetSizeInBlocks = 8;
        const float textureSizeInUv = 1 / (float)spriteSheetSizeInBlocks;
        int row = (int)block / spriteSheetSizeInBlocks;
        int column = (int)block % spriteSheetSizeInBlocks;
        float topLeftU = column * textureSizeInUv;
        float topLeftV = row * textureSizeInUv;
        // TODO fix edge sampling
        float offsetFromTopLeftU = vertexIndex is 0 or 1 ? 0 : textureSizeInUv;
        float offsetFromTopLeftV = vertexIndex is 1 or 2 ? 0 : textureSizeInUv;
        float u = topLeftU + offsetFromTopLeftU;
        float v = topLeftV + offsetFromTopLeftV;
        return (u, v);
    }
}