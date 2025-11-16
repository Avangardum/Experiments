namespace SilknetOpenglBlocks;

public static class DirectionExtensions
{
    public static float GetLightLevel(this Direction direction)
    {
        return
            direction == Direction.Forward ? 0.5f :
            direction == Direction.Back ? 0.9f :
            direction == Direction.Right ? 0.8f :
            direction == Direction.Left ? 0.6f :
            direction == Direction.Up ? 1.0f :
            direction == Direction.Down ? 0.2f :
            throw new ArgumentOutOfRangeException();
    }
}