using System.Collections.Immutable;
using AwesomeAssertions;
using Silk.NET.Maths;

namespace SilknetOpenglBlocks.Tests;

public sealed class ChunkPositionConversionTests
{
    public static readonly object[][] WorldPosChunkIndexChunkPosValues =
        [
            [0, 0, 0],
            [-1, -1, 15],
            [20, 1, 4],
            [15, 0, 15],
            [16, 1, 0],
            [17, 1, 1],
            [-15, -1, 1],
            [-16, -1, 0],
            [-17, -2, 15],
            [63, 3, 15],
            [64, 4, 0],
            [65, 4, 1],
            [-63, -4, 1],
            [-64, -4, 0],
            [-65, -5, 15]
        ];
    
    [Theory]
    [MemberData(nameof(WorldPosChunkIndexChunkPosValues))]
    public void ConvertsWorldPosToChunkIndexAndChunkPos(int worldPos, int chunkIndex, int chunkPos)
    {
        Chunk.WorldPosToChunkIndex(worldPos).Should().Be(chunkIndex);
        Chunk.WorldPosToChunkPos(worldPos).Should().Be(chunkPos);
    }
}