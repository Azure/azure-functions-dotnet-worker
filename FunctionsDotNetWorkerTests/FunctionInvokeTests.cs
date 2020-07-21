using FunctionsDotNetWorker;
using FunctionsDotNetWorker.Converters;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Moq;
using System.Threading.Channels;
using Xunit;

namespace FunctionsDotNetWorkerTests
{
    public class FunctionInvokeTests
    {
        private readonly Mock<ParameterConverterManager> _parameterConverterManagerMock = new Mock<ParameterConverterManager>(MockBehavior.Strict);
        private readonly Mock<Channel<StreamingMessage>> _workerChannelMock = new Mock<Channel<StreamingMessage>>(MockBehavior.Strict);
        private FunctionBroker _functionBroker;

        public FunctionInvokeTests()
        {
            _functionBroker = new FunctionBroker(_parameterConverterManagerMock.Object, _workerChannelMock.Object);
        }

        [Fact]
        public void FunctionNotFoundTest()
        {

        }

        [Fact]
        public void FunctionParamNotInInputDataTest()
        {

        }

        [Fact]
        public void FunctionConverterNotFoundTest()
        {
            //_parameterConverterManagerMock.Setup(p => p.TryConvert(It.IsAny<object>(), It.IsAny<System.Type>(), It.IsAny<string>(), )).Returns(false);
        }

        [Fact]
        public void TargetSameAsSourceTest()
        {
            // any situation where there might not be a converter but target == source anyways so it doesn't matter? 
        }
    }

}
