using Mono.Cecil;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal static class TypeReferenceExtensions
    {
        public static string GetReflectionFullName(this TypeReference typeRef)
        {
            return typeRef.FullName.Replace("/", "+");
        }
    }
}
