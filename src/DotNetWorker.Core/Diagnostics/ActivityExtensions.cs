// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal static class ActivityExtensions
    {
        /// <summary>
        /// Records an exception as an ActivityEvent.
        /// </summary>
        /// <param name="activity">The Activity.</param>
        /// <param name="ex">The exception.</param>
        /// <param name="escaped">If the exception is re-thrown out of the current span, set to true. 
        /// See https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/exceptions/#recording-an-exception.
        /// </param>
        public static void RecordException(this Activity activity, Exception ex, bool escaped)
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

            if (escaped)
            {
                tagsCollection.Add(TraceConstants.AttributeExceptionEscaped, true);
            }

            activity?.AddEvent(new ActivityEvent(TraceConstants.AttributeExceptionEventName, default, tagsCollection));
        }
    }
}
