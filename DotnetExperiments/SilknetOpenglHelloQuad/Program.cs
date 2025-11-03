using System.Drawing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace SilknetOpenglHelloQuad;

public static class Program
{
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static uint _shaderProgram;
    private static uint _quadVao;
    
    private static void Main()
    {
        WindowOptions options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "Hello Quad!"
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
        SetupQuadVao();
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
        const string vertexShaderSource =
            """
            #version 330 core
            
            layout (location = 0) in vec3 position;
            
            void main()
            {
                gl_Position = vec4(position, 1.0);
            }
            """;
        
        const string fragmentShaderSource =
            """
            #version 330 core
            
            out vec4 out_color;
            
            void main()
            {
                out_color = vec4(1.0, 0.5, 0.2, 1.0);
            }
            """;
        
        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);
        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vertexShaderCompileStatus);
        if (vertexShaderCompileStatus != 1)
            throw new Exception("Vertex shader failed to compile: " + _gl.GetShaderInfoLog(vertexShader));
        
        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentShaderSource);
        _gl.CompileShader(fragmentShader);
        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fragmentShaderCompileStatus);
        if (fragmentShaderCompileStatus != 1)
            throw new Exception("Fragment shader failed to compile: " + _gl.GetShaderInfoLog(fragmentShader));
        
        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vertexShader);
        _gl.AttachShader(_shaderProgram, fragmentShader);
        _gl.LinkProgram(_shaderProgram);
        _gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus != 1)
            throw new Exception("Shader program failed to link: " + _gl.GetProgramInfoLog(_shaderProgram));
        
        _gl.DetachShader(_shaderProgram, vertexShader);
        _gl.DetachShader(_shaderProgram, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
    }
    
    private static void SetupQuadVao()
    {
        _quadVao = _gl.GenVertexArray();
        _gl.BindVertexArray(_quadVao);
        
        float[] vertices =
        [
             0.5f,  0.5f, 0.0f,
             0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.0f
        ];
        
        uint vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices.AsSpan(), BufferUsageARB.StaticDraw);
        
        uint[] indices =
        [
            0u, 1u, 3u,
            1u, 2u, 3u
        ];
        uint ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices.AsSpan(), BufferUsageARB.StaticDraw);
        
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        
        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape) _window.Close();
    }
    
    private static void OnUpdate(double deltaTime)
    {
        
    }
    
    private static unsafe void OnRender(double deltaTime)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        _gl.BindVertexArray(_quadVao);
        _gl.UseProgram(_shaderProgram);
        _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
    }
}