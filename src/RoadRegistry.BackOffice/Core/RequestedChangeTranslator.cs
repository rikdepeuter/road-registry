namespace RoadRegistry.BackOffice.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Messages;

internal class RequestedChangeTranslator
{
    private readonly Func<AttributeId> _nextEuropeanRoadAttributeId;
    private readonly Func<GradeSeparatedJunctionId> _nextGradeSeparatedJunctionId;
    private readonly Func<AttributeId> _nextNationalRoadAttributeId;
    private readonly Func<AttributeId> _nextNumberedRoadAttributeId;
    private readonly Func<RoadNodeId> _nextRoadNodeId;
    private readonly Func<RoadSegmentId> _nextRoadSegmentId;
    private readonly Func<RoadSegmentId, Func<AttributeId>> _nextRoadSegmentLaneAttributeId;
    private readonly Func<RoadSegmentId, Func<AttributeId>> _nextRoadSegmentSurfaceAttributeId;
    private readonly Func<RoadSegmentId, Func<AttributeId>> _nextRoadSegmentWidthAttributeId;
    private readonly Func<TransactionId> _nextTransactionId;

    public RequestedChangeTranslator(
        Func<TransactionId> nextTransactionId,
        Func<RoadNodeId> nextRoadNodeId,
        Func<RoadSegmentId> nextRoadSegmentId,
        Func<GradeSeparatedJunctionId> nextGradeSeparatedJunctionId,
        Func<AttributeId> nextEuropeanRoadAttributeId,
        Func<AttributeId> nextNationalRoadAttributeId,
        Func<AttributeId> nextNumberedRoadAttributeId,
        Func<RoadSegmentId, Func<AttributeId>> nextRoadSegmentLaneAttributeId,
        Func<RoadSegmentId, Func<AttributeId>> nextRoadSegmentWidthAttributeId,
        Func<RoadSegmentId, Func<AttributeId>> nextRoadSegmentSurfaceAttributeId)
    {
        _nextTransactionId =
            nextTransactionId ?? throw new ArgumentNullException(nameof(nextTransactionId));
        _nextRoadNodeId =
            nextRoadNodeId ?? throw new ArgumentNullException(nameof(nextRoadNodeId));
        _nextRoadSegmentId =
            nextRoadSegmentId ?? throw new ArgumentNullException(nameof(nextRoadSegmentId));
        _nextGradeSeparatedJunctionId =
            nextGradeSeparatedJunctionId ?? throw new ArgumentNullException(nameof(nextGradeSeparatedJunctionId));
        _nextEuropeanRoadAttributeId =
            nextEuropeanRoadAttributeId ?? throw new ArgumentNullException(nameof(nextEuropeanRoadAttributeId));
        _nextNationalRoadAttributeId =
            nextNationalRoadAttributeId ?? throw new ArgumentNullException(nameof(nextNationalRoadAttributeId));
        _nextNumberedRoadAttributeId =
            nextNumberedRoadAttributeId ?? throw new ArgumentNullException(nameof(nextNumberedRoadAttributeId));
        _nextRoadSegmentLaneAttributeId =
            nextRoadSegmentLaneAttributeId ?? throw new ArgumentNullException(nameof(nextRoadSegmentLaneAttributeId));
        _nextRoadSegmentWidthAttributeId =
            nextRoadSegmentWidthAttributeId ?? throw new ArgumentNullException(nameof(nextRoadSegmentWidthAttributeId));
        _nextRoadSegmentSurfaceAttributeId =
            nextRoadSegmentSurfaceAttributeId ?? throw new ArgumentNullException(nameof(nextRoadSegmentSurfaceAttributeId));
    }

