using System.Linq;
using Mono.Cecil;

namespace SourceGenerator
{
    internal static class ParameterInfoExtensions
    {
        /// <summary>
        /// A parameter is an SDK parameter if it has at lease 1 SDK attribute.
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <returns></returns>
        public static bool IsWebJobSdkTriggerParameter(this ParameterDefinition parameterInfo)
        {
            return parameterInfo
               .CustomAttributes
               .Any(a => a.IsWebJobsAttribute() && a.ToAttributeFriendlyName().IndexOf("Trigger") > -1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <returns></returns>
        public static CustomAttribute GetDisabledAttribute(this ParameterDefinition parameterInfo)
        {
            return parameterInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Microsoft.Azure.WebJobs.DisableAttribute");
        }
    }
}