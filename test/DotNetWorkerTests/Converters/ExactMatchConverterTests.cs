using Microsoft.Azure.Functions.Worker.Converters;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    public class ExactMatchConverterTests
    {
        [Fact]
        public void SameType_Succeeds()
        {
            var converter = new ExactMatchConverter();
            var subclass = new SubClass();

            var context = new TestConverterContext("trigger", typeof(SubClass), subclass);

            Assert.True(converter.TryConvert(context, out object target));

            var convertedType = TestUtility.AssertIsTypeAndConvert<SubClass>(target);
            Assert.Equal("subVal", convertedType.MyProperty);
        }

        [Fact]
        public void SubClass_ConvertsTo_BaseClass()
        {
            var converter = new ExactMatchConverter();
            var subclass = new SubClass();

            var context = new TestConverterContext("trigger", typeof(BaseClass), subclass);

            Assert.True(converter.TryConvert(context, out object target));

            var convertedType = TestUtility.AssertIsTypeAndConvert<BaseClass>(target);
            Assert.Equal("subVal", convertedType.MyProperty);
        }
    }

    internal class BaseClass
    {
        public string MyProperty { get; set; } = "baseVal";
    }

    internal class SubClass : BaseClass
    {
        public SubClass()
        {
            MyProperty = "subVal";
        }
    }
}
