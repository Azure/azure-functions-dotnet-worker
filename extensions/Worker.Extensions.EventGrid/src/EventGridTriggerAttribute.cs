// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters;

namespace Microsoft.Azure.Functions.Worker
{
    [InputConverter(typeof(EventGridCloudEventConverter))]
    [InputConverter(typeof(EventGridEventConverter))]
    [InputConverter(typeof(EventGridBinaryDataConverter))]
    [InputConverter(typeof(EventGridStringArrayConverter))]
    [ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
    public sealed class EventGridTriggerAttribute : TriggerBindingAttribute
    {
        private bool _isBatched = false;

        /// <summary>
        /// Gets or sets the configuration to enable batch processing of event grid. Default value is "false".
        /// </summary>
        [DefaultValue(false)]
        public bool IsBatched
        {
            get => _isBatched;
            set => _isBatched = value;
        }

    }
}
