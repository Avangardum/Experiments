using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;

namespace SilknetOpenglBlocks;

public sealed class Game
{
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static IInputContext _input = null!;
    private static ShaderProgram _blockShaderProgram = null!;
    private static Vao _chunkVao = null!;
    private static uint _blockTextureId;
    private static Vector3 _cameraPosition = new(20, 20, 20);
    private static Vector3 _cameraFront = new(0, 0, 0);
    private static Vector3 _cameraUp = new(0, 1, 0);
    private static float _cameraPitch;
    private static float _cameraYaw = -90;
    private const int ChunkSize = 64;
    private static Block[,,] _chunk = new Block[ChunkSize, ChunkSize, ChunkSize];
    
    public Game(IWindow window)
    {
        _window = window;
        
        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
    }
    
    private static void InitChunk()
    {
        for (int x = 0; x < ChunkSize; x++)
        for (int y = 0; y < ChunkSize; y++)
        for (int z = 0; z < ChunkSize; z++)
        {
            _chunk[x, y, z] = y switch
            {
                < 16 => Block.Stone,
                16 => Block.Dirt,
                _ => Block.Air
            };
        }
        
        for (int y = 0; y < ChunkSize; y++)
        {
            _chunk[10, y, 10] = Block.Wood;
        }
    }
    
    private static void OnLoad()
    {
        InitChunk();
        SetupInput();
        _gl = _window.CreateOpenGL();
        _gl.ClearColor(Color.CornflowerBlue);
        _gl.Enable(EnableCap.DepthTest);
        SetupShaders();
        SetupBlockVao();
        SetupBlockTexture();
        //_gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
    }

    private static void SetupInput()
    {
        _input = _window.CreateInput();
        foreach (IKeyboard keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
        }
        foreach (IMouse mouse in _input.Mice)
        {
            mouse.Cursor.CursorMode = CursorMode.Disabled;
            mouse.MouseMove += OnMouseMove;
        }
    }
    
    private static void SetupShaders()
    {
        _blockShaderProgram = new ShaderProgram(_gl, "Block");
    }
    
    private static void SetupBlockVao()
    {
        _chunkVao = new Vao(_gl, [], [], [3, 2, 1]);
    }
    
    private static unsafe void SetupBlockTexture()
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

    private static void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape) _window.Close();
    }
    
    private static void OnUpdate(double deltaTime)
    {
        
    }
    
    private static void OnRender(double deltaTime)
    {
        ProcessInput(deltaTime);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _blockShaderProgram.Use();
        _blockShaderProgram.SetUniform("textureSampler", 0);
        Func<float, float> sin = it => float.Sin(float.DegreesToRadians(it));
        Func<float, float> cos = it => float.Cos(float.DegreesToRadians(it));
        _cameraFront = Vector3.Normalize
            (
                new Vector3
                (
                    cos(_cameraYaw) * cos(_cameraPitch),
                    sin(_cameraPitch),
                    sin(_cameraYaw) * cos(_cameraPitch)
                )
            );
        var model = Matrix4x4.Identity;
        var view = Matrix4x4.CreateLookAt(_cameraPosition, _cameraPosition + _cameraFront, _cameraUp);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90), 1, 0.01f, 100f);
        _blockShaderProgram.SetUniform("model", model);
        _blockShaderProgram.SetUniform("view", view);
        _blockShaderProgram.SetUniform("projection", projection);
        
        RenderChunk();
    }
    
    private static readonly ImmutableDictionary<Direction, ImmutableList<Vector3D<float>>> FaceVertexPositionsByDirection =
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
    
    private static void RenderChunk()
    {
        const int cubeFaces = 6;
        const int elementsPerCubeFace = 6;
        const int verticesPerCubeFace = 4;
        const int vertexSize = 6;
        
        List<float> vertices = [];
        int faceCount = 0;
        for (int x = 0; x < ChunkSize; x++)
        for (int y = 0; y < ChunkSize; y++)
        for (int z = 0; z < ChunkSize; z++)
        {
            if (_chunk[x, y, z] == Block.Air) continue;
            Vector3D<int> blockPos = new(x, y, z);
            foreach (Direction direction in Direction.All)
            {
                Vector3D<int> neighborPos = blockPos + direction.IntUnitVector;
                if (IsOpaqueBlockAt(neighborPos)) continue;
                foreach ((Vector3D<float> pos, int i) in FaceVertexPositionsByDirection[direction].Select((p, i) => (p, i)))
                {
                    vertices.Add(x + pos.X);
                    vertices.Add(y + pos.Y);
                    vertices.Add(z + pos.Z);
                    (float u, float v) = GetVertexUv(_chunk[x, y, z], i);
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
    
    private static (float U, float V) GetVertexUv(Block block, int vertexIndex)
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
    
    private static bool IsOpaqueBlockAt(Vector3D<int> pos)
    {
        if (pos.X is < 0 or >= ChunkSize) return false;
        if (pos.Y is < 0 or >= ChunkSize) return false;
        if (pos.Z is < 0 or >= ChunkSize) return false;
        return _chunk[pos.X, pos.Y, pos.Z] != Block.Air;
    }
    
    private static void ProcessInput(double deltaTime)
    {
        const float speed = 4;
        if (IsKeyPressed(Key.A)) _cameraPosition -= Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp)) * (float)deltaTime * speed; 
        if (IsKeyPressed(Key.D)) _cameraPosition += Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp)) * (float)deltaTime * speed;
        if (IsKeyPressed(Key.W)) _cameraPosition += _cameraFront * (float)deltaTime * speed;
        if (IsKeyPressed(Key.S)) _cameraPosition -= _cameraFront * (float)deltaTime * speed;
        if (IsKeyPressed(Key.Space)) _cameraPosition += Vector3.UnitY * (float)deltaTime * speed;
        if (IsKeyPressed(Key.ShiftLeft)) _cameraPosition -= Vector3.UnitY * (float)deltaTime * speed;
    }
    
    private static Vector2? _lastMousePosition;
    
    private static void OnMouseMove(IMouse mouse, Vector2 position)
    {
        _lastMousePosition ??= position;
        Vector2 delta = position - _lastMousePosition.Value;
        const float sensitivity = 0.05f;
        _cameraYaw += delta.X * sensitivity;
        _cameraPitch -= delta.Y * sensitivity;
        _cameraPitch = float.Clamp(_cameraPitch, -89, 89);
        _lastMousePosition = position;
    }
    
    private static bool IsKeyPressed(Key key) => _input.Keyboards.Any(x => x.IsKeyPressed(key));
}