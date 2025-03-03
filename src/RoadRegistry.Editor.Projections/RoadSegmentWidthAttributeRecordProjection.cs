namespace RoadRegistry.Editor.Projections
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using BackOffice.Messages;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.Connector;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.SqlStreamStore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IO;
    using Schema;
    using Schema.RoadSegments;

    public class RoadSegmentWidthAttributeRecordProjection : ConnectedProjection<EditorContext>
    {
        public RoadSegmentWidthAttributeRecordProjection(RecyclableMemoryStreamManager manager,
            Encoding encoding)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));

            When<Envelope<ImportedRoadSegment>>((context, envelope, token) =>
            {
                if (envelope.Message.Widths.Length == 0)
                    return Task.CompletedTask;

                var widths = envelope.Message
                    .Widths
                    .Select(width => new RoadSegmentWidthAttributeRecord
                    {
                        Id = width.AttributeId,
                        RoadSegmentId = envelope.Message.Id,
                        DbaseRecord = new RoadSegmentWidthAttributeDbaseRecord
                        {
                            WB_OIDN = { Value = width.AttributeId },
                            WS_OIDN = { Value = envelope.Message.Id },
                            WS_GIDN = { Value = $"{envelope.Message.Id}_{width.AsOfGeometryVersion}" },
                            BREEDTE = { Value = width.Width },
                            VANPOS = { Value = (double)width.FromPosition },
                            TOTPOS = { Value = (double)width.ToPosition },
                            BEGINTIJD = { Value = width.Origin.Since },
                            BEGINORG = { Value = width.Origin.OrganizationId },
                            LBLBGNORG = { Value = width.Origin.Organization }
                        }.ToBytes(manager, encoding)
                    });

                return context.RoadSegmentWidthAttributes.AddRangeAsync(widths, token);
            });

            When<Envelope<RoadNetworkChangesAccepted>>(async (context, envelope, token) =>
            {
                foreach (var change in envelope.Message.Changes.Flatten())
                    switch (change)
                    {
                        case RoadSegmentAdded segment:
                            await AddRoadSegment(manager, encoding, segment, envelope, context);
                            break;

                        case RoadSegmentModified segment:
                            await ModifyRoadSegment(manager, encoding, context, segment, envelope, token);

                            break;

                        case RoadSegmentRemoved segment:
                            await RemoveRoadSegment(context, segment, token);

                            break;
                    }
            });
        }

        private static async Task AddRoadSegment(RecyclableMemoryStreamManager manager,
            Encoding encoding,
            RoadSegmentAdded segment,
            Envelope<RoadNetworkChangesAccepted> envelope,
            EditorContext context)
        {
            if (segment.Widths.Length != 0)
            {
                var widths = segment
                    .Widths
                    .Select(width => new RoadSegmentWidthAttributeRecord
                    {
                        Id = width.AttributeId,
                        RoadSegmentId = segment.Id,
                        DbaseRecord = new RoadSegmentWidthAttributeDbaseRecord
                        {
                            WB_OIDN = { Value = width.AttributeId },
                            WS_OIDN = { Value = segment.Id },
                            WS_GIDN = { Value = $"{segment.Id}_{width.AsOfGeometryVersion}" },
                            BREEDTE = { Value = width.Width },
                            VANPOS = { Value = (double)width.FromPosition },
                            TOTPOS = { Value = (double)width.ToPosition },
                            BEGINTIJD = { Value = LocalDateTimeTranslator.TranslateFromWhen(envelope.Message.When) },
                            BEGINORG = { Value = envelope.Message.OrganizationId },
                            LBLBGNORG = { Value = envelope.Message.Organization }
                        }.ToBytes(manager, encoding)
                    });

                await context.RoadSegmentWidthAttributes.AddRangeAsync(widths);
            }
        }

        private static async Task ModifyRoadSegment(RecyclableMemoryStreamManager manager,
            Encoding encoding,
            EditorContext context,
            RoadSegmentModified segment,
            Envelope<RoadNetworkChangesAccepted> envelope,
            CancellationToken token)
        {
            if (segment.Lanes.Length == 0)
            {
                context.RoadSegmentWidthAttributes.RemoveRange(
                    context
                        .RoadSegmentWidthAttributes
                        .Local.Where(a => a.RoadSegmentId == segment.Id)
                        .Concat(await context
                            .RoadSegmentWidthAttributes
                            .Where(a => a.RoadSegmentId == segment.Id)
                            .ToArrayAsync(token)
                        ));
            }
            else
            {
                //Causes all attributes to be loaded into Local
                await context
                    .RoadSegmentWidthAttributes
                    .Where(a => a.RoadSegmentId == segment.Id)
                    .ToArrayAsync(token);
                var currentSet = context
                    .RoadSegmentWidthAttributes
                    .Local.Where(a => a.RoadSegmentId == segment.Id)
                    .ToDictionary(a => a.Id);
                var nextSet = segment
                    .Widths
                    .Select(width => new RoadSegmentWidthAttributeRecord
                    {
                        Id = width.AttributeId,
                        RoadSegmentId = segment.Id,
                        DbaseRecord = new RoadSegmentWidthAttributeDbaseRecord
                        {
                            WB_OIDN = { Value = width.AttributeId },
                            WS_OIDN = { Value = segment.Id },
                            WS_GIDN = { Value = $"{segment.Id}_{width.AsOfGeometryVersion}" },
                            BREEDTE = { Value = width.Width },
                            VANPOS = { Value = (double)width.FromPosition },
                            TOTPOS = { Value = (double)width.ToPosition },
                            BEGINTIJD = { Value = LocalDateTimeTranslator.TranslateFromWhen(envelope.Message.When) },
                            BEGINORG = { Value = envelope.Message.OrganizationId },
                            LBLBGNORG = { Value = envelope.Message.Organization }
                        }.ToBytes(manager, encoding)
                    })
                    .ToDictionary(a => a.Id);
                context.RoadSegmentWidthAttributes.Synchronize(currentSet, nextSet,
                    (current, next) => { current.DbaseRecord = next.DbaseRecord; });
            }
        }

        private static async Task RemoveRoadSegment(EditorContext context, RoadSegmentRemoved segment,
            CancellationToken token)
        {
            context.RoadSegmentWidthAttributes.RemoveRange(
                context
                    .RoadSegmentWidthAttributes
                    .Local.Where(a => a.RoadSegmentId == segment.Id)
                    .Concat(await context
                        .RoadSegmentWidthAttributes
                        .Where(a => a.RoadSegmentId == segment.Id)
                        .ToArrayAsync(token)
                    ));
        }
    }
}
