using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.ApplicationInsights;

public class FunctionsTelemetryInitializerTests
{
    [Fact]
    public void Initialize_SetsContextProperties()
    {
        var initializer = new FunctionsTelemetryInitializer("testversion");
        var telemetry = new TraceTelemetry();
        initializer.Initialize(telemetry);

        Assert.Equal("testversion", telemetry.Context.GetInternalContext().SdkVersion);
    }
}

