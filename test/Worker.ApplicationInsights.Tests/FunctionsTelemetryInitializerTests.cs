using System.Diagnostics;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Tests;

public class FunctionsTelemetryInitializerTests
{
    [Fact]
    public void Initialize_SetsContextProperties()
    {
        var initializer = new FunctionsTelemetryInitializer("testversion", "testrolename");
        var telemetry = new TraceTelemetry();
        initializer.Initialize(telemetry);

        Assert.Equal("testversion", telemetry.Context.GetInternalContext().SdkVersion);
        Assert.Equal("testrolename", telemetry.Context.Cloud.RoleInstance);
    }

    [Fact]
    public void Initialize_SetsProperties_WithActivityTags()
    {
        var activity = new Activity("operation");
        var telemetry = new TraceTelemetry();

        try
        {
            activity.Start();
            activity.AddTag("Name", "MyFunction");
            activity.AddTag("CustomKey", "CustomValue");

            var initializer = new FunctionsTelemetryInitializer("testversion", "testrolename");
            initializer.Initialize(telemetry);
        }
        finally
        {
            activity.Stop();
        }

        Assert.Equal("MyFunction", telemetry.Context.Operation.Name);
        Assert.Equal("CustomValue", telemetry.Properties["CustomKey"]);
    }
}
