using System;
using System.Reflection;
#if NET6_0_OR_GREATER
using System.Runtime.Loader;
#endif
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal partial class DefaultMethodInfoLocator : IMethodInfoLocator
    {
        private const string EntryPointRegexPattern = "^(?<typename>.*)\\.(?<methodname>\\S*)$";
#if NET7_0_OR_GREATER
        private static readonly Regex _entryPointRegex = EntryPointRegex();

        [GeneratedRegex(EntryPointRegexPattern)]
        private static partial Regex EntryPointRegex();
#else
        private static readonly Regex _entryPointRegex = new(EntryPointRegexPattern);
#endif
        public MethodInfo GetMethod(string pathToAssembly, string entryPoint)
        {
            var entryPointMatch = _entryPointRegex.Match(entryPoint);
            if (!entryPointMatch.Success)
            {
                throw new InvalidOperationException("Invalid entry point configuration. The function entry point must be defined in the format <fulltypename>.<methodname>");
            }

            string typeName = entryPointMatch.Groups["typename"].Value;
            string methodName = entryPointMatch.Groups["methodname"].Value;
#if NET6_0_OR_GREATER
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pathToAssembly);
#else
            Assembly assembly = Assembly.LoadFrom(pathToAssembly);
#endif

            Type? functionType = assembly.GetType(typeName);

            MethodInfo? methodInfo = functionType?.GetMethod(methodName);

            if (methodInfo == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' specified in {nameof(FunctionDefinition.EntryPoint)} was not found. This function cannot be created.");
            }

            return methodInfo;
        }


    }
}
