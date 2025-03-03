namespace RoadRegistry.BackOffice.Handlers.Sqs.Lambda.Commands
{
    using Abstractions;
    using Amazon.Lambda.Core;
    using Newtonsoft.Json;

    public record InitiateFeatureCompareDockerContainerCommand(ILambdaContext Context) : LambdaCommand(Context)
    {
        public ArchiveId ArchiveId { get; init; }
    }
}
