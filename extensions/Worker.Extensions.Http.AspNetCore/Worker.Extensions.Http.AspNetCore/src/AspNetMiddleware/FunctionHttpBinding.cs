// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    /// <summary>
    /// Represents an HttpTrigger binding. Internal class for deserializing raw binding info.
    /// </summary>
    internal class FunctionHttpBinding
    {
        public string Name { get; set; } = default!;

        public string Type { get; set; } = default!;

        public string Route { get; set; } = default!;

        public string[] Methods { get; set; } = default!;
    }
}
