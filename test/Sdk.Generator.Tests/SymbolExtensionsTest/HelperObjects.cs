// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    internal class FooOutAttribute : OutputBindingAttribute
    {
    }

    internal class FooOutAttribute2 : FooOutAttribute
    {
    }

    internal class BarInAttribute : InputBindingAttribute
    {
    }

    internal class BarInAttribute2 : BarInAttribute
    {
    }
}
