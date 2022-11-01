// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Core;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal sealed partial class ModelBindingData : IModelBindingData
    {
        string IModelBindingData.Version => Version;

        string IModelBindingData.Source => Source;

        BinaryData IModelBindingData.Content => BinaryData.FromBytes(Content.ToByteArray());

        string IModelBindingData.ContentType => ContentType;
    }
}