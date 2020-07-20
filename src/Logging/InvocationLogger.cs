using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Logging;

namespace FunctionsDotNetWorker.Logging
{
    public class InvocationLogger : ILogger
    {
        private string _invocationId;
        private BlockingCollection<StreamingMessage> _blockingQueue;

        public InvocationLogger(string invocationId, BlockingCollection<StreamingMessage> messageQueue)
        {
            _invocationId = invocationId;
            _blockingQueue = messageQueue;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var response = new StreamingMessage();
            string message = formatter(state, exception);
            response.RpcLog = new RpcLog() { InvocationId = _invocationId, EventId = eventId.ToString(), Message = message};
            _blockingQueue.Add(response);
        }
    }
}
