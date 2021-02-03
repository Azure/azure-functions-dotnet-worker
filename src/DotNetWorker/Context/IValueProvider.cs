// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Context
{
    public interface IValueProvider
    {
        object? GetValue(string name);
    }
}