    public async Task<RequestedChanges> Translate(IReadOnlyCollection<RequestedChange> changes, IOrganizations organizations, CancellationToken ct = default)
    {
        if (changes == null)
            throw new ArgumentNullException(nameof(changes));
        if (organizations == null)
            throw new ArgumentNullException(nameof(organizations));

        var translated = RequestedChanges.Start(_nextTransactionId());
        foreach (var change in changes.Flatten()
                     .Select((change, ordinal) => new SortableChange(change, ordinal))
                     .OrderBy(_ => _, new RankChangeBeforeTranslation())
                     .Select(_ => _.Change))
            switch (change)
            {
                case Messages.AddRoadNode command:
                    translated = translated.Append(Translate(command));
                    break;
                case Messages.ModifyRoadNode command:
                    translated = translated.Append(Translate(command));
                    break;
                case Messages.RemoveRoadNode command:
                    translated = translated.Append(Translate(command));
                    break;
                case Messages.AddRoadSegment command:
                    translated = translated.Append(await Translate(command, translated, organizations, ct));
                    break;
                case Messages.ModifyRoadSegment command:
                    translated = translated.Append(await Translate(command, translated, organizations, ct));
                    break;
                case Messages.RemoveRoadSegment command:
                    translated = translated.Append(Translate(command));
                    break;
                case Messages.AddRoadSegmentToEuropeanRoad command:
                    translated = translated.Append(Translate(command, translated));
                    break;
                case Messages.RemoveRoadSegmentFromEuropeanRoad command:
                    translated = translated.Append(Translate(command));
                    break;
                case Messages.AddRoadSegmentToNationalRoad command:
                    translated = translated.Append(Translate(command, translated));
                    break;
                case Messages.RemoveRoadSegmentFromNationalRoad command:
                    translated = translated.Append(Translate(command, translated));
                    break;
                case Messages.AddRoadSegmentToNumberedRoad command:
                    translated = translated.Append(Translate(command, translated));
                    break;
                case Messages.ModifyRoadSegmentOnNumberedRoad command:
                    translated = translated.Append(Translate(command));
                    break;
                case Messages.RemoveRoadSegmentFromNumberedRoad command:
                    translated = translated.Append(Translate(command, translated));
                    break;
                case Messages.AddGradeSeparatedJunction command:
                    translated = translated.Append(Translate(command, translated));
                    break;
                case Messages.ModifyGradeSeparatedJunction command:
                    translated = translated.Append(Translate(command, translated));
                    break;
                case Messages.RemoveGradeSeparatedJunction command:
                    translated = translated.Append(Translate(command));
                    break;
            }

        return translated;
    }

    private AddRoadNode Translate(Messages.AddRoadNode command)
    {
        var permanent = _nextRoadNodeId();
        var temporary = new RoadNodeId(command.TemporaryId);
        return new AddRoadNode
        (
            permanent,
            temporary,
            RoadNodeType.Parse(command.Type),
            GeometryTranslator.Translate(command.Geometry)
        );
    }

    private ModifyRoadNode Translate(Messages.ModifyRoadNode command)
    {
        var permanent = new RoadNodeId(command.Id);
        return new ModifyRoadNode
        (
            permanent,
            RoadNodeType.Parse(command.Type),
            GeometryTranslator.Translate(command.Geometry)
        );
    }

    private RemoveRoadNode Translate(Messages.RemoveRoadNode command)
    {
        var permanent = new RoadNodeId(command.Id);
        return new RemoveRoadNode
        (
            permanent
        );
    }

