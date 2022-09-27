namespace RoadRegistry.BackOffice.Core;

public class IntersectingRoadSegmentsDoNotHaveGradeSeparatedJunction : Error
{
    public IntersectingRoadSegmentsDoNotHaveGradeSeparatedJunction(RoadSegmentId modifiedRoadSegmentId, RoadSegmentId intersectingRoadSegmentId)
        : base(nameof(IntersectingRoadSegmentsDoNotHaveGradeSeparatedJunction),
            new ProblemParameter(nameof(modifiedRoadSegmentId), modifiedRoadSegmentId.ToString()),
            new ProblemParameter(nameof(intersectingRoadSegmentId), intersectingRoadSegmentId.ToString()))
    {
    }
}
