// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal partial class GrpcCollectionModelBindingData : Microsoft.Azure.Functions.Worker.Core.CollectionModelBindingData
    {
        public GrpcCollectionModelBindingData(CollectionModelBindingData modelBindingDataArray)
        {
            ModelBindingDataArray = modelBindingDataArray.ModelBindingData
                                        .Select(p => new GrpcModelBindingData(p)).ToArray();
        }

        public override Core.ModelBindingData[] ModelBindingDataArray { get; }
    }
}
