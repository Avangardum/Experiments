using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;

namespace SilknetOpenglBlocks;

public sealed class Renderer
{
    private readonly Game _game;
    private readonly Camera _camera;
    private readonly GL _gl;
    private ShaderProgram _chunkShaderProgram = null!;
    private ShaderProgram _crosshairShaderProgram = null!;
    private uint _blockTextureId;
    private uint _chunkEboId;
    private readonly float[] _vertices = new float[Chunk.Volume * CubeFaces * VerticesPerCubeFace * VertexSize];
    private readonly Dictionary<Chunk, ChunkRenderState> _chunkRenderStates = [];
    private readonly Vao _crosshairVao;
    private readonly float _aspectRatio;
    private bool _isWireframeEnabled;
    
    private const int ElementsPerCubeFace = 6;
    private const int CubeFaces = 6;
    private const int VerticesPerCubeFace = 4;
    private const int VertexSize = 6;
    
    public Renderer(GL gl, Game game, Camera camera, float aspectRatio)
    {
        _gl = gl;
        _game = game;
        _camera = camera;
        _aspectRatio = aspectRatio;
        
        gl.ClearColor(Color.CornflowerBlue);
        gl.Enable(EnableCap.DepthTest);
        SetupShaders();
        SetupChunkEbo();
        SetupBlockTexture();
        _crosshairVao = SetupCrosshairVao();
        game.BlockUpdated += OnBlockUpdated;
    }
    
    private Vao SetupCrosshairVao()
    {
        Vao vao = new(_gl);
        vao.SetVertexAttributeSizes([2]);
        vao.SetPrimitiveType(PrimitiveType.Lines);
        vao.SetVertices([-0.02f, 0f, 0.02f, 0f, 0f, -0.02f, 0f, 0.02f]);
        vao.SetVertexOrElementCount(4);
        return vao;
    }
    
    private void OnBlockUpdated(Vector3D<int> updatedBlockWorldPos)
    {
        Vector3D<int> updatedBlockChunkIndex = Chunk.WorldPosToChunkIndex(updatedBlockWorldPos);
        for (int x = updatedBlockChunkIndex.X - 1; x <= updatedBlockChunkIndex.X + 1; x++)
        for (int y = updatedBlockChunkIndex.Y - 1; y <= updatedBlockChunkIndex.Y + 1; y++)
        for (int z = updatedBlockChunkIndex.Z - 1; z <= updatedBlockChunkIndex.Z + 1; z++)
        {
            Chunk chunk = _game.GetChunk(new Vector3D<int>(x, y, z));
            GetChunkRenderState(chunk).ShouldRecalcVertices = true;
        }
    }
    
    private void SetupShaders()
    {
        _chunkShaderProgram = new ShaderProgram(_gl, "Block");
        _crosshairShaderProgram = new ShaderProgram(_gl, "Crosshair");
    }
    
