using FunctionsDotNetWorker;
using System.Threading.Channels;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace FunctionsDotNetWorkerTests
{
    public class FunctionRpcClientTests
    {
        private FunctionRpcClient _rpcClient;
        private Mock<FunctionRpc.FunctionRpcClient> _clientMock = new Mock<FunctionRpc.FunctionRpcClient>();
        private Mock<FunctionBroker> _functionBrokerMock = new Mock<FunctionBroker>();
        private Mock<Channel<StreamingMessage>> _channelMock = new Mock<Channel<StreamingMessage>>();

        public FunctionRpcClientTests()
        {
            _rpcClient = new FunctionRpcClient(_clientMock.Object, "123", _functionBrokerMock.Object, _channelMock.Object);
        }


    }

}