    private async Task<AddRoadSegment> Translate(Messages.AddRoadSegment command, IRequestedChangeIdentityTranslator translator, IOrganizations organizations, CancellationToken ct)
    {
        var permanent = _nextRoadSegmentId();
        var temporary = new RoadSegmentId(command.TemporaryId);

        var startNodeId = new RoadNodeId(command.StartNodeId);
        RoadNodeId? temporaryStartNodeId;
        if (translator.TryTranslateToPermanent(startNodeId, out var permanentStartNodeId))
        {
            temporaryStartNodeId = startNodeId;
            startNodeId = permanentStartNodeId;
        }
        else
        {
            temporaryStartNodeId = null;
        }

        var endNodeId = new RoadNodeId(command.EndNodeId);
        RoadNodeId? temporaryEndNodeId;
        if (translator.TryTranslateToPermanent(endNodeId, out var permanentEndNodeId))
        {
            temporaryEndNodeId = endNodeId;
            endNodeId = permanentEndNodeId;
        }
        else
        {
            temporaryEndNodeId = null;
        }

        var geometry = GeometryTranslator.Translate(command.Geometry);
        var maintainerId = new OrganizationId(command.MaintenanceAuthority);
        var maintainer = await organizations.TryGet(maintainerId, ct);
        var geometryDrawMethod = RoadSegmentGeometryDrawMethod.Parse(command.GeometryDrawMethod);
        var morphology = RoadSegmentMorphology.Parse(command.Morphology);
        var status = RoadSegmentStatus.Parse(command.Status);
        var category = RoadSegmentCategory.Parse(command.Category);
        var accessRestriction = RoadSegmentAccessRestriction.Parse(command.AccessRestriction);
        var leftSideStreetNameId = command.LeftSideStreetNameId.HasValue
            ? new CrabStreetnameId(command.LeftSideStreetNameId.Value)
            : new CrabStreetnameId?();
        var rightSideStreetNameId = command.RightSideStreetNameId.HasValue
            ? new CrabStreetnameId(command.RightSideStreetNameId.Value)
            : new CrabStreetnameId?();
        var nextLaneAttributeId = _nextRoadSegmentLaneAttributeId(permanent);
        var laneAttributes = Array.ConvertAll(
            command.Lanes,
            item => new RoadSegmentLaneAttribute(
                nextLaneAttributeId(),
                new AttributeId(item.AttributeId),
                new RoadSegmentLaneCount(item.Count),
                RoadSegmentLaneDirection.Parse(item.Direction),
                new RoadSegmentPosition(item.FromPosition),
                new RoadSegmentPosition(item.ToPosition),
                new GeometryVersion(0)
            )
        );
        var nextWidthAttributeId = _nextRoadSegmentWidthAttributeId(permanent);
        var widthAttributes = Array.ConvertAll(
            command.Widths,
            item => new RoadSegmentWidthAttribute(
                nextWidthAttributeId(),
                new AttributeId(item.AttributeId),
                new RoadSegmentWidth(item.Width),
                new RoadSegmentPosition(item.FromPosition),
                new RoadSegmentPosition(item.ToPosition),
                new GeometryVersion(0)
            )
        );
        var nextSurfaceAttributeId = _nextRoadSegmentSurfaceAttributeId(permanent);
        var surfaceAttributes = Array.ConvertAll(
            command.Surfaces,
            item => new RoadSegmentSurfaceAttribute(
                nextSurfaceAttributeId(),
                new AttributeId(item.AttributeId),
                RoadSegmentSurfaceType.Parse(item.Type),
                new RoadSegmentPosition(item.FromPosition),
                new RoadSegmentPosition(item.ToPosition),
                new GeometryVersion(0)
            )
        );

        return new AddRoadSegment
        (
            permanent,
            temporary,
            startNodeId,
            temporaryStartNodeId,
            endNodeId,
            temporaryEndNodeId,
            geometry,
            maintainerId,
            maintainer?.Translation.Name,
            geometryDrawMethod,
            morphology,
            status,
            category,
            accessRestriction,
            leftSideStreetNameId,
            rightSideStreetNameId,
            laneAttributes,
            widthAttributes,
            surfaceAttributes
        );
    }

