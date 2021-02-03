// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class FunctionsJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            // We need to camelCase everything but this one property or the host won't honor it.
            if (string.Equals("IsCodeless", name, System.StringComparison.OrdinalIgnoreCase))
            {
                return "IsCodeless";
            }

            return CamelCase.ConvertName(name);
        }
    }
}
