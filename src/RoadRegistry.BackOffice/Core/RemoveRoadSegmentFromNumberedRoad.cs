namespace RoadRegistry.BackOffice.Core;

using System;
using Messages;

public class RemoveRoadSegmentFromNumberedRoad : IRequestedChange
{
    public RemoveRoadSegmentFromNumberedRoad(
        AttributeId attributeId,
        RoadSegmentId segmentId,
        NumberedRoadNumber number)
    {
        AttributeId = attributeId;
        SegmentId = segmentId;
        Number = number;
    }

    public AttributeId AttributeId { get; }
    public RoadSegmentId SegmentId { get; }
    public NumberedRoadNumber Number { get; }

    public Problems VerifyBefore(BeforeVerificationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var problems = Problems.None;

        if (!context.BeforeView.View.Segments.TryGetValue(SegmentId, out var segment))
        {
            problems = problems.Add(new RoadSegmentMissing(SegmentId));
        }
        else
        {
            if (!segment.PartOfNumberedRoads.Contains(Number)) problems = problems.Add(new NumberedRoadNumberNotFound(Number));
        }

        return problems;
    }

    public Problems VerifyAfter(AfterVerificationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        return Problems.None;
    }

    public void TranslateTo(Messages.AcceptedChange message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        message.RoadSegmentRemovedFromNumberedRoad = new RoadSegmentRemovedFromNumberedRoad
        {
            AttributeId = AttributeId,
            Number = Number,
            SegmentId = SegmentId
        };
    }

    public void TranslateTo(Messages.RejectedChange message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        message.RemoveRoadSegmentFromNumberedRoad = new Messages.RemoveRoadSegmentFromNumberedRoad
        {
            AttributeId = AttributeId,
            Number = Number,
            SegmentId = SegmentId
        };
    }
}
