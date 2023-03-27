using System.Globalization;

namespace FunctionsNetHost.ManagedLoader
{
    internal static class Logger
    {
        public static void Log(string message)
        {
            string ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Console.WriteLine($"{ts} [FunctionsNetHost.ManagedAppLoader] { message}");
        }
    }
}
