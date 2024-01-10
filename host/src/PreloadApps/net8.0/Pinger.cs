
using System.Diagnostics;

namespace App
{
    internal record PingResult(int StatusCode, int LatencyMilliseconds);

    internal class Pinger
    {
        internal static async Task<PingResult> PingAsync(string url)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                using var client = new HttpClient();
                using var response = await client.GetAsync(url);
                sw.Stop();
                var elapsed = sw.ElapsedMilliseconds;

                return new PingResult((int)response.StatusCode, (int)elapsed);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in PingAsync: {ex.Message}");
                return new PingResult(0, 0);
            }
        }
    }
}
