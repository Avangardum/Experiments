namespace SilknetOpenglBlocks;

public static class BlockExtensions
{
    public static bool IsOpaque(this Block block) => block != Block.Air;
}