    private void SetupChunkEbo()
    {
        uint[] elements = Enumerable.Repeat(new [] { 0, 1, 2, 0, 2, 3 }, Chunk.Volume * CubeFaces)
            .SelectMany((x, i) => x.Select(n => (uint)(n + i * VerticesPerCubeFace)))
            .ToArray();
        _chunkEboId = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _chunkEboId);
        _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, elements, BufferUsageARB.StaticDraw);
    }
    
    private unsafe void SetupBlockTexture()
    {
        _blockTextureId = _gl.GenTexture();
        _gl.ActiveTexture(GLEnum.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _blockTextureId);
        
        byte[] bytes = File.ReadAllBytes("Textures/Blocks.png");
        ImageResult imageResult = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
        fixed (byte* ptr = imageResult.Data)
            _gl.TexImage2D
            (
                target: TextureTarget.Texture2D,
                level: 0,
                internalformat: InternalFormat.Rgba,
                width: (uint)imageResult.Width,
                height: (uint)imageResult.Height,
                border: 0,
                format: PixelFormat.Rgba,
                type: PixelType.UnsignedByte,
                pixels: ptr
            );
        
        _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
    }

    public void Render(double deltaTime)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        RenderChunks();
        RenderCrosshair();
        HandleGlErrors();
    }
    
    private void RenderCrosshair()
    {
        _crosshairShaderProgram.Use();
        _crosshairShaderProgram.SetUniform("aspectRatio", _aspectRatio);
        _crosshairVao.Draw();
    }
    
    private void RenderChunks()
    {
        _chunkShaderProgram.Use();
        _chunkShaderProgram.SetUniform("textureSampler", 0);
        Matrix4X4<float> model = Matrix4X4<float>.Identity; // TODO remove
        Matrix4X4<float> view = _camera.ViewMatrix;
        Matrix4X4<float> projection = Matrix4X4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90), _aspectRatio,
            nearPlaneDistance: 0.01f, farPlaneDistance: 1000);
        Matrix4X4<float> viewProjection = view * projection;
        _chunkShaderProgram.SetUniform("model", model);
        _chunkShaderProgram.SetUniform("view", view);
        _chunkShaderProgram.SetUniform("projection", projection);
        
        const int renderDistance = 5;
        Vector3D<int> currentChunkIndex = Chunk.WorldPosToChunkIndex(_camera.Position);
        Vector3D<int> minChunkIndex = currentChunkIndex - Vector3D<int>.One * renderDistance;
        Vector3D<int> maxChunkIndex = currentChunkIndex + Vector3D<int>.One * renderDistance;
     
        for (int x = minChunkIndex.X; x <= maxChunkIndex.X; x++)
        for (int y = minChunkIndex.Y; y <= maxChunkIndex.Y; y++)
        for (int z = minChunkIndex.Z; z <= maxChunkIndex.Z; z++)
        {
            Vector3D<int> index = new(x, y, z);
            if (!IsChunkReadyForRendering(index)) continue;
            Chunk chunk = _game.GetChunk(index);
            // TODO Frustum culling currently decreases FPS, review later.
            // if (!IsChunkInFrustum(chunk, viewProjection)) continue;
            RenderChunk(chunk);
        }
    }
    
    private bool IsChunkInFrustum(Chunk chunk, Matrix4X4<float> viewProjection)
    {
        IReadOnlyList<Vector3D<float>> worldSpaceCorners = chunk.Aabb.Corners;
        IReadOnlyList<Vector3D<float>> clipSpaceCorners =
            worldSpaceCorners.Select(it => it.TransformHomogenous(viewProjection)).ToImmutableList();

        if (clipSpaceCorners.All(it => it.X > 1)) return false;
        if (clipSpaceCorners.All(it => it.X < -1)) return false;
        if (clipSpaceCorners.All(it => it.Y > 1)) return false;
        if (clipSpaceCorners.All(it => it.Y < -1)) return false;
        return true;
    }
    
    private bool IsChunkReadyForRendering(Vector3D<int> index)
    {
        return _game.IsChunkGenerated(index) &&
            _game.IsChunkGenerated(index + Vector3D<int>.UnitX) &&
            _game.IsChunkGenerated(index - Vector3D<int>.UnitX) &&
            _game.IsChunkGenerated(index + Vector3D<int>.UnitY) &&
            _game.IsChunkGenerated(index - Vector3D<int>.UnitY) &&
            _game.IsChunkGenerated(index + Vector3D<int>.UnitZ) &&
            _game.IsChunkGenerated(index - Vector3D<int>.UnitZ);
    }
    
    private void HandleGlErrors()
    {
        GLEnum error = _gl.GetError();
        if (error == GLEnum.NoError) return;
        Console.WriteLine($"OpenGL error {error}.");
    }
    
    private readonly ImmutableDictionary<Direction, ImmutableList<Vector3D<float>>> FaceVertexPositionsByDirection =
        new Dictionary<Direction, ImmutableList<Vector3D<float>>>
        {
            [Direction.Back] =
                [new(-0.5f, -0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(0.5f, -0.5f, 0.5f)],
            [Direction.Forward] =
                [new(0.5f, -0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f)],
            [Direction.Right] =
                [new(0.5f, -0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, -0.5f, -0.5f)],
            [Direction.Left] =
                [new(-0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, 0.5f), new(-0.5f, -0.5f, 0.5f)],
            [Direction.Up] =
                [new(-0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, 0.5f)],
            [Direction.Down] =
                [new(-0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, -0.5f)]
        }.ToImmutableDictionary();
    
    private void RenderChunk(Chunk chunk)
    {
        ChunkRenderState renderState = GetChunkRenderState(chunk);
        if (renderState.ShouldRecalcVertices) GenerateChunkGeometry(renderState);
        renderState.Vao.Draw();
    }
    
    private void GenerateChunkGeometry(ChunkRenderState renderState)
    {
        int vertexCount = 0;
        int faceCount = 0;
        renderState.Chunk.ForEachVisibleBlock((block, chunkPos) =>
        {
            Vector3D<int> worldPos = renderState.Chunk.ChunkPosToWorldPos(chunkPos);
            foreach (Direction direction in Direction.All) GenerateBlockFace(block, worldPos, direction);
        });
            
        renderState.Vao.SetVertices(new Span<float>(_vertices, 0, vertexCount));
        renderState.Vao.SetVertexOrElementCount((uint)faceCount * ElementsPerCubeFace);
        renderState.ShouldRecalcVertices = false;
        return;
        
        void GenerateBlockFace(Block block, Vector3D<int> worldPos, Direction direction)
        {
            Vector3D<int> neighborPos = worldPos + direction.IntUnitVector;
            if (_game.GetBlock(neighborPos).IsOpaque()) return;
            ImmutableList<Vector3D<float>> vertexPositions = FaceVertexPositionsByDirection[direction];
            for (int i = 0; i < vertexPositions.Count; i++)
            {
                Vector3D<float> vertexPos = vertexPositions[i];
                _vertices[vertexCount++] = worldPos.X + vertexPos.X;
                _vertices[vertexCount++] = worldPos.Y + vertexPos.Y;
                _vertices[vertexCount++] = worldPos.Z + vertexPos.Z;
                (float u, float v) = GetVertexUv(block, i);
                _vertices[vertexCount++] = u;
                _vertices[vertexCount++] = v;
                _vertices[vertexCount++] = direction.GetLightLevel();
            }
            faceCount++;
        }
    }
    
    private ChunkRenderState GetChunkRenderState(Chunk chunk)
    {
        if (_chunkRenderStates.TryGetValue(chunk, out ChunkRenderState? renderState)) return renderState;
        renderState = CreateChunkRenderState(chunk);
        _chunkRenderStates[chunk] = renderState;
        return renderState;
    }
    
    private ChunkRenderState CreateChunkRenderState(Chunk chunk)
    {
        Vao vao = new(_gl);
        vao.SetElements(_chunkEboId);
        vao.SetVertexAttributeSizes([3, 2, 1]);
        return new ChunkRenderState
        {
            Chunk = chunk,
            Vao = vao
        };
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
    
    public void ToggleWireframe()
    {
        _isWireframeEnabled = !_isWireframeEnabled;
        _gl.PolygonMode(TriangleFace.FrontAndBack, _isWireframeEnabled ? PolygonMode.Line : PolygonMode.Fill);
    }
    
    public void PrintState()
    {
        Console.WriteLine($"Camera pos: {_camera.Position}");
    }
}