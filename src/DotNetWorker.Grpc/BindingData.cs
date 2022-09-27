// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal sealed partial class BindingData : IBindingData
    {
        public string Version => Version;

        public string ContentType => ContentType;

        public object Content => Content;
    }
}
