﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public void Execute(SourceGeneratorContext context)
        {
            Compilation compilation = context.Compilation;

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => CurrentDomain_AssemblyResolve(sender, args, compilation);

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
            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();
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

                    Attribute a = CreateAttribute(compilation, attributeData);

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

                    foreach (var prop in a.GetType().GetProperties())
                    {
                        var propertyName = prop.Name;
                        var propertyValue = FormatObject(prop.GetValue(a));

                        if (prop.PropertyType.IsArray)
                        {
                            string jarr = FormatArray(prop.GetValue(a) as IEnumerable);
                            sourceBuilder.AppendLine(jarr);
                            sourceBuilder.Append(@"
                           raw[""" + propertyName + @$"""] = jarr{arrayCount};");

                            arrayCount++;
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

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args, Compilation compilation)
        {
            string assemblyName = new AssemblyName(args.Name).Name;
            var attributeAssemblyReference = compilation.ExternalReferences.First(r => r.Display.Contains(assemblyName));
            return Assembly.LoadFile(attributeAssemblyReference.Display);
        }

        public void Initialize(InitializationContext context)
        {
#if DEBUG
            //Debugger.Launch();
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

        public static Attribute CreateAttribute(Compilation compilation, AttributeData attributeData)
        {
            Type attributeType = ConvertToType(compilation, attributeData.AttributeClass);

            List<object> arguments = new List<object>();
            List<Type> constructorTypes = new List<Type>();

            foreach (TypedConstant arg in attributeData.ConstructorArguments)
            {
                constructorTypes.Add(ConvertToType(compilation, arg.Type));

                switch (arg.Kind)
                {
                    case TypedConstantKind.Error:
                        break;
                    case TypedConstantKind.Primitive:
                    case TypedConstantKind.Enum:
                        arguments.Add(arg.Value);
                        break;
                    case TypedConstantKind.Type:
                        break;
                    case TypedConstantKind.Array:
                        var arrayValues = arg.Values.Select(a => a.Value.ToString()).ToArray();
                        arguments.Add(arrayValues);
                        break;
                    default:
                        break;
                }
            }

            var constructorInfo = attributeType.GetConstructor(constructorTypes.ToArray());

            Attribute attribute = constructorInfo.Invoke(arguments.ToArray()) as Attribute;

            // now apply the named 
            foreach (var namedArgument in attributeData.NamedArguments)
            {
                attributeType.GetProperty(namedArgument.Key)?.SetValue(attribute, namedArgument.Value.Value);
                attributeType.GetField(namedArgument.Key)?.SetValue(attribute, namedArgument.Value.Value);
            }

            return attribute;
        }

        public static Type ConvertToType(Compilation compilation, ITypeSymbol typeSymbol)
        {
            string typeString = $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}";

            if (typeSymbol is IArrayTypeSymbol arrayType)
            {
                var elementType = arrayType.ElementType;
                typeString = $"{elementType.ContainingNamespace}.{elementType.Name}[]";
            }

            Type t = Type.GetType(typeString);

            if (t == null)
            {
                var attributeAssemblyName = typeSymbol.ContainingAssembly.Modules.Single().Locations.Single().MetadataModule.Name;

                //// Find assembly full path
                var attributeAssemblyReference = compilation.ExternalReferences.First(r => r.Display.EndsWith(attributeAssemblyName));
                var a = Assembly.LoadFile(attributeAssemblyReference.Display);
                t = a.GetType(typeSymbol.ToString());
            }

            return t;
        }
    }

}