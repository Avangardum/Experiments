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
    
    private const int ElementsPerCubeFace = 6;
    private const int CubeFaces = 6;
    private const int VerticesPerCubeFace = 4;
    private const int VertexSize = 6;
    
    public Renderer(GL gl, Game game, Camera camera)
    {
        _gl = gl;
        _game = game;
        _camera = camera;
        
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
        vao.SetVertexCount(4);
        return vao;
    }
    
    private void OnBlockUpdated(Vector3D<int> updatedBlockWorldPos)
    {
        Vector3D<int> updatedBlockChunkIndex = Chunk.WorldPosToChunkIndex(updatedBlockWorldPos);
        For.XyzInclusive(updatedBlockChunkIndex - Vector3D<int>.One, updatedBlockChunkIndex + Vector3D<int>.One, chunkIndex =>
        {
            Chunk chunk = _game.GetChunk(chunkIndex);
            GetChunkRenderState(chunk).ShouldRecalcVertices = true;
        });
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
        _crosshairVao.Draw();
    }
    
    private void RenderChunks()
    {
        _chunkShaderProgram.Use();
        _chunkShaderProgram.SetUniform("textureSampler", 0);
        Matrix4x4 model = Matrix4x4.Identity;
        Matrix4X4<float> view = _camera.ViewMatrix;
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView: float.DegreesToRadians(90),
            aspectRatio: 1, nearPlaneDistance: 0.01f, farPlaneDistance: 10_000f);
        _chunkShaderProgram.SetUniform("model", model);
        _chunkShaderProgram.SetUniform("view", view);
        _chunkShaderProgram.SetUniform("projection", projection);
        
        const int renderDistance = 10;
        Vector3D<int> currentChunkIndex = Chunk.WorldPosToChunkIndex(_camera.Position);
        Vector3D<int> minChunkIndex = currentChunkIndex - Vector3D<int>.One * renderDistance;
        Vector3D<int> maxChunkIndex = currentChunkIndex + Vector3D<int>.One * renderDistance;
        For.XyzInclusive(minChunkIndex, maxChunkIndex, (Vector3D<int> chunkIndex) =>
        {
            if (IsChunkReadyForRendering(chunkIndex))
                RenderChunk(_game.GetChunk(chunkIndex));
        });
    }
    
    public bool IsChunkReadyForRendering(Vector3D<int> index)
    {
        bool result = true;
        For.XyzInclusive(index - Vector3D<int>.One, index + Vector3D<int>.One, (i, breakLoop) =>
        {
            if (!_game.IsChunkGenerated(i))
            {
                result = false;
                breakLoop();
            }
        });
        return result;
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
        if (renderState.ShouldRecalcVertices) RecalcChunkVertices(renderState);
        renderState.Vao.Draw();
    }
    
    private void RecalcChunkVertices(ChunkRenderState renderState)
    {
        // TODO simplify
        int verticesNextIndex = 0;
        int faceCount = 0;
        renderState.Chunk.ForEachVisibleBlock((block, chunkPos) =>
        {
            Vector3D<int> worldPos = renderState.Chunk.ChunkPosToWorldPos(chunkPos);
            foreach (Direction direction in Direction.All)
            {
                Vector3D<int> neighborPos = worldPos + direction.IntUnitVector;
                if (_game.GetBlock(neighborPos).IsOpaque()) continue;
                ImmutableList<Vector3D<float>> vertexPositions = FaceVertexPositionsByDirection[direction];
                for (int i = 0; i < vertexPositions.Count; i++)
                {
                    Vector3D<float> vertexPos = vertexPositions[i];
                    _vertices[verticesNextIndex++] = worldPos.X + vertexPos.X;
                    _vertices[verticesNextIndex++] = worldPos.Y + vertexPos.Y;
                    _vertices[verticesNextIndex++] = worldPos.Z + vertexPos.Z;
                    (float u, float v) = GetVertexUv(block, i);
                    _vertices[verticesNextIndex++] = u;
                    _vertices[verticesNextIndex++] = v;
                    _vertices[verticesNextIndex++] = direction.GetLightLevel();
                }
                faceCount++;
            }
        });
            
        renderState.Vao.SetVertices(new Span<float>(_vertices, 0, verticesNextIndex));
        renderState.Vao.SetVertexCount((uint)faceCount * ElementsPerCubeFace);
        renderState.ShouldRecalcVertices = false;
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
        float offsetFromTopLeftU = vertexIndex is 0 or 1 ? 0 : textureSizeInUv;
        float offsetFromTopLeftV = vertexIndex is 1 or 2 ? 0 : textureSizeInUv;
        float u = topLeftU + offsetFromTopLeftU;
        float v = topLeftV + offsetFromTopLeftV;
        return (u, v);
    }
    
    private bool _isWireframeEnabled;
    
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