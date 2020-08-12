using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace SourceGenerator
{
    internal static class MethodInfoExtensions
    {
        /// <summary>
        /// A method is an SDK method if it has a FunctionNameAttribute AND at least one parameter has an SDK attribute or the method has a NoAutomaticTriggerAttribute.
        /// </summary>
        /// <param name="method">method to check if an SDK method or not.</param>
        /// <returns>true if <paramref name="method"/> is a WebJobs SDK method. False otherwise.</returns>
        public static bool IsWebJobsSdkMethod(this MethodDefinition method)
        {
            return method.HasFunctionNameAttribute() && method.HasValidWebJobSdkTriggerAttribute();
        }

        public static bool HasValidWebJobSdkTriggerAttribute(this MethodDefinition method)
        {
            var hasNoAutomaticTrigger = method.HasNoAutomaticTriggerAttribute();
            var hasTrigger = method.HasTriggerAttribute();
            return (hasNoAutomaticTrigger || hasTrigger) && !(hasNoAutomaticTrigger && hasTrigger);
        }

        public static bool HasFunctionNameAttribute(this MethodDefinition method)
        {
            return method.CustomAttributes.FirstOrDefault(d => d.AttributeType.FullName == "Microsoft.Azure.WebJobs.FunctionNameAttribute") != null;
        }

        public static bool HasNoAutomaticTriggerAttribute(this MethodDefinition method)
        {
            return method.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Microsoft.Azure.WebJobs.NoAutomaticTriggerAttribute") != null;
        }

        public static bool HasTriggerAttribute(this MethodDefinition method)
        {
            return method.Parameters.Any(p => p.IsWebJobSdkTriggerParameter());
        }

        /// <summary>
        /// Gets a function name from a <paramref name="method"/>
        /// </summary>
        /// <param name="method">method has to be a WebJobs SDK method. <see cref="IsWebJobsSdkMethod(MethodInfo)"/></param>
        /// <returns>Function name.</returns>
        public static string GetSdkFunctionName(this MethodDefinition method)
        {
            if (!method.IsWebJobsSdkMethod())
            {
                throw new ArgumentException($"{nameof(method)} has to be a WebJob SDK function");
            }

            string functionName = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "FunctionNameAttribute")?.ConstructorArguments[0].Value.ToString();
            if (functionName != null)
            {
                return functionName;
            }
            else
            {
                throw new InvalidOperationException("Missing FunctionNameAttribute");
            }
        }

        /// <summary>
        /// A method is disabled is any of it's parameters have [Disabled] attribute
        /// or if the method itself or class have the [Disabled] attribute. The overloads
        /// are stringified so that the ScriptHost will do its job.
        /// </summary>
        /// <param name="method"></param>
        /// <returns>a boolean true or false if the outcome is fixed, a string if the ScriptHost should interpret it</returns>
        public static object GetDisabled(this MethodDefinition method)
        {
            var customAttribute = method.Parameters.Select(p => p.GetDisabledAttribute()).Where(a => a != null).FirstOrDefault() ??
                method.GetDisabledAttribute() ??
                method.DeclaringType.GetDisabledAttribute();

            if (customAttribute != null)
            {
                var attribute = customAttribute.ToReflection();

                // With a SettingName defined, just put that as string. The ScriptHost will evaluate it.
                var settingName = attribute.GetValue<string>("SettingName");
                if (!string.IsNullOrEmpty(settingName))
                {
                    return settingName;
                }

                var providerType = attribute.GetValue<Type>("ProviderType");
                if (providerType != null)
                {
                    return providerType;
                }

                // With neither settingName or providerType, no arguments were given and it should always be true

                return true;
            }

            // No attribute means not disabled
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static CustomAttribute GetDisabledAttribute(this MethodDefinition method)
        {
            return method.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Microsoft.Azure.WebJobs.DisableAttribute");
        }

        /// <summary>
        /// A method has an unsupported attributes if it has any of the following:
        ///     1) [Disabled("%settingName%")]
        ///     2) [Disabled(typeof(TypeName))]
        /// However this [Disabled("settingName")] is valid.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool HasUnsuportedAttributes(this MethodDefinition method, out string error)
        {
            error = string.Empty;
            var disabled = method.GetDisabled();
            if (disabled is string disabledStr &&
                disabledStr.StartsWith("%") &&
                disabledStr.EndsWith("%"))
            {
                error = "'%' expressions are not supported for 'Disable'. Use 'Disable(\"settingName\") instead of 'Disable(\"%settingName%\")'";
                return true;
            }
            else if (disabled is Type)
            {
                error = "the constructor 'DisableAttribute(Type)' is not supported.";
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}