namespace RoadRegistry.BackOffice.ZipArchiveWriters.Validation;

using System.IO.Compression;
using Be.Vlaanderen.Basisregisters.Shaperon;
using Editor.Schema.RoadSegments;
using Uploads;

public class RoadSegmentNationalRoadAttributeDbaseRecordsValidator : IZipArchiveDbaseRecordsValidator<RoadSegmentNationalRoadAttributeDbaseRecord>
{
    public (ZipArchiveProblems, ZipArchiveValidationContext) Validate(ZipArchiveEntry entry, IDbaseRecordEnumerator<RoadSegmentNationalRoadAttributeDbaseRecord> records, ZipArchiveValidationContext context)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (records == null) throw new ArgumentNullException(nameof(records));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var problems = ZipArchiveProblems.None;
        try
        {
            var identifiers = new Dictionary<AttributeId, RecordNumber>();
            var moved = records.MoveNext();
            if (moved)
                while (moved)
                {
                    var recordContext = entry.AtDbaseRecord(records.CurrentRecordNumber);
                    var record = records.Current;
                    if (record != null)
                    {
                        if (record.NW_OIDN.HasValue)
                        {
                            if (record.NW_OIDN.Value == 0) problems += recordContext.IdentifierZero();
                        }
                        else
                        {
                            problems += recordContext.RequiredFieldIsNull(record.NW_OIDN.Field);
                        }

                        if (!record.IDENT2.HasValue)
                            problems += recordContext.RequiredFieldIsNull(record.IDENT2.Field);
                        else if (!NationalRoadNumber.CanParse(record.IDENT2.Value)) problems += recordContext.NotNationalRoadNumber(record.IDENT2.Value);

                        if (!record.WS_OIDN.HasValue)
                            problems += recordContext.RequiredFieldIsNull(record.WS_OIDN.Field);
                        else if (!RoadSegmentId.Accepts(record.WS_OIDN.Value)) problems += recordContext.RoadSegmentIdOutOfRange(record.WS_OIDN.Value);

                        moved = records.MoveNext();
                    }
                }
            else
                problems += entry.HasNoDbaseRecords(true);
        }
        catch (Exception exception)
        {
            problems += entry.AtDbaseRecord(records.CurrentRecordNumber).HasDbaseRecordFormatError(exception);
        }

        return (problems, context);
    }
}
