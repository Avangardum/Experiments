using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using static SilknetOpenglBlocks.Const;

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
    private readonly Dictionary<Vector3D<int>, ChunkRenderState> _chunkRenderStates = [];
    private readonly Vao _crosshairVao;
    private readonly float _aspectRatio;
    private bool _isWireframeEnabled;
    private readonly ChunkMesher _chunkMesher;
    
    public Renderer(GL gl, Game game, Camera camera, float aspectRatio)
    {
        _gl = gl;
        _game = game;
        _camera = camera;
        _aspectRatio = aspectRatio;
        _chunkMesher = new ChunkMesher();
        
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
        Vector3D<int> chunkIndex = Chunk.WorldPosToChunkIndex(updatedBlockWorldPos);
        GetChunkRenderState(chunkIndex).ShouldRequestMeshing = true;
        GetChunkRenderState(chunkIndex + Vector3D<int>.UnitX).ShouldRequestMeshing = true;
        GetChunkRenderState(chunkIndex - Vector3D<int>.UnitX).ShouldRequestMeshing = true;
        GetChunkRenderState(chunkIndex + Vector3D<int>.UnitY).ShouldRequestMeshing = true;
        GetChunkRenderState(chunkIndex - Vector3D<int>.UnitY).ShouldRequestMeshing = true;
        GetChunkRenderState(chunkIndex + Vector3D<int>.UnitZ).ShouldRequestMeshing = true;
        GetChunkRenderState(chunkIndex - Vector3D<int>.UnitZ).ShouldRequestMeshing = true;
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
        Matrix4X4<float> view = _camera.ViewMatrix;
        Matrix4X4<float> projection = Matrix4X4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90), _aspectRatio,
            nearPlaneDistance: 0.01f, farPlaneDistance: 1000);
        Matrix4X4<float> viewProjection = view * projection;
        _chunkShaderProgram.SetUniform("view", view);
        _chunkShaderProgram.SetUniform("projection", projection);
        ApplyGeneratedChunkMeshes();
        
        const int renderDistance = 3;
        Vector3D<int> currentChunkIndex = Chunk.WorldPosToChunkIndex(_camera.Position);
        Vector3D<int> minChunkIndex = currentChunkIndex - Vector3D<int>.One * renderDistance;
        Vector3D<int> maxChunkIndex = currentChunkIndex + Vector3D<int>.One * renderDistance;
        
        List<Chunk> chunksToMesh = [];
        
        for (int x = minChunkIndex.X; x <= maxChunkIndex.X; x++)
        for (int y = minChunkIndex.Y; y <= maxChunkIndex.Y; y++)
        for (int z = minChunkIndex.Z; z <= maxChunkIndex.Z; z++)
        {
            Vector3D<int> index = new(x, y, z);
            ChunkRenderState renderState = GetChunkRenderState(index);
            if (!IsChunkInFrustum(renderState.Chunk, viewProjection)) continue;
            if (renderState.ShouldRequestMeshing)
            {
                chunksToMesh.Add(renderState.Chunk);
                renderState.ShouldRequestMeshing = false;
            }
            renderState.Vao.Draw();
        }
        
        RequestChunkMeshing(chunksToMesh);
    }
    
    private bool IsChunkInFrustum(Chunk chunk, Matrix4X4<float> viewProjection)
    {
        // TODO Implement
        return true;
    }
    
    private void ApplyGeneratedChunkMeshes()
    {
        IReadOnlyList<ChunkMesh> meshes = _chunkMesher.TakeGeneratedMeshes();
        foreach (var mesh in meshes)
        {
            ChunkRenderState renderState = _chunkRenderStates[mesh.Index];
            renderState.Vao.SetVertices(mesh.Vertices);
            renderState.Vao.SetVertexCount((uint)mesh.Vertices.Count);
        }
    }
    
    private void RequestChunkMeshing(IReadOnlyList<Chunk> chunksToMesh)
    {
        ImmutableList<Vector3D<int>> chunksToMeshIndices = chunksToMesh.Select(it => it.Index).ToImmutableList();
        Dictionary<Vector3D<int>, Chunk> chunksToMeshAndTheirNeighbors =
            chunksToMesh.ToDictionary(it => it.Index, it => it);
        foreach (Vector3D<int> index in chunksToMeshAndTheirNeighbors.Keys.ToImmutableList())
        foreach (Direction direction in Direction.All)
        {
            Vector3D<int> neighborIndex = index + direction.IntUnitVector;
            if (!chunksToMeshAndTheirNeighbors.ContainsKey(neighborIndex))
                chunksToMeshAndTheirNeighbors[neighborIndex] = GetChunkRenderState(neighborIndex).Chunk;
        }
        _chunkMesher.RequestMeshing(chunksToMeshAndTheirNeighbors, chunksToMeshIndices);
    }

    private void HandleGlErrors()
    {
        GLEnum error = _gl.GetError();
        if (error == GLEnum.NoError) return;
        Console.WriteLine($"OpenGL error {error}.");
    }
    
    private ChunkRenderState GetChunkRenderState(Vector3D<int> index)
    {
        if (_chunkRenderStates.TryGetValue(index, out ChunkRenderState? renderState)) return renderState;
        renderState = CreateChunkRenderState(index);
        _chunkRenderStates[index] = renderState;
        return renderState;
    }
    
    private ChunkRenderState CreateChunkRenderState(Vector3D<int> index)
    {
        Vao vao = new(_gl);
        vao.SetVertices([]);
        vao.SetVertexCount(0);
        vao.SetElements(_chunkEboId);
        vao.SetVertexAttributeSizes([3, 2, 1]);
        return new ChunkRenderState
        {
            Chunk = _game.GetChunk(index),
            Vao = vao
        };
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