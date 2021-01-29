using Xunit.Sdk;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    internal static class TestUtility
    {
        public static string DefaultPropertyName = "input";

        public static T AssertIsTypeAndConvert<T>(object target)
            where T : class
        {
            if (target is not T converted)
            {
                throw new AssertActualExpectedException(typeof(T), target?.GetType(), string.Empty);
            }

            return converted;
        }
    }
}
