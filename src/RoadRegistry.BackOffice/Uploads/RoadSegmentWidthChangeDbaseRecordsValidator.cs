namespace RoadRegistry.BackOffice.Uploads;

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using Be.Vlaanderen.Basisregisters.Shaperon;
using Schema;

public class RoadSegmentWidthChangeDbaseRecordsValidator : IZipArchiveDbaseRecordsValidator<RoadSegmentWidthChangeDbaseRecord>
{
    public (ZipArchiveProblems, ZipArchiveValidationContext) Validate(ZipArchiveEntry entry, IDbaseRecordEnumerator<RoadSegmentWidthChangeDbaseRecord> records, ZipArchiveValidationContext context)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (records == null) throw new ArgumentNullException(nameof(records));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var problems = ZipArchiveProblems.None;
        try
        {
            var segments =
                context
                    .KnownRoadSegments
                    .ToDictionary(segment => segment, segment => 0);
            var identifiers = new Dictionary<AttributeId, RecordNumber>();
            var moved = records.MoveNext();
            if (moved)
            {
                while (moved)
                {
                    var recordContext = entry.AtDbaseRecord(records.CurrentRecordNumber);
                    var record = records.Current;
                    if (record != null)
                    {
                        RecordType recordType = default;
                        if (!record.RECORDTYPE.HasValue)
                        {
                            problems += recordContext.RequiredFieldIsNull(record.RECORDTYPE.Field);
                        }
                        else
                        {
                            if (!RecordType.ByIdentifier.TryGetValue(record.RECORDTYPE.Value, out recordType))
                                problems += recordContext.RecordTypeMismatch(record.RECORDTYPE.Value);
                            else if (!recordType.IsAnyOf(RecordType.Identical, RecordType.Added, RecordType.Removed)) problems += recordContext.RecordTypeNotSupported(record.RECORDTYPE.Value, RecordType.Identical.Translation.Identifier, RecordType.Added.Translation.Identifier, RecordType.Removed.Translation.Identifier);
                        }

                        if (record.WB_OIDN.HasValue)
                        {
                            if (record.WB_OIDN.Value == 0)
                            {
                                problems += recordContext.IdentifierZero();
                            }
                            else
                            {
                                if (recordType != RecordType.Added)
                                {
                                    var identifier = new AttributeId(record.WB_OIDN.Value);
                                    if (identifiers.TryGetValue(identifier, out var takenByRecordNumber))
                                        problems += recordContext.IdentifierNotUnique(identifier, takenByRecordNumber);
                                    else
                                        identifiers.Add(identifier, records.CurrentRecordNumber);
                                }
                            }
                        }
                        else
                        {
                            problems += recordContext.RequiredFieldIsNull(record.WB_OIDN.Field);
                        }

                        if (!record.BREEDTE.HasValue)
                            problems += recordContext.RequiredFieldIsNull(record.BREEDTE.Field);
                        else if (!RoadSegmentWidth.Accepts(record.BREEDTE.Value)) problems += recordContext.WidthOutOfRange(record.BREEDTE.Value);

                        if (!record.VANPOSITIE.HasValue)
                            problems += recordContext.RequiredFieldIsNull(record.VANPOSITIE.Field);
                        else if (!RoadSegmentPosition.Accepts(record.VANPOSITIE.Value)) problems += recordContext.FromPositionOutOfRange(record.VANPOSITIE.Value);

                        if (!record.TOTPOSITIE.HasValue)
                            problems += recordContext.RequiredFieldIsNull(record.TOTPOSITIE.Field);
                        else if (!RoadSegmentPosition.Accepts(record.TOTPOSITIE.Value)) problems += recordContext.ToPositionOutOfRange(record.TOTPOSITIE.Value);

                        if (record.VANPOSITIE.HasValue && record.TOTPOSITIE.HasValue &&
                            record.VANPOSITIE.Value >= record.TOTPOSITIE.Value)
                            problems += recordContext.FromPositionEqualToOrGreaterThanToPosition(
                                record.VANPOSITIE.Value,
                                record.TOTPOSITIE.Value);

                        if (!record.WS_OIDN.HasValue)
                            problems += recordContext.RequiredFieldIsNull(record.WS_OIDN.Field);
                        else if (!RoadSegmentId.Accepts(record.WS_OIDN.Value))
                            problems += recordContext.RoadSegmentIdOutOfRange(record.WS_OIDN.Value);
                        else if (!segments.ContainsKey(new RoadSegmentId(record.WS_OIDN.Value)))
                            problems += recordContext.RoadSegmentMissing(record.WS_OIDN.Value);
                        else
                            segments[new RoadSegmentId(record.WS_OIDN.Value)] += 1;
                    }

                    moved = records.MoveNext();
                }

                var segmentsWithoutAttributes = segments
                    .Where(pair => pair.Value == 0)
                    .Select(pair => pair.Key)
                    .ToArray();
                if (segmentsWithoutAttributes.Length > 0) problems += entry.RoadSegmentsWithoutWidthAttributes(segmentsWithoutAttributes.ToArray());
            }
            else
            {
                problems += entry.HasNoDbaseRecords(false);
            }
        }
        catch (Exception exception)
        {
            problems += entry.AtDbaseRecord(records.CurrentRecordNumber).HasDbaseRecordFormatError(exception);
        }

        return (problems, context);
    }
}
