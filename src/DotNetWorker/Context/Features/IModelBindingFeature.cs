// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    internal interface IModelBindingFeature
    {
        object?[]? InputArguments { get; }

        object?[] TryBindFunctionInput(FunctionContext context);
    }
}
