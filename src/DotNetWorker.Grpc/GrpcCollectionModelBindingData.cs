// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal partial class GrpcCollectionModelBindingData : Microsoft.Azure.Functions.Worker.Core.CollectionModelBindingData
    {
        private readonly CollectionModelBindingData _modelBindingDataArray;

        public GrpcCollectionModelBindingData(CollectionModelBindingData modelBindingDataArray)
        {
            _modelBindingDataArray = modelBindingDataArray;
        }

        public override Core.ModelBindingData[] ModelBindingDataArray => _modelBindingDataArray.ModelBindingData.Select(
                                                            p => new GrpcModelBindingData(p)).ToArray();
    }
}
