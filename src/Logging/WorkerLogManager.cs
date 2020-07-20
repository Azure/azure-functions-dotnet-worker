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
    public class WorkerLogManager
    {
        private BlockingCollection<StreamingMessage> _blockingCollectionQueue;

        public void AddBlockingQueue(BlockingCollection<StreamingMessage> queue)
        {
            _blockingCollectionQueue = queue;
        }

        public InvocationLogger GetInvocationLogger(string invocationId)
        {
            var logger = new InvocationLogger(invocationId, _blockingCollectionQueue);

            //logger.setLevel(Level.ALL);
            //addHostClientHandlers(logger, invocationId);
            return logger;
        }
    }
}
