// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
///  The application used to configure an Azure Functions worker.
/// </summary>
public class FunctionsApplication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionsApplicationBuilder"/> class with preconfigured defaults.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The <see cref="FunctionsApplicationBuilder"/>.</returns>
    public static FunctionsApplicationBuilder CreateBuilder(string[] args) => new(args);
}