    private async Task<ModifyRoadSegment> Translate(Messages.ModifyRoadSegment command, IRequestedChangeIdentityTranslator translator, IOrganizations organizations, CancellationToken ct)
    {
        var permanent = new RoadSegmentId(command.Id);

        var startNodeId = new RoadNodeId(command.StartNodeId);
        RoadNodeId? temporaryStartNodeId;
        if (translator.TryTranslateToPermanent(startNodeId, out var permanentStartNodeId))
        {
            temporaryStartNodeId = startNodeId;
            startNodeId = permanentStartNodeId;
        }
        else
        {
            temporaryStartNodeId = null;
        }

        var endNodeId = new RoadNodeId(command.EndNodeId);
        RoadNodeId? temporaryEndNodeId;
        if (translator.TryTranslateToPermanent(endNodeId, out var permanentEndNodeId))
        {
            temporaryEndNodeId = endNodeId;
            endNodeId = permanentEndNodeId;
        }
        else
        {
            temporaryEndNodeId = null;
        }

        var geometry = GeometryTranslator.Translate(command.Geometry);
        var maintainerId = new OrganizationId(command.MaintenanceAuthority);
        var maintainer = await organizations.TryGet(maintainerId, ct);
        var geometryDrawMethod = RoadSegmentGeometryDrawMethod.Parse(command.GeometryDrawMethod);
        var morphology = RoadSegmentMorphology.Parse(command.Morphology);
        var status = RoadSegmentStatus.Parse(command.Status);
        var category = RoadSegmentCategory.Parse(command.Category);
        var accessRestriction = RoadSegmentAccessRestriction.Parse(command.AccessRestriction);
        var leftSideStreetNameId = command.LeftSideStreetNameId.HasValue
            ? new CrabStreetnameId(command.LeftSideStreetNameId.Value)
            : new CrabStreetnameId?();
        var rightSideStreetNameId = command.RightSideStreetNameId.HasValue
            ? new CrabStreetnameId(command.RightSideStreetNameId.Value)
            : new CrabStreetnameId?();
        var nextLaneAttributeId = _nextRoadSegmentLaneAttributeId(permanent);
        var laneAttributes = Array.ConvertAll(
            command.Lanes,
            item => new RoadSegmentLaneAttribute(
                nextLaneAttributeId(),
                new AttributeId(item.AttributeId),
                new RoadSegmentLaneCount(item.Count),
                RoadSegmentLaneDirection.Parse(item.Direction),
                new RoadSegmentPosition(item.FromPosition),
                new RoadSegmentPosition(item.ToPosition),
                new GeometryVersion(0)
            )
        );
        var nextWidthAttributeId = _nextRoadSegmentWidthAttributeId(permanent);
        var widthAttributes = Array.ConvertAll(
            command.Widths,
            item => new RoadSegmentWidthAttribute(
                nextWidthAttributeId(),
                new AttributeId(item.AttributeId),
                new RoadSegmentWidth(item.Width),
                new RoadSegmentPosition(item.FromPosition),
                new RoadSegmentPosition(item.ToPosition),
                new GeometryVersion(0)
            )
        );
        var nextSurfaceAttributeId = _nextRoadSegmentSurfaceAttributeId(permanent);
        var surfaceAttributes = Array.ConvertAll(
            command.Surfaces,
            item => new RoadSegmentSurfaceAttribute(
                nextSurfaceAttributeId(),
                new AttributeId(item.AttributeId),
                RoadSegmentSurfaceType.Parse(item.Type),
                new RoadSegmentPosition(item.FromPosition),
                new RoadSegmentPosition(item.ToPosition),
                new GeometryVersion(0)
            )
        );

        return new ModifyRoadSegment
        (
            permanent,
            startNodeId,
            temporaryStartNodeId,
            endNodeId,
            temporaryEndNodeId,
            geometry,
            maintainerId,
            maintainer?.Translation.Name,
            geometryDrawMethod,
            morphology,
            status,
            category,
            accessRestriction,
            leftSideStreetNameId,
            rightSideStreetNameId,
            laneAttributes,
            widthAttributes,
            surfaceAttributes
        );
    }

    private RemoveRoadSegment Translate(Messages.RemoveRoadSegment command)
    {
        var permanent = new RoadSegmentId(command.Id);
        return new RemoveRoadSegment
        (
            permanent
        );
    }

    private AddRoadSegmentToEuropeanRoad Translate(Messages.AddRoadSegmentToEuropeanRoad command, IRequestedChangeIdentityTranslator translator)
    {
        var permanent = _nextEuropeanRoadAttributeId();
        var temporary = new AttributeId(command.TemporaryAttributeId);

        var segmentId = new RoadSegmentId(command.SegmentId);
        RoadSegmentId? temporarySegmentId;
        if (translator.TryTranslateToPermanent(segmentId, out var permanentSegmentId))
        {
            temporarySegmentId = segmentId;
            segmentId = permanentSegmentId;
        }
        else
        {
            temporarySegmentId = null;
        }

        var number = EuropeanRoadNumber.Parse(command.Number);
        return new AddRoadSegmentToEuropeanRoad
        (
            permanent,
            temporary,
            segmentId,
            temporarySegmentId,
            number
        );
    }

    private RemoveRoadSegmentFromEuropeanRoad Translate(Messages.RemoveRoadSegmentFromEuropeanRoad command)
    {
        var permanent = new AttributeId(command.AttributeId);
        var segmentId = new RoadSegmentId(command.SegmentId);

        var number = EuropeanRoadNumber.Parse(command.Number);
        return new RemoveRoadSegmentFromEuropeanRoad
        (
            permanent,
            segmentId,
            number
        );
    }

