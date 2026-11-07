// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.Testing;

/// <summary>
/// Configures startup, invocation, shutdown, and transport limits for a functions application factory.
/// </summary>
public sealed class FunctionsApplicationFactoryOptions
{
    /// <summary>Gets or sets the maximum time allowed for application startup.</summary>
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the maximum time allowed for one function invocation.</summary>
    public TimeSpan InvocationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the maximum time allowed for application shutdown.</summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets or sets the maximum protocol message length in bytes.</summary>
    public int MaxMessageLength { get; set; } = 134_217_728;

    /// <summary>Gets or sets the host environment name.</summary>
    public string EnvironmentName { get; set; } = Environments.Development;

    internal FunctionsApplicationFactoryOptions Clone()
        => new()
        {
            StartupTimeout = StartupTimeout,
            InvocationTimeout = InvocationTimeout,
            ShutdownTimeout = ShutdownTimeout,
            MaxMessageLength = MaxMessageLength,
            EnvironmentName = EnvironmentName
        };

    internal void Validate()
    {
        ValidatePositive(StartupTimeout, nameof(StartupTimeout));
        ValidatePositive(InvocationTimeout, nameof(InvocationTimeout));
        ValidatePositive(ShutdownTimeout, nameof(ShutdownTimeout));

        if (MaxMessageLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxMessageLength), "The maximum message length must be positive.");
        }

        if (string.IsNullOrWhiteSpace(EnvironmentName))
        {
            throw new ArgumentException("The environment name cannot be empty.", nameof(EnvironmentName));
        }
    }

    private static void ValidatePositive(TimeSpan value, string parameterName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Timeouts must be positive.");
        }
    }
}
