using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilknetOpenglBlocks;

Renderer renderer = null!;
InputHandler inputHandler = null!;
Camera camera = null!;
Game game = null!;

IWindow window = CreateWindow();

window.Load += OnLoad;
window.Update += OnUpdate;
window.Render += OnRender;

window.Run();

IWindow CreateWindow()
{
    return Window.Create(WindowOptions.Default with
    {
        Size = new Vector2D<int>(1200, 1200),
        Title = "Silk.NET OpenGL Blocks"
    });
}

void OnLoad()
{
    GL gl = window.CreateOpenGL();
    IInputContext input = window.CreateInput();
    
    camera = new Camera();
    game = new Game();
    renderer = new Renderer(gl, game, camera);
    inputHandler = new InputHandler(input, camera);
}

void OnUpdate(double deltaTime)
{
    inputHandler.ProcessInput(deltaTime);
}

void OnRender(double deltaTime)
{
    renderer.Render(deltaTime);
}