namespace RoadRegistry.BackOffice.Extracts;

using System;
using System.IO.Compression;
using Be.Vlaanderen.Basisregisters.BlobStore;
using Core;
using Framework;
using Messages;
using NodaTime;
using SqlStreamStore;
using Uploads;

public class RoadNetworkExtractCommandModule : CommandHandlerModule
{
    public RoadNetworkExtractCommandModule(
        RoadNetworkExtractUploadsBlobClient uploadsBlobClient,
        IStreamStore store,
        IRoadNetworkSnapshotReader snapshotReader,
        IZipArchiveAfterFeatureCompareValidator validator,
        IClock clock)
    {
        if (uploadsBlobClient == null) throw new ArgumentNullException(nameof(uploadsBlobClient));
        if (store == null) throw new ArgumentNullException(nameof(store));
        if (validator == null) throw new ArgumentNullException(nameof(validator));
        if (snapshotReader == null) throw new ArgumentNullException(nameof(snapshotReader));
        if (clock == null) throw new ArgumentNullException(nameof(clock));

        For<RequestRoadNetworkExtract>()
            .UseValidator(new RequestRoadNetworkExtractValidator())
            .UseRoadRegistryContext(store, snapshotReader, EnrichEvent.WithTime(clock))
            .Handle(async (context, message, ct) =>
            {
                var externalRequestId = new ExternalExtractRequestId(message.Body.ExternalRequestId);
                var requestId = ExtractRequestId.FromExternalRequestId(externalRequestId);
                var downloadId = new DownloadId(message.Body.DownloadId);
                var description = new ExtractDescription(message.Body.Description);
                var contour = GeometryTranslator.Translate(message.Body.Contour);
                var extract = await context.RoadNetworkExtracts.Get(requestId, ct);
                if (extract == null)
                {
                    extract = RoadNetworkExtract.Request(externalRequestId, downloadId, description, contour);
                    context.RoadNetworkExtracts.Add(extract);
                }
                else
                {
                    extract.RequestAgain(downloadId, contour);
                }
            });

        For<AnnounceRoadNetworkExtractDownloadBecameAvailable>()
            .UseRoadRegistryContext(store, snapshotReader, EnrichEvent.WithTime(clock))
            .Handle(async (context, message, ct) =>
            {
                var requestId = ExtractRequestId.FromString(message.Body.RequestId);
                var downloadId = new DownloadId(message.Body.DownloadId);
                var archiveId = new ArchiveId(message.Body.ArchiveId);
                var extract = await context.RoadNetworkExtracts.Get(requestId, ct);
                extract.Announce(downloadId, archiveId);
            });

        For<UploadRoadNetworkExtractChangesArchive>()
            .UseRoadRegistryContext(store, snapshotReader, EnrichEvent.WithTime(clock))
            .Handle(async (context, message, ct) =>
            {
                var requestId = ExtractRequestId.FromString(message.Body.RequestId);
                var forDownloadId = new DownloadId(message.Body.DownloadId);
                var uploadId = new UploadId(message.Body.UploadId);
                var archiveId = new ArchiveId(message.Body.ArchiveId);
                var extract = await context.RoadNetworkExtracts.Get(requestId, ct);

                var upload = extract.Upload(forDownloadId, uploadId, archiveId);

                var archiveBlob = await uploadsBlobClient.GetBlobAsync(new BlobName(archiveId), ct);
                await using (var archiveBlobStream = await archiveBlob.OpenAsync(ct))
                using (var archive = new ZipArchive(archiveBlobStream, ZipArchiveMode.Read, false))
                {
                    upload.ValidateArchiveUsing(archive, validator);
                }
            });
    }
}
