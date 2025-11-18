using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace SilknetOpenglBlocks;

public sealed class InputHandler
{
    private readonly IInputContext _input;
    private readonly Camera _camera;
    private readonly Renderer _renderer;
    private Vector2? _lastMousePosition;

    public InputHandler(IInputContext input, Camera camera, Renderer renderer)
    {
        _input = input;
        _camera = camera;
        _renderer = renderer;

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

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        _lastMousePosition ??= position;
        Vector2 delta = position - _lastMousePosition.Value;
        const float sensitivity = 0.05f;
        _camera.YawDeg += delta.X * sensitivity;
        _camera.PitchDeg -= delta.Y * sensitivity;
        _camera.PitchDeg = float.Clamp(_camera.PitchDeg, -89, 89);
        _lastMousePosition = position;
    }
    
    private bool IsKeyPressed(Key key) => _input.Keyboards.Any(x => x.IsKeyPressed(key));
    
    private void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape) Environment.Exit(0);
        if (key == Key.F1) _renderer.ToggleWireframe();
        if (key == Key.F2) _renderer.PrintState();
    }

    public void ProcessInput(double deltaTime)
    {
        const float speed = 10;
        if (IsKeyPressed(Key.A)) _camera.Position -= Vector3D.Normalize(Vector3D.Cross(_camera.Front, _camera.Up)) * (float)deltaTime * speed; 
        if (IsKeyPressed(Key.D)) _camera.Position += Vector3D.Normalize(Vector3D.Cross(_camera.Front, _camera.Up)) * (float)deltaTime * speed;
        if (IsKeyPressed(Key.W)) _camera.Position += _camera.Front * (float)deltaTime * speed;
        if (IsKeyPressed(Key.S)) _camera.Position -= _camera.Front * (float)deltaTime * speed;
        if (IsKeyPressed(Key.Space)) _camera.Position += _camera.Up * (float)deltaTime * speed;
        if (IsKeyPressed(Key.ShiftLeft)) _camera.Position -= _camera.Up * (float)deltaTime * speed;
    }
}