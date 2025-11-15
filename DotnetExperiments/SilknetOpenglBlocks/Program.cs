using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilknetOpenglBlocks;

WindowOptions options = WindowOptions.Default with
{
    Size = new Vector2D<int>(1200, 1200),
    Title = "Silk.NET OpenGL Blocks"
};
var window = Window.Create(options);
Game game = new(window);
Camera camera = new();
Renderer renderer = new(window, game, camera);
window.Run();