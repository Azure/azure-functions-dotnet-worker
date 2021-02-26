using System;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal class MethodInfoLocator : IMethodInfoLocator
    {
        private static readonly Regex _entryPointRegex = new Regex("^(?<typename>.*)\\.(?<methodname>\\S*)$");

        public MethodInfo GetMethod(string pathToAssembly, string entryPoint)
        {
            var entryPointMatch = _entryPointRegex.Match(entryPoint);
            if (!entryPointMatch.Success)
            {
                throw new InvalidOperationException("Invalid entry point configuration. The function entry point must be defined in the format <fulltypename>.<methodname>");
            }

            string typeName = entryPointMatch.Groups["typename"].Value;
            string methodName = entryPointMatch.Groups["methodname"].Value;

            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pathToAssembly);

            Type? functionType = assembly.GetType(typeName);

            MethodInfo? methodInfo = functionType?.GetMethod(methodName);

            if (methodInfo == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' specified in {nameof(FunctionMetadata.EntryPoint)} was not found. This function cannot be created.");
            }

            return methodInfo;
        }
    }
}
