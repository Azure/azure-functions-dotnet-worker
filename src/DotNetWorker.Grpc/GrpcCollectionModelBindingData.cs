// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal class GrpcCollectionModelBindingData : Microsoft.Azure.Functions.Worker.Core.CollectionModelBindingData
    {
        private readonly CollectionModelBindingData _modelBindingDataArray;

        private readonly Core.ModelBindingData[] _collectionModelBindingData;

        public GrpcCollectionModelBindingData(CollectionModelBindingData modelBindingDataArray)
        {
            _modelBindingDataArray = modelBindingDataArray;
            _collectionModelBindingData = _modelBindingDataArray.ModelBindingData.Select(
                                                p => new GrpcModelBindingData(p)).ToArray();
        }

        public override Core.ModelBindingData[] ModelBindingDataArray => _collectionModelBindingData;
    }
}
