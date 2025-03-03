namespace RoadRegistry.BackOffice.ZipArchiveWriters.ForEditor;

using System.IO.Compression;
using System.Text;
using Be.Vlaanderen.Basisregisters.Shaperon;
using Editor.Schema;
using Editor.Schema.RoadSegments;
using Microsoft.EntityFrameworkCore;
using Microsoft.IO;

public class RoadSegmentNationalRoadAttributesToZipArchiveWriter : IZipArchiveWriter<EditorContext>
{
    private readonly Encoding _encoding;
    private readonly RecyclableMemoryStreamManager _manager;

    public RoadSegmentNationalRoadAttributesToZipArchiveWriter(RecyclableMemoryStreamManager manager,
        Encoding encoding)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
    }

    public async Task WriteAsync(ZipArchive archive, EditorContext context, CancellationToken cancellationToken)
    {
        if (archive == null) throw new ArgumentNullException(nameof(archive));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var count = await context.RoadSegmentNationalRoadAttributes.CountAsync(cancellationToken);
        var dbfEntry = archive.CreateEntry("AttNationweg.dbf");
        var dbfHeader = new DbaseFileHeader(
            DateTime.Now,
            DbaseCodePage.Western_European_ANSI,
            new DbaseRecordCount(count),
            RoadSegmentNationalRoadAttributeDbaseRecord.Schema
        );
        await using (var dbfEntryStream = dbfEntry.Open())
        using (var dbfWriter =
               new DbaseBinaryWriter(
                   dbfHeader,
                   new BinaryWriter(dbfEntryStream, _encoding, true)))
        {
            var dbfRecord = new RoadSegmentNationalRoadAttributeDbaseRecord();
            foreach (var data in context.RoadSegmentNationalRoadAttributes.OrderBy(_ => _.Id).Select(_ => _.DbaseRecord))
            {
                dbfRecord.FromBytes(data, _manager, _encoding);
                dbfWriter.Write(dbfRecord);
            }

            dbfWriter.Writer.Flush();
            await dbfEntryStream.FlushAsync(cancellationToken);
        }
    }
}
