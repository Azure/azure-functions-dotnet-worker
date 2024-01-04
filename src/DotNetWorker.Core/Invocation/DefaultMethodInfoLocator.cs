using System;
using System.Linq;
using System.Reflection;
#if NET5_0_OR_GREATER
using System.Runtime.Loader;
#endif
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal class DefaultMethodInfoLocator : IMethodInfoLocator
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
#if NET5_0_OR_GREATER
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pathToAssembly);
#else
            Assembly assembly = Assembly.LoadFrom(pathToAssembly);
#endif

            Type? functionType = assembly.GetType(typeName);

            MethodInfo? methodInfo = functionType?
                .GetMethods()
                .Where(x => x.Name.Equals(methodName))
                .FirstOrDefault(m => m.CustomAttributes.Any(ca => ca.AttributeType.FullName == "Microsoft.Azure.Functions.Worker.FunctionAttribute"));

            if (methodInfo == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' specified in {nameof(FunctionDefinition.EntryPoint)} was not found. This function cannot be created.");
            }

            return methodInfo;
        }
    }
}
