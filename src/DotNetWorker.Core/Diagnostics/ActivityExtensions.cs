// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal static class ActivityExtensions
    {
        public static void RecordException(this Activity activity, Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            var tagsCollection = new ActivityTagsCollection
            {
                { TraceConstants.AttributeExceptionType, ex.GetType().FullName },
                { TraceConstants.AttributeExceptionStacktrace, ex.ToString() }
            };

            if (!string.IsNullOrWhiteSpace(ex.Message))
            {
                tagsCollection.Add(TraceConstants.AttributeExceptionMessage, ex.Message);
            }

            activity?.AddEvent(new ActivityEvent(TraceConstants.AttributeExceptionEventName, default, tagsCollection));
        }
    }
}
