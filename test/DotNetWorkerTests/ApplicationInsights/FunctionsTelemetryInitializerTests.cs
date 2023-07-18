using System;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.ApplicationInsights;

public class FunctionsTelemetryInitializerTests : IDisposable
{
    public FunctionsTelemetryInitializerTests()
    {
        // make sure these are clear before each test
        SetEnvironmentVariables(null, null, null);
    }

    [Fact]
    public void Initialize_SetsContextProperties()
    {
        var initializer = new FunctionsTelemetryInitializer(new FunctionsRoleInstanceProvider(), "testversion");
        var telemetry = new TraceTelemetry();
        initializer.Initialize(telemetry);

        Assert.Equal("testversion", telemetry.Context.GetInternalContext().SdkVersion);
    }

    [Fact]
    public void RoleInstanceProvider_UsesWebsiteInstanceId()
    {
        SetEnvironmentVariables("instanceId", "computerName", "containerName");

        var provider = new FunctionsRoleInstanceProvider();
        Assert.Equal("instanceId", provider.GetRoleInstanceName());
    }

    [Fact]
    public void RoleInstanceProvider_UsesComputerName()
    {
        SetEnvironmentVariables(null, "computerName", "containerName");

        var provider = new FunctionsRoleInstanceProvider();
        Assert.Equal("computerName", provider.GetRoleInstanceName());
    }

    [Fact]
    public void RoleInstanceProvider_UsesContainerName()
    {
        SetEnvironmentVariables(null, null, "containerName");

        var provider = new FunctionsRoleInstanceProvider();
        Assert.Equal("containerName", provider.GetRoleInstanceName());
    }

    private static void SetEnvironmentVariables(string instanceId, string computerName, string containerName)
    {
        Environment.SetEnvironmentVariable(FunctionsRoleInstanceProvider.WebSiteInstanceIdKey, instanceId);
        Environment.SetEnvironmentVariable(FunctionsRoleInstanceProvider.ComputerNameKey, computerName);
        Environment.SetEnvironmentVariable(FunctionsRoleInstanceProvider.ContainerNameKey, containerName);
    }

    public void Dispose()
    {
        SetEnvironmentVariables(null, null, null);
    }
}
