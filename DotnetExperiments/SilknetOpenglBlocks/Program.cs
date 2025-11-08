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
    
    private static void Main()
    {
        WindowOptions options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(800, 600),
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
//           x      y      z     u   v   light
            -0.5f, -0.5f,  0.5f, 0f, 0f, 0.2f,  // 0  front
            -0.5f,  0.5f,  0.5f, 1f, 0f, 0.5f,  // 1
             0.5f,  0.5f,  0.5f, 1f, 1f, 1.0f,  // 2
             0.5f, -0.5f,  0.5f, 0f, 1f, 0.8f,  // 3
        ];
        
        IReadOnlyList<uint> indices =
        [
            0, 1, 2,
            0, 2, 3
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
        _blockShaderProgram.SetUniform("model", Matrix4x4.Identity);
        _blockShaderProgram.SetUniform("view", Matrix4x4.Identity);
        _blockShaderProgram.SetUniform("projection", Matrix4x4.Identity);
        _blockVao.Draw();
    }
}