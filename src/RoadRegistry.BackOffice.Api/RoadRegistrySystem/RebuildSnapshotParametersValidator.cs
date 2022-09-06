namespace RoadRegistry.BackOffice.Api.RoadRegistrySystem;

using Be.Vlaanderen.Basisregisters.BlobStore;
using FluentValidation;

public class RebuildSnapshotParametersValidator : AbstractValidator<RebuildSnapshotParameters>
{
    private static readonly BlobName SnapshotPrefix = new BlobName("roadnetworksnapshot-");

    public RebuildSnapshotParametersValidator(IBlobClient client)
    {
        RuleFor(x => x.StartFromVersion)
            .GreaterThanOrEqualTo(0)
            .MustAsync(async (version, ct) =>
            {
                if (version > 0)
                {
                    var snapshotBlobName = SnapshotPrefix.Append(new BlobName(version.ToString()));
                    return await client.BlobExistsAsync(snapshotBlobName, ct);
                }

                return true;
            });
    }
}
