// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Core;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal sealed partial class BindingData : IBindingData
    {
        string IBindingData.Version => Version;

        string IBindingData.ContentType => ContentType;

        string IBindingData.Source => Source;

        string IBindingData.Content => Content;
    }
}