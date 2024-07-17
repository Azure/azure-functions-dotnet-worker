// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal static class ActivityExtensions
    {

        private static readonly Action<Activity, string> _setSpanId;
        private static readonly Action<Activity, string> _setId;
        private static readonly Action<Activity, string> _setTraceId;
        private static readonly Action<Activity, string> _setRootId;

        static ActivityExtensions()
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var activityType = typeof(Activity);

            // Empty setter serves as a safe fallback mechanism to handle cases where the field is not available.
            _setSpanId = activityType.GetField("_spanId", flags)?.CreateSetter<Activity, string>() ?? ((_, _) => { /* Ignore */ });
            _setId = activityType.GetField("_id", flags)?.CreateSetter<Activity, string>() ?? ((_, _) => { /* Ignore */ });
            _setRootId = activityType.GetField("_rootId", flags)?.CreateSetter<Activity, string>() ?? ((_, _) => { /* Ignore */ });
            _setTraceId = activityType.GetField("_traceId", flags)?.CreateSetter<Activity, string>() ?? ((_, _) => { /* Ignore */ });
        }

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

        public static void SetId(this Activity activity, string id)
            => _setId(activity, id);

        public static void SetSpanId(this Activity activity, string spanId)
            => _setSpanId(activity, spanId);

        public static void SetRootId(this Activity activity, string rootId)
            => _setRootId(activity, rootId);

        public static void SetTraceId(this Activity activity, string traceId)
            => _setTraceId(activity, traceId);
    }

    internal static class FieldInfoExtensionMethods
    {
        /// <summary>
        /// Create a re-usable setter for a <see cref="FieldInfo"/>.
        /// When cached and reused, This is quicker than using <see cref="FieldInfo.SetValue(object, object)"/>.
        /// </summary>
        /// <typeparam name="TTarget">The target type of the object.</typeparam>
        /// <typeparam name="TValue">The value type of the field.</typeparam>
        /// <param name="fieldInfo">The field info.</param>
        /// <returns>A re-usable action to set the field.</returns>
        internal static Action<TTarget, TValue> CreateSetter<TTarget, TValue>(this FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldInfo));
            }

            ParameterExpression targetExp = Expression.Parameter(typeof(TTarget), "target");
            Expression source = targetExp;

            if (typeof(TTarget) != fieldInfo.DeclaringType)
            {
                source = Expression.Convert(targetExp, fieldInfo.DeclaringType);
            }

            // Creating the setter to set the value to the field
            ParameterExpression valueExp = Expression.Parameter(typeof(TValue), "value");
            MemberExpression fieldExp = Expression.Field(source, fieldInfo);
            BinaryExpression assignExp = Expression.Assign(fieldExp, valueExp);
            return Expression.Lambda<Action<TTarget, TValue>>(assignExp, targetExp, valueExp).Compile();
        }
    }
}
