using System.Collections.Concurrent;
using System.Collections.Immutable;
using Silk.NET.Maths;
using static SilknetOpenglBlocks.Const;

namespace SilknetOpenglBlocks;

public sealed class ChunkMesher
{
    // TODO add priority
    
    private record MeshingRequest
    (
        IReadOnlyDictionary<Vector3D<int>, Chunk> Chunks,
        IReadOnlyList<Vector3D<int>> ChunksToMeshIndices
    );
    
    private readonly ConcurrentQueue<MeshingRequest> _meshingRequests = [];
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

    public ChunkMesher()
    {
        Task.Run(Loop);
    }
    
    private void Loop()
    {
        while (true)
        {
            if (_meshingRequests.TryDequeue(out MeshingRequest? request))
            {
                foreach (var index in request.ChunksToMeshIndices)
                {
                    ChunkMesh mesh = GenerateChunkMesh(request.Chunks[index], request.Chunks);
                    _generatedMeshes.Enqueue(mesh);
                }
            }
        }
    }
    
    public void RequestMeshing
    (
        IReadOnlyDictionary<Vector3D<int>, Chunk> chunks,
        IReadOnlyList<Vector3D<int>> chunksToMeshIndices
    )
    {
        _meshingRequests.Enqueue(new MeshingRequest(chunks, chunksToMeshIndices));
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
    
    private ChunkMesh GenerateChunkMesh(Chunk chunkToMesh, IReadOnlyDictionary<Vector3D<int>, Chunk> chunks)
    {
        // Here we read chunk data from the meshing thread, which could be altered by the main thread at the same time,
        // leading to inconsistent data reads. But in such case affected meshes will be regenerated soon, so we can
        // disregard this.
        
        List<float> vertices = [];
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        for (int z = 0; z < Chunk.Size; z++)
        {
            Block block = chunkToMesh[x, y, z];
            if (!block.IsVisible()) continue;
            Vector3D<int> chunkPos = new(x, y, z);
            for (int i = 0; i < DirectionsAndFaceVertices.Count; i++)
            {
                (Direction direction, IReadOnlyList<Vector3D<float>> faceVertices) = DirectionsAndFaceVertices[i];
                GenerateBlockFace(block, chunkPos, direction, faceVertices);
            }
        }
        return new ChunkMesh { Index = chunkToMesh.Index, Vertices = vertices };
        
        void GenerateBlockFace
        (
            Block block,
            Vector3D<int> chunkPos,
            Direction direction,
            IReadOnlyList<Vector3D<float>> vertexPositions
        )
        {
            Vector3D<int> worldPos = chunkToMesh.ChunkPosToWorldPos(chunkPos);
            if (GetNeighborBlock(worldPos, direction).IsOpaque()) return;
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
        
        Block GetNeighborBlock(Vector3D<int> worldPos, Direction direction)
        {
            Vector3D<int> neighborWorldPos = worldPos + direction.IntUnitVector;
            Vector3D<int> neighborChunkIndex = Chunk.WorldPosToChunkIndex(neighborWorldPos);
            Vector3D<int> neighborChunkPos = Chunk.WorldPosToChunkPos(neighborWorldPos);
            return chunks[neighborChunkIndex][neighborChunkPos];
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