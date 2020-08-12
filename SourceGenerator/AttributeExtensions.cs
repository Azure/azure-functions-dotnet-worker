using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace SourceGenerator
{
    internal static class AttributeExtensions
    {
        /// <summary>
        /// {Name}Attribute -> name
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static string ToAttributeFriendlyName(this Attribute attribute)
        {
            return ToAttributeFriendlyName(attribute.GetType().Name);
        }

        public static string ToAttributeFriendlyName(this CustomAttribute attribute)
        {
            return ToAttributeFriendlyName(attribute.AttributeType.Name);
        }

        private static string ToAttributeFriendlyName(string name)
        {
            const string suffix = nameof(Attribute);
            name = name.Substring(0, name.Length - suffix.Length);
            return name.ToLowerFirstCharacter();
        }

        private static readonly HashSet<string> _supportedAttributes = new HashSet<string>
         {
             "BlobTriggerAttribute",
             "QueueTriggerAttribute",
             "EventHubTriggerAttribute",
             "TimerTriggerAttribute",
             "ServiceBusTriggerAttribute",
             "CosmosDBTriggerAttribute"
         };

        /// <summary>
        ///
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static bool IsWebJobsAttribute(this CustomAttribute attribute)
        {
            return attribute.AttributeType.Resolve().CustomAttributes.Any(a => a.AttributeType.FullName == "Microsoft.Azure.WebJobs.Description.BindingAttribute")
                || _supportedAttributes.Contains(attribute.AttributeType.FullName);
        }

        /// <summary>
        /// For every binding (which is what the returned JObject represents) there are 3 special keys:
        ///     "name" -> that is the parameter name, not set by this function
        ///     "type" -> that is the binding type. This is derived from the Attribute.Name itself. <see cref="AttributeExtensions.ToAttributeFriendlyName(Attribute)"/>
        /// a side from these 3, all the others are direct serialization of all of the attribute's properties.
        /// The mapping however isn't 1:1 in terms of the naming. Therefore, <see cref="NormalizePropertyName(string, PropertyInfo)"/>
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static T GetValue<T>(this Attribute attribute, string propertyName)
        {
            var property = attribute.GetType().GetProperty(propertyName);
            if (property != null)
            {
                return (T)property.GetValue(attribute);
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetValue(this Attribute attribute, string propertyName, object propertyValue)
        {
            var property = attribute.GetType().GetProperty(propertyName);
            if (property != null)
            {
                property.SetValue(attribute, propertyValue);
            }
        }

        private static bool TryGetPropertyValue(PropertyInfo property, object propertyValue, out string value)
        {
            value = null;
#if NET46
            if (property.PropertyType.IsEnum)
#else
            if (property.PropertyType.GetTypeInfo().IsEnum)
#endif
            {
                value = Enum.GetName(property.PropertyType, propertyValue).ToLowerFirstCharacter();
                return true;
            }
            return false;
        }

        private static void CheckIfPropertyIsSupported(string attributeName, PropertyInfo property)
        {
            var propertyName = property.Name;
            if (attributeName == "TimerTriggerAttribute")
            {
                if (propertyName == "ScheduleType")
                {
                    throw new NotImplementedException($"Property '{propertyName}' on attribute '{attributeName}' is not supported in Azure Functions.");
                }
            }
        }

        /// <summary>
        /// These exceptions are coming from how the script runtime is reading function.json
        /// See https://github.com/Azure/azure-webjobs-sdk-script/tree/dev/src/WebJobs.Script/Binding
        /// If there are no exceptions for a given property name on a given attribute, then return it's name with a lowerCase first character.
        /// </summary>
        /// <param name="attributeName"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        private static string NormalizePropertyName(Attribute attribute, PropertyInfo property)
        {
            var attributeName = attribute.GetType().Name;
            var propertyName = property.Name;

            if (attributeName == "BlobTriggerAttribute")
            {
                if (propertyName == "BlobPath")
                {
                    return "path";
                }
            }
            else if (attributeName == "EventHubTriggerAttribute" &&
                attribute.GetType().Assembly.GetName().Version.Major == 2)
            {
                if (propertyName == "EventHubName")
                {
                    return "path";
                }
            }
            else if (attributeName == "ServiceBusTriggerAttribute")
            {
                if (propertyName == "Access")
                {
                    return "accessRights";
                }
            }
            else if (attributeName == "TimerTriggerAttribute")
            {
                if (propertyName == "ScheduleExpression")
                {
                    return "schedule";
                }
            }
            else if (attributeName == "ApiHubFileTrigger")
            {
                if (propertyName == "ConnectionStringSetting")
                {
                    return "connection";
                }
            }

            return propertyName.ToLowerFirstCharacter();
        }
    }
}