namespace RoadRegistry.BackOffice.Api;

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

[ApiVersionNeutral]
[Route("")]
public class HomeController : ControllerBase
{
    [HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Get()
    {
        return Request.Headers[HeaderNames.Accept].ToString().Contains("text/html")
            ? new RedirectResult("/docs")
            : new OkObjectResult($"Welcome to the Wegenregister Api v{Assembly.GetEntryAssembly()?.GetName().Version}.");
    }
}
