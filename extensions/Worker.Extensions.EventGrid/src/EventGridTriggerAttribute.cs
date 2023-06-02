// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters;

namespace Microsoft.Azure.Functions.Worker
{
    [AllowConverterFallback(true)]
    [InputConverter(typeof(EventGridCloudEventConverter))]
    public sealed class EventGridTriggerAttribute : TriggerBindingAttribute
    {
    }
}
