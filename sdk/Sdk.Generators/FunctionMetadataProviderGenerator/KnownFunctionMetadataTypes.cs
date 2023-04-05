// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal readonly struct KnownFunctionMetadataTypes
    {
        internal readonly INamedTypeSymbol? BindingAttribute;
        internal readonly INamedTypeSymbol? OutputBindingAttribute;
        internal readonly INamedTypeSymbol? FunctionName;
        internal readonly INamedTypeSymbol? BindingPropertyNameAttribute;
        internal readonly INamedTypeSymbol? DefaultValue;
        internal readonly INamedTypeSymbol? HttpResponse;
        internal readonly INamedTypeSymbol? HttpTriggerBinding;

        internal KnownFunctionMetadataTypes(Compilation compilation)
        {
            BindingAttribute = compilation.GetTypeByMetadataName(Constants.Types.BindingAttribute); // TODO: Find appropriate exception and/or error message.
            OutputBindingAttribute = compilation.GetTypeByMetadataName(Constants.Types.OutputBindingAttribute);
            FunctionName = compilation.GetTypeByMetadataName(Constants.Types.FunctionName);
            BindingPropertyNameAttribute = compilation.GetTypeByMetadataName(Constants.Types.BindingPropertyNameAttribute);
            DefaultValue = compilation.GetTypeByMetadataName(Constants.Types.DefaultValue);
            HttpResponse = compilation.GetTypeByMetadataName(Constants.Types.HttpResponse);
            HttpTriggerBinding = compilation.GetTypeByMetadataName(Constants.Types.HttpTriggerBinding);
        }
    }
}
