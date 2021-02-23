// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    public class GrpcBindingMetadata : BindingMetadata
    {
        public GrpcBindingMetadata(BindingInfo bindingInfo)
        {
            Type = bindingInfo.Type;
            Direction = bindingInfo.Direction == BindingInfo.Types.Direction.In ? BindingDirection.In : BindingDirection.Out;
        }

        public override string Type { get; set; }

        public override BindingDirection Direction { get; set; }
    }
}
