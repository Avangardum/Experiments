using System.Drawing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace SilknetOpenglBlocks;

public static class Program
{
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static ShaderProgram _shaderProgram = null!;
    private static StaticModelVao _blockVao;
    
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
        _shaderProgram = new ShaderProgram(_gl, "Block");
    }
    
    private static void SetupBlockVao()
    {
        ReadOnlySpan<float> vertices =
        [
             0.5f,  0.5f, 0.0f,
             0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.0f
        ];
        
        ReadOnlySpan<uint> indices =
        [
            0u, 1u, 3u,
            1u, 2u, 3u
        ];
        
        ReadOnlySpan<int> vertexAttributeSizes = [3];
        
        _blockVao = new StaticModelVao(_gl, vertices, vertexAttributeSizes, indices);
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
        _blockVao.Draw(_shaderProgram);
    }
}