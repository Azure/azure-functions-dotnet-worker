using System;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights
{
    internal class FunctionsRoleInstanceProvider
    {
        internal const string ComputerNameKey = "COMPUTERNAME";
        internal const string WebSiteInstanceIdKey = "WEBSITE_INSTANCE_ID";
        internal const string ContainerNameKey = "CONTAINER_NAME";

        private string? _roleInstanceName;

        public string GetRoleInstanceName()
        {
            _roleInstanceName ??= GetRoleInstance();
            return _roleInstanceName;
        }

        private static string GetRoleInstance()
        {
            string instanceName = Environment.GetEnvironmentVariable(WebSiteInstanceIdKey);
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = Environment.GetEnvironmentVariable(ComputerNameKey);
                if (string.IsNullOrEmpty(instanceName))
                {
                    instanceName = Environment.GetEnvironmentVariable(ContainerNameKey);
                }
            }

            return instanceName;
        }
    }
}