    private AddRoadSegmentToNationalRoad Translate(Messages.AddRoadSegmentToNationalRoad command, IRequestedChangeIdentityTranslator translator)
    {
        var permanent = _nextNationalRoadAttributeId();
        var temporary = new AttributeId(command.TemporaryAttributeId);

        var segmentId = new RoadSegmentId(command.SegmentId);
        RoadSegmentId? temporarySegmentId;
        if (translator.TryTranslateToPermanent(segmentId, out var permanentSegmentId))
        {
            temporarySegmentId = segmentId;
            segmentId = permanentSegmentId;
        }
        else
        {
            temporarySegmentId = null;
        }

        var number = NationalRoadNumber.Parse(command.Number);
        return new AddRoadSegmentToNationalRoad
        (
            permanent,
            temporary,
            segmentId,
            temporarySegmentId,
            number
        );
    }

    private RemoveRoadSegmentFromNationalRoad Translate(Messages.RemoveRoadSegmentFromNationalRoad command, IRequestedChangeIdentityTranslator translator)
    {
        var permanent = new AttributeId(command.AttributeId);
        var segmentId = new RoadSegmentId(command.SegmentId);
        var number = NationalRoadNumber.Parse(command.Number);

        return new RemoveRoadSegmentFromNationalRoad
        (
            permanent,
            segmentId,
            number
        );
    }

    private AddRoadSegmentToNumberedRoad Translate(Messages.AddRoadSegmentToNumberedRoad command, IRequestedChangeIdentityTranslator translator)
    {
        var permanent = _nextNumberedRoadAttributeId();
        var temporary = new AttributeId(command.TemporaryAttributeId);

        var segmentId = new RoadSegmentId(command.SegmentId);
        RoadSegmentId? temporarySegmentId;
        if (translator.TryTranslateToPermanent(segmentId, out var permanentSegmentId))
        {
            temporarySegmentId = segmentId;
            segmentId = permanentSegmentId;
        }
        else
        {
            temporarySegmentId = null;
        }

        var number = NumberedRoadNumber.Parse(command.Number);
        var direction = RoadSegmentNumberedRoadDirection.Parse(command.Direction);
        var ordinal = new RoadSegmentNumberedRoadOrdinal(command.Ordinal);
        return new AddRoadSegmentToNumberedRoad
        (
            permanent,
            temporary,
            segmentId,
            temporarySegmentId,
            number,
            direction,
            ordinal
        );
    }

    private ModifyRoadSegmentOnNumberedRoad Translate(Messages.ModifyRoadSegmentOnNumberedRoad command)
    {
        var permanent = new AttributeId(command.AttributeId);
        var segmentId = new RoadSegmentId(command.SegmentId);
        var number = NumberedRoadNumber.Parse(command.Number);
        var direction = RoadSegmentNumberedRoadDirection.Parse(command.Direction);
        var ordinal = new RoadSegmentNumberedRoadOrdinal(command.Ordinal);
        return new ModifyRoadSegmentOnNumberedRoad
        (
            permanent,
            segmentId,
            number,
            direction,
            ordinal
        );
    }

    private RemoveRoadSegmentFromNumberedRoad Translate(Messages.RemoveRoadSegmentFromNumberedRoad command, IRequestedChangeIdentityTranslator translator)
    {
        var permanent = new AttributeId(command.AttributeId);
        var segmentId = new RoadSegmentId(command.SegmentId);
        var number = NumberedRoadNumber.Parse(command.Number);

        return new RemoveRoadSegmentFromNumberedRoad
        (
            permanent,
            segmentId,
            number
        );
    }

