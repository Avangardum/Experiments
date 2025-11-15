using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Camera
{
    public Vector3D<float> Position { get; set; } = new(20, 20, 20);

    public float YawDeg { get; set; } = -90;
    public float PitchDeg { get; set; }

    public Vector3D<float> Front
    {
        get
        {
            float Sin(float it) => float.Sin(float.DegreesToRadians(it));
            float Cos(float it) => float.Cos(float.DegreesToRadians(it));
            return Vector3D.Normalize
            (
                new Vector3D<float>
                (
                    Cos(YawDeg) * Cos(PitchDeg),
                    Sin(PitchDeg),
                    Sin(YawDeg) * Cos(PitchDeg)
                )
            );
        }
    }
    
    public Vector3D<float> Up => Vector3D<float>.UnitY;

    public Matrix4X4<float> ViewMatrix => Matrix4X4.CreateLookAt(Position, Position + Front, Vector3D<float>.UnitY);
}