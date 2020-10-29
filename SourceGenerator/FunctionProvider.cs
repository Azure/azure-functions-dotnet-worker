using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerator
{
    [Generator]
    public class FunctionProvider : ISourceGenerator
    {
        private static int arrayCount = 0;

        public void Execute(GeneratorExecutionContext context)
        {
            // Suggested here: https://github.com/dotnet/roslyn/issues/46084
            // Though the underlying issue is fixed, the default severity set there is warning.
            // This way allows us to throw an actual error on any failure.
            try
            {
                ExecuteInternal(context);
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "AFG1001",
                        "An exception was thrown by the Functions generator",
                        "An exception was thrown by the Functions generator: '{0}'",
                        "SourceGenerator",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Location.None,
                    e.ToString()));
            }
        }

        private void ExecuteInternal(GeneratorExecutionContext context)
        {
            Compilation compilation = context.Compilation;

            // begin creating the source we'll inject into the users compilation
            var sourceBuilder = new StringBuilder(@"
            using Microsoft.Azure.WebJobs.Script.Description;
            using System.Collections.Immutable;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using FunctionProviderGenerator;
            using Microsoft.Azure.WebJobs;
            using Microsoft.Azure.WebJobs.Hosting;
            using Microsoft.Azure.WebJobs.Extensions.Http;
            using Microsoft.Extensions.DependencyInjection;
            using Newtonsoft.Json;
            using Newtonsoft.Json.Converters;
            using Newtonsoft.Json.Linq;
 
            [assembly: WebJobsStartup(typeof(Startup))]
 
            namespace FunctionProviderGenerator
             {
                public class Startup : IWebJobsStartup
                {
                    public void Configure(IWebJobsBuilder builder)
                    {           
                        builder.Services.AddSingleton<IFunctionProvider, DefaultFunctionProvider>();
                    }
                }");

            sourceBuilder.Append(@"
             public class DefaultFunctionProvider : IFunctionProvider
            {
                public ImmutableDictionary<string, ImmutableArray<string>> FunctionErrors { get; }

                public Task<ImmutableArray<FunctionMetadata>> GetFunctionMetadataAsync()
                {
                    var metadataList = new List<FunctionMetadata>();
                    var raw = new JObject();");

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // loop over the candidate fields, and keep the ones that are actually annotated
            foreach (MethodDeclarationSyntax method in receiver.CandidateMethods)
            {
                var assemblyName = compilation.Assembly.Name;
                var functionClass = (ClassDeclarationSyntax)method.Parent;
                var functionName = functionClass.Identifier.ValueText;
                var scriptFile = Path.Combine("bin/" + assemblyName + ".dll");
                var entryPoint = assemblyName + "." + functionName + "." + method.Identifier.ValueText;

                sourceBuilder.AppendLine($"var {functionName} = new FunctionMetadata");
                sourceBuilder.AppendLine("{");
                sourceBuilder.AppendLine($"Name = \"{functionName}\",");
                sourceBuilder.AppendLine($"ScriptFile = \"{scriptFile}\",");
                sourceBuilder.AppendLine($"EntryPoint = \"{entryPoint}\",");
                sourceBuilder.AppendLine($"Language = \"dotnet5\",");
                sourceBuilder.AppendLine("};");
                sourceBuilder.AppendLine($"{functionName}.Properties[\"IsCodeless\"] = false;");

                var model = compilation.GetSemanticModel(method.SyntaxTree);

                foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
                {
                    if (parameter.AttributeLists.Count == 0)
                    {
                        continue;
                    }

                    IParameterSymbol parameterSymbol = model.GetDeclaredSymbol(parameter) as IParameterSymbol;
                    AttributeData attributeData = parameterSymbol.GetAttributes().First();

                    AttributeSyntax attributeSyntax = parameter.AttributeLists.First().Attributes.First();
                    IMethodSymbol attribMethodSymbol = model.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;

                    if (attribMethodSymbol?.Parameters is null)
                    {
                        throw new InvalidOperationException($"The constructor of attribute with syntax '{nameof(attributeSyntax)}' is invalid");
                    }

                    IDictionary<string, object> attributeProperties = GetAttributeProperties(attribMethodSymbol, attributeData);

                    string attributeName = attributeData.AttributeClass.Name;

                    // create binding metadata w/ info below and add to function metadata created above
                    var triggerName = parameter.Identifier.ValueText; // correct?
                    var triggerType = attributeName.Replace("Attribute", "");

                    var bindingDirection = "in";

                    if (parameterSymbol.Type is INamedTypeSymbol parameterNamedType &&
                        parameterNamedType.IsGenericType &&
                        parameterNamedType.ConstructUnboundGenericType().ToString() == "Microsoft.Azure.Functions.DotNetWorker.OutputBinding<>")
                    {
                        bindingDirection = "out";
                    }

                    // create raw JObject for the BindingMetadata
                    sourceBuilder.Append(@"raw = new JObject();
                        raw[""name""] = """ + triggerName + @""";
                        raw[""direction""] = """ + bindingDirection + @""";
                        raw[""type""] = """ + triggerType + @""";");

                    foreach (var prop in attributeProperties)
                    {
                        var propertyName = prop.Key;
                        var propertyValue = FormatObject(prop.Value);

                        if (prop.Value.GetType().IsArray)
                        {
                            string jarr = FormatArray(prop.Value as IEnumerable);
                            sourceBuilder.AppendLine(jarr);
                            sourceBuilder.Append(@"raw[""" + propertyName + @$"""] = jarr{arrayCount++};");
                        }
                        else
                        {
                            sourceBuilder.Append(@"
                           raw[""" + propertyName + @"""] =" + propertyValue + ";");
                        }
                    }

                    sourceBuilder.Append(functionName + @".Bindings.Add(BindingMetadata.Create(raw));");

                    // auto-add a return type for http for now
                    if (string.Equals(triggerType, "httptrigger", StringComparison.OrdinalIgnoreCase))
                    {
                        sourceBuilder.Append(@"raw = new JObject();
                        raw[""name""] = ""$return"";
                        raw[""direction""] = ""out"";
                        raw[""type""] = ""http"";");

                        sourceBuilder.AppendLine(functionName + @".Bindings.Add(BindingMetadata.Create(raw));");
                    }
                }
                sourceBuilder.AppendLine("metadataList.Add(" + functionName + ");");
            }

            sourceBuilder.Append(@"
                    return Task.FromResult(metadataList.ToImmutableArray());
                  }
               }
            }");

            // inject the created source into the users compilation
            context.AddSource("DefaultFunctionProvider.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private static string FormatObject(object propValue)
        {
            string s = null;

            if (propValue != null)
            {
                s = "\"" + propValue.ToString() + "\"";
            }
            else
            {
                s = "null";
            }

            return s;
        }

        private static string FormatArray(IEnumerable enumerableValues)
        {
            string code;
            Type propType = enumerableValues?.GetType();

            Type elementType = propType.GetElementType();

            code = $"var arr{arrayCount} = new {elementType}[] {{";

            bool first = true;
            foreach (var o in enumerableValues)
            {
                if (!first)
                {
                    code += ", ";
                }
                else
                {
                    first = false;
                }

                code += FormatObject(o);
            }

            code.TrimEnd(',', ' ');
            code += "};";

            code += $"var jarr{arrayCount} = new JArray(arr{arrayCount});";

            return code;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            // Debugger.Launch();
#endif
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<MethodDeclarationSyntax> CandidateMethods { get; } = new List<MethodDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is MethodDeclarationSyntax methodSyntax)
                {
                    foreach (var p in methodSyntax.ParameterList.Parameters)
                    {
                        if (p.AttributeLists.Count > 0)
                        {
                            CandidateMethods.Add(methodSyntax);
                            break;
                        }
                    }
                }
            }
        }

        internal static IDictionary<string, object> GetAttributeProperties(IMethodSymbol attribMethodSymbol, AttributeData attributeData)
        {
            Dictionary<string, object> argumentData = new();
            if (attributeData.ConstructorArguments.Any())
            {
                LoadConstructorArguments(attribMethodSymbol, attributeData, argumentData);
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Value.Value != null)
                {
                    argumentData[namedArgument.Key] = namedArgument.Value.Value;
                }
            }

            return argumentData;
        }

        internal static void LoadConstructorArguments(IMethodSymbol attribMethodSymbol, AttributeData attributeData, IDictionary<string, object> dict)
        {
            if (attribMethodSymbol.Parameters.Length < attributeData.ConstructorArguments.Length)
            {
                throw new InvalidOperationException($"The constructor at '{nameof(attribMethodSymbol)}' has less total arguments than '{nameof(attributeData)}'.");
            }

            // It's fair to assume than constructor arguments appear before named arguments, and
            // that the constructor names would match the property names
            for (int i = 0; i < attributeData.ConstructorArguments.Length; i++)
            {
                var argumentName = attribMethodSymbol.Parameters[i].Name;

                var arg = attributeData.ConstructorArguments[i];
                switch (arg.Kind)
                {
                    case TypedConstantKind.Error:
                        break;
                    case TypedConstantKind.Primitive:
                    case TypedConstantKind.Enum:
                        dict[argumentName] = arg.Value;
                        break;
                    case TypedConstantKind.Type:
                        break;
                    case TypedConstantKind.Array:
                        var arrayValues = arg.Values.Select(a => a.Value.ToString()).ToArray();
                        dict[argumentName] = arrayValues;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}