    private AddGradeSeparatedJunction Translate(Messages.AddGradeSeparatedJunction command, IRequestedChangeIdentityTranslator translator)
    {
        var permanent = _nextGradeSeparatedJunctionId();
        var temporary = new GradeSeparatedJunctionId(command.TemporaryId);

        var upperSegmentId = new RoadSegmentId(command.UpperSegmentId);
        RoadSegmentId? temporaryUpperSegmentId;
        if (translator.TryTranslateToPermanent(upperSegmentId, out var permanentUpperSegmentId))
        {
            temporaryUpperSegmentId = upperSegmentId;
            upperSegmentId = permanentUpperSegmentId;
        }
        else
        {
            temporaryUpperSegmentId = null;
        }

        var lowerSegmentId = new RoadSegmentId(command.LowerSegmentId);
        RoadSegmentId? temporaryLowerSegmentId;
        if (translator.TryTranslateToPermanent(lowerSegmentId, out var permanentLowerSegmentId))
        {
            temporaryLowerSegmentId = lowerSegmentId;
            lowerSegmentId = permanentLowerSegmentId;
        }
        else
        {
            temporaryLowerSegmentId = null;
        }

        return new AddGradeSeparatedJunction(
            permanent,
            temporary,
            GradeSeparatedJunctionType.Parse(command.Type),
            upperSegmentId,
            temporaryUpperSegmentId,
            lowerSegmentId,
            temporaryLowerSegmentId);
    }

    private ModifyGradeSeparatedJunction Translate(Messages.ModifyGradeSeparatedJunction command, IRequestedChangeIdentityTranslator translator)
    {
        var permanent = new GradeSeparatedJunctionId(command.Id);

        var upperSegmentId = new RoadSegmentId(command.UpperSegmentId);
        RoadSegmentId? temporaryUpperSegmentId;
        if (translator.TryTranslateToPermanent(upperSegmentId, out var permanentUpperSegmentId))
        {
            temporaryUpperSegmentId = upperSegmentId;
            upperSegmentId = permanentUpperSegmentId;
        }
        else
        {
            temporaryUpperSegmentId = null;
        }

        var lowerSegmentId = new RoadSegmentId(command.LowerSegmentId);
        RoadSegmentId? temporaryLowerSegmentId;
        if (translator.TryTranslateToPermanent(lowerSegmentId, out var permanentLowerSegmentId))
        {
            temporaryLowerSegmentId = lowerSegmentId;
            lowerSegmentId = permanentLowerSegmentId;
        }
        else
        {
            temporaryLowerSegmentId = null;
        }

        return new ModifyGradeSeparatedJunction(
            permanent,
            GradeSeparatedJunctionType.Parse(command.Type),
            upperSegmentId,
            temporaryUpperSegmentId,
            lowerSegmentId,
            temporaryLowerSegmentId);
    }

    private RemoveGradeSeparatedJunction Translate(Messages.RemoveGradeSeparatedJunction command)
    {
        var permanent = new GradeSeparatedJunctionId(command.Id);

        return new RemoveGradeSeparatedJunction(permanent);
    }

    private sealed class SortableChange
    {
        public SortableChange(object change, int ordinal)
        {
            Ordinal = ordinal;
            Change = change;
        }

        public int Ordinal { get; }
        public object Change { get; }
    }

    private sealed class RankChangeBeforeTranslation : IComparer<SortableChange>
    {
        private static readonly Type[] SequenceByTypeOfChange =
        {
            typeof(Messages.AddRoadNode),
            typeof(Messages.AddRoadSegment),
            typeof(Messages.AddRoadSegmentToEuropeanRoad),
            typeof(Messages.AddRoadSegmentToNationalRoad),
            typeof(Messages.AddRoadSegmentToNumberedRoad),
            typeof(Messages.AddGradeSeparatedJunction),
            typeof(Messages.ModifyRoadNode),
            typeof(Messages.ModifyRoadSegment),
            typeof(Messages.ModifyGradeSeparatedJunction),
            typeof(Messages.RemoveRoadSegmentFromEuropeanRoad),
            typeof(Messages.RemoveRoadSegmentFromNationalRoad),
            typeof(Messages.RemoveRoadSegmentFromNumberedRoad),
            typeof(Messages.RemoveGradeSeparatedJunction),
            typeof(Messages.RemoveRoadSegment),
            typeof(Messages.RemoveRoadNode)
        };

        public int Compare(SortableChange left, SortableChange right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            var leftRank = Array.IndexOf(SequenceByTypeOfChange, left.Change.GetType());
            var rightRank = Array.IndexOf(SequenceByTypeOfChange, right.Change.GetType());
            var comparison = leftRank.CompareTo(rightRank);
            return comparison != 0
                ? comparison
                : left.Ordinal.CompareTo(right.Ordinal);
        }
    }
}
