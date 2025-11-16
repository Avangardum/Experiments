namespace SilknetOpenglBlocks;

public static class BlockExtensions
{
    public static bool IsOpaque(this Block block) => block != Block.Air;
    
    public static bool IsVisible(this Block block) => block != Block.Air;
}