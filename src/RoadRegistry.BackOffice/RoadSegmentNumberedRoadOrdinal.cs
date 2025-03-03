namespace RoadRegistry.BackOffice;

using System;

public readonly struct RoadSegmentNumberedRoadOrdinal : IEquatable<RoadSegmentNumberedRoadOrdinal>
{
    public struct WellKnownValues
    {
        public const int NotKnown = -8;
    }

    private readonly int _value;

    public RoadSegmentNumberedRoadOrdinal(int value)
    {
        if (value < 0 && value != WellKnownValues.NotKnown) throw new ArgumentOutOfRangeException(nameof(value), value, $"The road segment numbered road ordinal must be greater than or equal to zero, or the value '{WellKnownValues.NotKnown}' (Not Known)");

        _value = value;
    }

    public static bool Accepts(int value)
    {
        return value >= 0 || value == WellKnownValues.NotKnown;
    }

    public int ToInt32()
    {
        return _value;
    }

    public bool Equals(RoadSegmentNumberedRoadOrdinal other)
    {
        return _value == other._value;
    }

    public override bool Equals(object obj)
    {
        return obj is RoadSegmentNumberedRoadOrdinal revision && Equals(revision);
    }

    public override int GetHashCode()
    {
        return _value;
    }

    public override string ToString()
    {
        return _value.ToString();
    }

    public static bool operator ==(RoadSegmentNumberedRoadOrdinal left, RoadSegmentNumberedRoadOrdinal right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RoadSegmentNumberedRoadOrdinal left, RoadSegmentNumberedRoadOrdinal right)
    {
        return !left.Equals(right);
    }

    public static implicit operator int(RoadSegmentNumberedRoadOrdinal instance)
    {
        return instance._value;
    }
}
