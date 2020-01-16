namespace RoadRegistry.BackOffice.Translation
{
    using System;
    using System.Collections.Generic;

    public class AddRoadSegmentToNationalRoadEqualityComparer : IEqualityComparer<AddRoadSegmentToNationalRoad>
    {
        public bool Equals(AddRoadSegmentToNationalRoad left, AddRoadSegmentToNationalRoad right)
        {
            if (left == null && right == null) return true;
            if (left == null || right == null) return false;
            return left.Number.Equals(right.Number)
                   && left.SegmentId.Equals(right.SegmentId)
                   && left.TemporaryAttributeId.Equals(right.TemporaryAttributeId);
        }

        public int GetHashCode(AddRoadSegmentToNationalRoad instance)
        {
            throw new NotSupportedException();
        }
    }
}