namespace RoadRegistry.Editor.ProjectionHost.Tests;

using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Startup : TestStartup
{
    public override void ConfigureContainer(ContainerBuilder builder)
    {
    }

    public override void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
    {
    }
}
