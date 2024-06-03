// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Azure.Functions.Worker.Extensions.Timer.Converters;
using Microsoft.Extensions.DependencyInjection;

[assembly: WorkerExtensionStartup(typeof(TimerExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker.Extensions.Timer
{
    /// <summary>
    /// Startup for Microsoft.Azure.Functions.Worker.Extensions.Timer extension.
    /// </summary>
    public sealed class TimerExtensionStartup : WorkerExtensionStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            applicationBuilder.Services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.RegisterAt<TimerPocoConverter>(0);
            });
        }
    }
}
