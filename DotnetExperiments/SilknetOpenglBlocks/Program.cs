using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilknetOpenglBlocks;

WindowOptions options = WindowOptions.Default with
{
    Size = new Vector2D<int>(1200, 1200),
    Title = "Silk.NET OpenGL Blocks"
};
var window = Window.Create(options);
Camera camera = new();
Game game = new(window, camera);
Renderer renderer = new(window, game, camera);
InputHandler inputHandler = new(camera, window);
window.Run();