// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal partial class GrpcModelBindingData : Microsoft.Azure.Functions.Worker.Core.ModelBindingData
    {
        private readonly ModelBindingData _modelBindingData;

        public GrpcModelBindingData(ModelBindingData modelBindingData)
        {
            _modelBindingData = modelBindingData;
        }

        public override string Version => _modelBindingData.Version;

        public override string Source => _modelBindingData.Source;

        public override BinaryData Content => BinaryData.FromBytes(_modelBindingData.Content.ToByteArray());

        public override string ContentType => _modelBindingData.ContentType;
    }
}
