using System.Runtime.CompilerServices;
using VerifyTests;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifySourceGenerators.Initialize();
        }
    }
}
