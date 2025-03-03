namespace RoadRegistry.Hosts.Metadata
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMetadataUpdater
    {
        Task UpdateAsync(CancellationToken cancellationToken);
    }
}
