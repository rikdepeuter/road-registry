namespace RoadRegistry.BackOffice.Core;

using System;
using Messages;

public class RemoveRoadSegmentFromEuropeanRoad : IRequestedChange
{
    public RemoveRoadSegmentFromEuropeanRoad(
        AttributeId attributeId,
        RoadSegmentId segmentId,
        EuropeanRoadNumber number)
    {
        AttributeId = attributeId;
        SegmentId = segmentId;
        Number = number;
    }

    public AttributeId AttributeId { get; }
    public RoadSegmentId SegmentId { get; }
    public EuropeanRoadNumber Number { get; }

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
            if (!segment.PartOfEuropeanRoads.Contains(Number)) problems = problems.Add(new EuropeanRoadNumberNotFound(Number));
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

        message.RoadSegmentRemovedFromEuropeanRoad = new RoadSegmentRemovedFromEuropeanRoad
        {
            AttributeId = AttributeId,
            Number = Number,
            SegmentId = SegmentId
        };
    }

    public void TranslateTo(Messages.RejectedChange message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        message.RemoveRoadSegmentFromEuropeanRoad = new Messages.RemoveRoadSegmentFromEuropeanRoad
        {
            AttributeId = AttributeId,
            Number = Number,
            SegmentId = SegmentId
        };
    }
}
