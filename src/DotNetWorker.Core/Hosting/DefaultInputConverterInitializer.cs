// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Core;

internal class DefaultInputConverterInitializer : IConfigureOptions<WorkerOptions>
{
    public void Configure(WorkerOptions options)
    {
        options.InputConverters.Register<FunctionContextConverter>();
        options.InputConverters.Register<TypeConverter>();
        options.InputConverters.Register<GuidConverter>();
        options.InputConverters.Register<DateTimeConverter>();
        options.InputConverters.Register<MemoryConverter>();
        options.InputConverters.Register<StringToByteConverter>();
        options.InputConverters.Register<JsonPocoConverter>();
        options.InputConverters.Register<ArrayConverter>();
        options.InputConverters.Register<CancellationTokenConverter>();
    }
}
