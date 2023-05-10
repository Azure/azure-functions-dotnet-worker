using System.CommandLine;

namespace FunctionsNetHost
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Log("Starting FunctionsNetHost V2v 1138");

            GrpcWorkerStartupOptions grpcOptions = new GrpcWorkerStartupOptions() ;


            var hostOption = new Option<string>( "--host");
            var portOption = new Option<int>("--port");
            var workerOption = new Option<string>("--workerId");

            var rootCommand = new RootCommand();
            rootCommand.AddOption(portOption);
            rootCommand.AddOption(hostOption);
            rootCommand.AddOption(workerOption);

            rootCommand.SetHandler((host, port, workerId) =>
            {
                grpcOptions.Host = host;
                grpcOptions.Port = port;
                grpcOptions.WorkerId = workerId;
                grpcOptions.GrpcMaxMessageLength = int.MaxValue;
            },
            hostOption, portOption, workerOption);

            await rootCommand.InvokeAsync(args);

            var client = new MyClient(grpcOptions);

            await client.InitAsync();
        }
    }
}
