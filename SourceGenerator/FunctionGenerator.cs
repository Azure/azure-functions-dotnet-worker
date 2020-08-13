using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerator
{
    [Generator]
    public class FunctionProvider : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // begin creating the source we'll inject into the users compilation
            var sourceBuilder = new StringBuilder(@"
            using Microsoft.Azure.WebJobs.Script.Description;
            using System.Collections.Immutable;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using FunctionProviderGenerator;
            using Microsoft.Azure.WebJobs;
            using Microsoft.Azure.WebJobs.Hosting;
            using Microsoft.Extensions.DependencyInjection;
 
            [assembly: WebJobsStartup(typeof(Startup))]
 
            namespace FunctionProviderGenerator
             {
                public class Startup : IWebJobsStartup
                {
                    public void Configure(IWebJobsBuilder builder)
                    {              
                    }
                }
             }");

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            Compilation compilation = context.Compilation;

            // loop over the candidate fields, and keep the ones that are actually annotated
            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();
            foreach (ParameterSyntax parameter in receiver.CandidateParameters)
            {
                SemanticModel model = compilation.GetSemanticModel(parameter.SyntaxTree);

                foreach (AttributeListSyntax attribute in parameter.AttributeLists)
                {
                    // get functionName
                    var functionClass = (ClassDeclarationSyntax)parameter.Parent.Parent.Parent;
                    var functionName = functionClass.Identifier.ValueText;
                    // get ScriptFile
                    var scriptFile = parameter.SyntaxTree.FilePath;
                    // get entry point (method name i think)
                    var functionMethod = (MethodDeclarationSyntax)parameter.Parent.Parent;
                    var entryPoint = functionMethod.Identifier.ValueText;
                    var language = "dotnet5";

                    // build new function metadata with info above

                    // create binding metadata w/ info below and add to function metadata created above
                    var triggerName = parameter.Identifier.ValueText; // correct
                    var triggerDirection = "BindingDirection.In"; //hard code binding direction for now? 
                    var triggerType = "trigger"; // parameter.Type.ToString(); returns the type like HttpRequestData not TriggerType
                }
            }

            // inject the created source into the users compilation
            context.AddSource("DefaultFunctionProvider.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(InitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ParameterSyntax> CandidateParameters { get; } = new List<ParameterSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is ParameterSyntax parameterSyntax
                    && parameterSyntax.AttributeLists.Count > 0)
                {
                    CandidateParameters.Add(parameterSyntax);
                }
            }
        }
    }

}