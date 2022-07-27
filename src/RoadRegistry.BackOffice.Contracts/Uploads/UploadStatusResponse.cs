namespace RoadRegistry.BackOffice.Contracts.Uploads;

using Microsoft.Net.Http.Headers;

public sealed record UploadStatusResponse(string FileName, MediaTypeHeaderValue MediaTypeHeaderValue, Func<Stream, CancellationToken, Task> Callback) : FileResponse(FileName, MediaTypeHeaderValue, Callback)
{
}
