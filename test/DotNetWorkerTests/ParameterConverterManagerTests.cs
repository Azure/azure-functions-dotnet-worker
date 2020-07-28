using System.Collections.Generic;
using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.DotNetWorkerTests
{
    public class ParameterConverterManagerTests
    {
        private readonly Mock<IParameterConverter> _iParamMock = new Mock<IParameterConverter>(MockBehavior.Strict);
        private ParameterConverterManager _paramConverterManager;

        public ParameterConverterManagerTests()
        {
            List<IParameterConverter> mockList = new List<IParameterConverter>();
            mockList.Add(_iParamMock.Object);
            _paramConverterManager = new ParameterConverterManager(mockList);
        }

        [Fact]
        public void TryConvertIsSucessfulTest()
        {
            var emptyObject = new object();
            _iParamMock.Setup(p => p.TryConvert(It.IsAny<object>(), It.IsAny<System.Type>(), It.IsAny<string>(), out emptyObject)).Returns(true);
            Assert.True(_paramConverterManager.TryConvert("source", typeof(string), "name", out emptyObject));
        }

        [Fact]
        public void TryConvertFailsTest()
        {
            var emptyObject = new object();
            _iParamMock.Setup(p => p.TryConvert(It.IsAny<object>(), It.IsAny<System.Type>(), It.IsAny<string>(), out emptyObject)).Returns(false);
            Assert.False(_paramConverterManager.TryConvert("source", typeof(string), "name", out emptyObject));
        }
    }

}
