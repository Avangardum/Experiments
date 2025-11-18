using AwesomeAssertions;

namespace SilknetOpenglBlocks;

public static class RangeConverter
{
    public static float ConvertFromFloatRangeToFloatRange(float oldValue, float oldRangeStart, float oldRangeEnd,
        float newRangeStart, float newRangeEnd)
    {
        oldValue.Should().BeInRange(oldRangeStart, oldRangeEnd);
        oldRangeEnd.Should().BeGreaterThan(oldRangeStart);
        newRangeEnd.Should().BeGreaterThan(newRangeStart);
        
        float valueIn0To1Range = (oldValue - oldRangeStart) / (oldRangeEnd - oldRangeStart);
        return valueIn0To1Range * (newRangeEnd - newRangeStart) + newRangeStart;
    }
    
    public static int ConvertFromFloatRangeToExclusiveIntRange(float oldValue, float oldRangeStart, float oldRangeEnd,
        int newRangeStart, int newRangeEnd)
    {
        float valueInFloatRange =
            ConvertFromFloatRangeToFloatRange(oldValue, oldRangeStart, oldRangeEnd, newRangeStart, newRangeEnd);
        return Math.Clamp((int)valueInFloatRange, newRangeStart, newRangeEnd - 1);
    }
    
    public static int ConvertFromFloatRangeToInclusiveIntRange(float oldValue, float oldRangeStart, float oldRangeEnd,
        int newRangeStart, int newRangeEnd)
    {
        return ConvertFromFloatRangeToExclusiveIntRange(oldValue, oldRangeStart, oldRangeEnd,
            newRangeStart, newRangeEnd + 1);
    }
}