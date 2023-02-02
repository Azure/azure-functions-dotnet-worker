using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


internal partial class FunctionExecutorGenerator
{
    internal class Parser
    {
        private readonly GeneratorExecutionContext _context;

        private Compilation Compilation => _context.Compilation;

        private CancellationToken CancellationToken => _context.CancellationToken;

        private KnownTypes _knownTypes;

        public Parser(GeneratorExecutionContext context)
        {
            _context = context;
            _knownTypes = new KnownTypes(_context.Compilation);
        }

        internal IEnumerable<FuncInfo> Get(List<MethodDeclarationSyntax> methods)
        {
            Dictionary<string, ClassInfo> classDict = new Dictionary<string, ClassInfo>();

            var functionList = new List<FuncInfo>();
            foreach (MethodDeclarationSyntax method in methods)
            {
                CancellationToken.ThrowIfCancellationRequested();

                var model = Compilation.GetSemanticModel(method.SyntaxTree);

                if (!FunctionsUtil.IsValidMethodAzureFunction(_context, Compilation, model, method, out string? functionName))
                {
                    continue;
                }

                var methodName = method.Identifier.Text;

                var methodParameterList = new List<string>(method.ParameterList.Parameters.Count);

                foreach (var methodParam in method.ParameterList.Parameters)
                {
                    if (model.GetDeclaredSymbol(methodParam) is not IParameterSymbol parameterSymbol) continue;

                    methodParameterList.Add(parameterSymbol.Type.ToDisplayString());
                }

                var methodSymSemanticModel = Compilation.GetSemanticModel(method.SyntaxTree);
                IMethodSymbol methodSymbol = methodSymSemanticModel.GetDeclaredSymbol(method)!;
                var fullyQualifiedClassName = methodSymbol.ContainingSymbol.ToDisplayString();

                ClassDeclarationSyntax functionClass = (ClassDeclarationSyntax)method.Parent!;
                var entryPoint = $"{fullyQualifiedClassName}.{methodName}";

                if (!classDict.TryGetValue(entryPoint, out var classInfo))
                {
                    classInfo = new ClassInfo(fullyQualifiedClassName)
                    {
                        ConstructorArgumentTypeNames = GetConstructorParamTypeNames(functionClass, model)
                    };
                    classDict[entryPoint] = classInfo;
                }

                var funcInfo = new FuncInfo(entryPoint!)
                {
                    ParameterTypeNames = methodParameterList,
                    MethodName = methodName,
                    ShouldAwait = IsTaskType(methodSymbol.ReturnType),
                    IsReturnValueAssignable = IsReturnValueAssignable(methodSymbol),
                    IsStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword)
                };
                funcInfo.ParentClass = classInfo;

                functionList.Add(funcInfo);
            }

            return functionList;
        }

        private bool IsTaskType(ITypeSymbol typeSymbol)
        {
            return SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.TaskType)
                || SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.TaskOfTType);

        }
        private bool IsReturnValueAssignable(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.ReturnsVoid)
            {
                return false;
            }

            if(SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType.OriginalDefinition, _knownTypes.TaskType))
            {
                return false;
            }

            return true;
        }

        private static IEnumerable<string> GetConstructorParamTypeNames(ClassDeclarationSyntax functionClass,
            SemanticModel model)
        {
            var firstConstructorMember = GetBestConstructor(functionClass);

            if (firstConstructorMember is not ConstructorDeclarationSyntax constructorSyntax)
            {
                return Enumerable.Empty<string>();
            }

            var constructorParams = new List<string>(constructorSyntax.ParameterList.Parameters.Count);

            foreach (var param in constructorSyntax.ParameterList.Parameters)
            {
                if (model.GetDeclaredSymbol(param) is not IParameterSymbol parameterSymbol) continue;

                constructorParams.Add(parameterSymbol.Type.ToDisplayString());
            }

            return constructorParams;
        }

        private static MemberDeclarationSyntax GetBestConstructor(ClassDeclarationSyntax functionClass)
        {
            // to do: Use a better algo for this instead of picking first constructor.
            var firstConstructorMember =
                functionClass.Members.FirstOrDefault(member => member is ConstructorDeclarationSyntax);

            return firstConstructorMember;
        }
    }

    public class FuncInfo
    {
        internal FuncInfo(string functionName)
        {
            FunctionName = functionName;
        }

        /// <summary>
        ///  Task or Void
        /// </summary>
        public bool IsReturnValueAssignable { set; get; }

        public bool ShouldAwait { get; set; }
        public string MethodName { get; set; }

        public bool IsStatic { get; set; }
        public string FunctionName { get; }

        public ClassInfo ParentClass { set; get; }

        public IEnumerable<string> ParameterTypeNames { set; get; } = Enumerable.Empty<string>();
    }

    public class ClassInfo
    {
        public ClassInfo(string className)
        {
            ClassName = className;
        }

        public IEnumerable<string> ConstructorArgumentTypeNames { set; get; } = Enumerable.Empty<string>();

        public string ClassName { get; }

    }
}
