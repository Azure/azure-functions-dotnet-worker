
using FunctionsNetHost.ManagedLoader;

namespace Microsoft.Azure.Functions.Worker.ManagedLoader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.Log("Starting FunctionsNetHost ManagedAppLoader");

            var loader = new ManagedAppLoader();
            loader.Start();
        }
    }
}
