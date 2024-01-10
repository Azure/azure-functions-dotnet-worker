namespace App
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Pinger");

            string pingUrl;
            if (args.Length > 0)
            {
                pingUrl = args[0];
            }
            else
            {
                pingUrl = "https://www.bing.com/version";
            }
            var response = await Pinger.PingAsync(pingUrl);
            Console.WriteLine($"{pingUrl} - {response.StatusCode} ({response.LatencyMilliseconds}ms)");
            Console.WriteLine("Exiting Pinger");
        }
    }
}
