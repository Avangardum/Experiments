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
    private uint _blockTextureId;
    private uint _chunkEboId;
    
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
        //_gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
    }
    
    private void SetupShaders()
    {
        _chunkShaderProgram = new ShaderProgram(_gl, "Block");
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
        _chunkShaderProgram.Use();
        _chunkShaderProgram.SetUniform("textureSampler", 0);
        var model = Matrix4x4.Identity;
        var view = _camera.ViewMatrix;
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView: float.DegreesToRadians(90),
            aspectRatio: 1, nearPlaneDistance: 0.01f, farPlaneDistance: 10_000f);
        _chunkShaderProgram.SetUniform("model", model);
        _chunkShaderProgram.SetUniform("view", view);
        _chunkShaderProgram.SetUniform("projection", projection);
        
        RenderChunks();
        
        HandleGlErrors();
    }
    
    private void RenderChunks()
    {
        const int renderDistance = 1;
        Vector3D<int> currentChunkIndex = Chunk.PosToChunkIndex(_camera.Position);
        Vector3D<int> minChunkIndex = currentChunkIndex - Vector3D<int>.One * renderDistance;
        Vector3D<int> maxChunkIndex = currentChunkIndex + Vector3D<int>.One * renderDistance;
        ForXyzInclusive(minChunkIndex, maxChunkIndex, (Vector3D<int> chunkIndex) =>
        {
            RenderChunk(_game.GetChunk(chunkIndex));
        });
    }
    
    private void ForXyzInclusive(Vector3D<int> start, Vector3D<int> end, Action<Vector3D<int>> func)
    {
        for (int x = start.X; x <= end.X; x++)
        for (int y = start.Y; y <= end.Y; y++)
        for (int z = start.Z; z <= end.Z; z++)
            func(new Vector3D<int>(x, y, z));
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
    
    private readonly float[] _vertices = new float[Chunk.Volume * CubeFaces * VerticesPerCubeFace * VertexSize];
    
    private void RenderChunk(Chunk chunk)
    {
        int verticesNextIndex = 0;
        int faceCount = 0;
        chunk.ForEachVisibleBlock((block, blockPosInChunk) =>
        {
            Vector3D<int> blockPosInWorld = Chunk.Size * chunk.Index + blockPosInChunk;
            foreach (Direction direction in Direction.All)
            {
                Vector3D<int> neighborPos = blockPosInWorld + direction.IntUnitVector;
                if (_game.BlockAt(neighborPos).IsOpaque()) continue;
                ImmutableList<Vector3D<float>> vertexPositions = FaceVertexPositionsByDirection[direction];
                for (int i = 0; i < vertexPositions.Count; i++)
                {
                    Vector3D<float> vertexPos = vertexPositions[i];
                    _vertices[verticesNextIndex++] = blockPosInWorld.X + vertexPos.X;
                    _vertices[verticesNextIndex++] = blockPosInWorld.Y + vertexPos.Y;
                    _vertices[verticesNextIndex++] = blockPosInWorld.Z + vertexPos.Z;
                    (float u, float v) = GetVertexUv(block, i);
                    _vertices[verticesNextIndex++] = u;
                    _vertices[verticesNextIndex++] = v;
                    _vertices[verticesNextIndex++] = direction.GetLightLevel();
                }
                faceCount++;
            }
        });
        
        Vao _chunkVao = new(_gl);
        _chunkVao.SetElements(_chunkEboId);
        _chunkVao.SetVertexAttributeSizes([3, 2, 1]);
        _chunkVao.SetVertices(new Span<float>(_vertices, 0, verticesNextIndex));
        _chunkVao.SetVertexCount((uint)faceCount * ElementsPerCubeFace);
        _chunkVao.Draw();
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
}