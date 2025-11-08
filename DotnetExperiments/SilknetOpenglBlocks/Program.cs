using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;

namespace SilknetOpenglBlocks;

public static class Program
{
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static ShaderProgram _blockShaderProgram = null!;
    private static StaticModelVao _blockVao = null!;
    private static uint _blockTextureId;
    private static float _time;
    
    private static void Main()
    {
        WindowOptions options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(800, 800),
            Title = "Silk.NET OpenGL Blocks"
        };
        _window = Window.Create(options);
     
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        
        _window.Run();
    }
    
    private static void OnLoad()
    {
        SetupInput();
        _gl = _window.CreateOpenGL();
        _gl.ClearColor(Color.CornflowerBlue);
        _gl.Enable(EnableCap.DepthTest);
        SetupShaders();
        SetupBlockVao();
        SetupBlockTexture();
    }

    private static void SetupInput()
    {
        IInputContext input = _window.CreateInput();
        foreach (IKeyboard keyboard in input.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
        }
    }
    
    private static void SetupShaders()
    {
        _blockShaderProgram = new ShaderProgram(_gl, "Block");
    }
    
    private static void SetupBlockVao()
    {
        IReadOnlyList<float> vertices =
        [
//           x      y      z     u  v  light
            -0.5f, -0.5f,  0.5f, 0, 1, 1.0f,  // 0 front
            -0.5f,  0.5f,  0.5f, 0, 0, 1.0f,  // 1
             0.5f,  0.5f,  0.5f, 1, 0, 1.0f,  // 2
             0.5f, -0.5f,  0.5f, 1, 1, 1.0f,  // 3
             0.5f, -0.5f, -0.5f, 0, 1, 0.5f,  // 4 back
             0.5f,  0.5f, -0.5f, 0, 0, 0.5f,  // 5
            -0.5f,  0.5f, -0.5f, 1, 0, 0.5f,  // 6
            -0.5f, -0.5f, -0.5f, 1, 1, 0.5f,  // 7
             0.5f, -0.5f,  0.5f, 0, 1, 0.8f,  // 8 right
             0.5f,  0.5f,  0.5f, 0, 0, 0.8f,  // 9
             0.5f,  0.5f, -0.5f, 1, 0, 0.8f,  // 10
             0.5f, -0.5f, -0.5f, 1, 1, 0.8f,  // 11
            -0.5f, -0.5f, -0.5f, 0, 1, 0.6f,  // 12 left
            -0.5f,  0.5f, -0.5f, 0, 0, 0.6f,  // 13
            -0.5f,  0.5f,  0.5f, 1, 0, 0.6f,  // 14
            -0.5f, -0.5f,  0.5f, 1, 1, 0.6f,  // 15
            -0.5f,  0.5f,  0.5f, 0, 1, 0.9f,  // 16 top
            -0.5f,  0.5f, -0.5f, 0, 0, 0.9f,  // 17
             0.5f,  0.5f, -0.5f, 1, 0, 0.9f,  // 18
             0.5f,  0.5f,  0.5f, 1, 1, 0.9f,  // 19
            -0.5f, -0.5f, -0.5f, 0, 1, 0.2f,  // 20 bottom
            -0.5f, -0.5f,  0.5f, 0, 0, 0.2f,  // 21
             0.5f, -0.5f,  0.5f, 1, 0, 0.2f,  // 22
             0.5f, -0.5f, -0.5f, 1, 1, 0.2f,  // 23
        ];
        
        IReadOnlyList<uint> indices =
        [
             0,  1,  2, // front
             0,  2,  3,
             4,  5,  6, // back
             4,  6,  7,
             8,  9, 10, // right
             8, 10, 11,
            12, 13, 14, // left
            12, 14, 15,
            16, 17, 18, // top
            16, 18, 19,
            20, 21, 22, // bottom
            20, 22, 23,
        ];
        
        IReadOnlyList<int> vertexAttributeSizes = [3, 2, 1];
        
        _blockVao = new StaticModelVao(_gl, vertices, vertexAttributeSizes, indices);
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
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _blockShaderProgram.Use();
        _blockShaderProgram.SetUniform("textureSampler", 0);
        _time += (float)deltaTime;
        var rotationY = _time * 100 % 360;
        float yPos = MathF.Sin(_time);
        var model = Matrix4x4.CreateRotationY(float.DegreesToRadians(rotationY)) * Matrix4x4.CreateTranslation(0, yPos, -3f);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90), 1, 0.01f, 100f);
        _blockShaderProgram.SetUniform("model", model);
        _blockShaderProgram.SetUniform("view", Matrix4x4.Identity);
        _blockShaderProgram.SetUniform("projection", projection);
        _blockVao.Draw();
    }
}