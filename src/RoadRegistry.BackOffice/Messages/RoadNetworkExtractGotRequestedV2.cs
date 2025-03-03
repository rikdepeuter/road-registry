namespace RoadRegistry.BackOffice.Messages;

using System;
using Be.Vlaanderen.Basisregisters.EventHandling;

[EventName("RoadNetworkExtractGotRequestedV2")]
[EventDescription("Indicates a road network extract was requested.")]
public class RoadNetworkExtractGotRequestedV2
{
    public string RequestId { get; set; }
    public string ExternalRequestId { get; set; }
    public Guid DownloadId { get; set; }
    public string Description { get; set; }
    public RoadNetworkExtractGeometry Contour { get; set; }
    public string When { get; set; }
}
