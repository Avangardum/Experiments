using Silk.NET.Maths;

namespace SilknetOpenglBlocks;

public sealed class Camera(Game game)
{
    public Vector3D<float> Position { get; set; } = new(20, 70, 20);

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
    
    private const float RaycastStep = 0.01f;
    private const float InteractionDistance = 3f;
    
    public void BreakBlock()
    {
        if (!BlockRaycast(out Vector3D<int> hitBlockPos, out Vector3D<int> prevBlockPos)) return;
        game.SetBlock(hitBlockPos, Block.Air);
    }
    
    public void PlaceBlock()
    {
        if (!BlockRaycast(out Vector3D<int> hitBlockPos, out Vector3D<int> prevBlockPos)) return;
        if (game.GetBlock(prevBlockPos) != Block.Air) return;
        game.SetBlock(prevBlockPos, Block.Brick);
    }
    
    private bool BlockRaycast(out Vector3D<int> hitBlockPos, out Vector3D<int> prevBlockPos)
    {
        Vector3D<float> raycastStepVec = Front * RaycastStep;
        float rayLength = 0f;
        Vector3D<float> rayEnd = Position;
        while (rayLength <= InteractionDistance)
        {
            if (game.GetBlock(rayEnd) != Block.Air)
            {
                hitBlockPos = rayEnd.RoundToInt();
                prevBlockPos = (rayEnd - raycastStepVec).RoundToInt();
                // Ensure that prevBlockPos only differs in a single coordinate
                if (prevBlockPos.X != hitBlockPos.X)
                {
                    prevBlockPos.Y = hitBlockPos.Y;
                    prevBlockPos.Z = hitBlockPos.Z;
                }
                else if (prevBlockPos.Y != hitBlockPos.Y)
                {
                    prevBlockPos.Z = hitBlockPos.Z;
                }
                return true;
            }
            rayEnd += raycastStepVec;
            rayLength += RaycastStep;
        }
        hitBlockPos = Vector3D<int>.Zero;
        prevBlockPos = Vector3D<int>.Zero;
        return false;
    }
}