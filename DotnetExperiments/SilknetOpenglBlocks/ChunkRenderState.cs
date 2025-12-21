namespace SilknetOpenglBlocks;

public record ChunkRenderState
{
    public required Chunk Chunk { get; init; }
    public required Vao Vao { get; init; }
    public bool ShouldRequestMeshing { get; set; } = true; 
}