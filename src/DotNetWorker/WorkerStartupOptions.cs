
namespace Microsoft.Azure.Functions.Worker
{
    internal class WorkerStartupOptions
    {
        public string? Host { get; set; }

        public int Port { get; set; }

        public string? WorkerId { get; set; }

        public string? RequestId { get; set; }

        public int MaxMessageLength { get; set; }
    }
}
