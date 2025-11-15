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
    private readonly IWindow _window;
    private readonly Game _game;
    private readonly Camera _camera;
    private GL _gl = null!;
    private ShaderProgram _blockShaderProgram = null!;
    private Vao _chunkVao = null!;
    private uint _blockTextureId;
    
    public Renderer(IWindow window, Game game, Camera camera)
    {
        _window = window;
        _game = game;
        _camera = camera;

        window.Load += OnLoad;
        window.Render += OnRender;
    }
    
    private void OnLoad()
    {
        _gl = _window.CreateOpenGL();
        _gl.ClearColor(Color.CornflowerBlue);
        _gl.Enable(EnableCap.DepthTest);
        SetupShaders();
        SetupBlockVao();
        SetupBlockTexture();
        //_gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
    }
    
    private void SetupShaders()
    {
        _blockShaderProgram = new ShaderProgram(_gl, "Block");
    }
    
    private void SetupBlockVao()
    {
        _chunkVao = new Vao(_gl, [], [], [3, 2, 1]);
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
    
    private void OnRender(double deltaTime)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _blockShaderProgram.Use();
        _blockShaderProgram.SetUniform("textureSampler", 0);
        var model = Matrix4x4.Identity;
        var view = _camera.ViewMatrix;
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90), 1, 0.01f, 100f);
        _blockShaderProgram.SetUniform("model", model);
        _blockShaderProgram.SetUniform("view", view);
        _blockShaderProgram.SetUniform("projection", projection);
        
        RenderChunk();
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
    
    private void RenderChunk()
    {
        const int cubeFaces = 6;
        const int elementsPerCubeFace = 6;
        const int verticesPerCubeFace = 4;
        const int vertexSize = 6;
        
        List<float> vertices = [];
        int faceCount = 0;
        for (int x = 0; x < Game.ChunkSize; x++)
        for (int y = 0; y < Game.ChunkSize; y++)
        for (int z = 0; z < Game.ChunkSize; z++)
        {
            if (_game.BlockAt(x, y, z) == Block.Air) continue;
            Vector3D<int> blockPos = new(x, y, z);
            foreach (Direction direction in Direction.All)
            {
                Vector3D<int> neighborPos = blockPos + direction.IntUnitVector;
                if (_game.BlockAt(neighborPos).IsOpaque()) continue;
                foreach ((Vector3D<float> pos, int i) in FaceVertexPositionsByDirection[direction].Select((p, i) => (p, i)))
                {
                    vertices.Add(x + pos.X);
                    vertices.Add(y + pos.Y);
                    vertices.Add(z + pos.Z);
                    (float u, float v) = GetVertexUv(_game.BlockAt(x, y, z), i);
                    vertices.Add(u);
                    vertices.Add(v);
                    float light =
                        direction == Direction.Forward ? 0.5f :
                        direction == Direction.Back ? 0.9f :
                        direction == Direction.Right ? 0.8f :
                        direction == Direction.Left ? 0.6f :
                        direction == Direction.Up ? 1.0f :
                        direction == Direction.Down ? 0.2f :
                        throw new ArgumentOutOfRangeException();
                    vertices.Add(light);
                }
                faceCount++;
            }
        }

        uint[] elements = Enumerable.Repeat(new [] { 0, 1, 2, 0, 2, 3 }, faceCount)
            .SelectMany((x, i) => x.Select(n => (uint)(n + i * verticesPerCubeFace)))
            .ToArray();
        
        _chunkVao.Update(vertices, elements);
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