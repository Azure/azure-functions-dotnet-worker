using CommandLine;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class WorkerArguments
    {
        [Option("host", Required = true, HelpText = "IP Address used to connect to the Host via gRPC.")]
        public string Host { get; set; }

        [Option("port", Required = true, HelpText = "Port used to connect to the Host via gRPC.")]
        public int Port { get; set; }

        [Option("workerId", Required = true, HelpText = "Worker ID assigned to this language worker.")]
        public string WorkerId { get; set; }

        [Option("requestId", Required = true, HelpText = "Request ID used for gRPC communication with the Host.")]
        public string RequestId { get; set; }

        [Option("grpcMaxMessageLength", Required = true, HelpText = "gRPC Maximum message size.")]
        public int MaxMessageLength { get; set; }
    }
}
