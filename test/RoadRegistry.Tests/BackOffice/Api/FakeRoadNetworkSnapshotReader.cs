namespace RoadRegistry.BackOffice.Api
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Messages;
    using SqlStreamStore.Streams;

    public class FakeRoadNetworkSnapshotReader : IRoadNetworkSnapshotReader
    {
        public Task<(RoadNetworkSnapshot snapshot, int version)> ReadSnapshot(CancellationToken cancellationToken)
        {
            return Task.FromResult<(RoadNetworkSnapshot snapshot, int version)>((null, ExpectedVersion.NoStream));
        }
    }
}
