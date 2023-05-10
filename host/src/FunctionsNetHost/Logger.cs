using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsNetHost
{
    internal class Logger
    {
        internal static void Log(string message)
        {
            string ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Console.WriteLine($"LaanguageWorkerConsoleLog [{ts}] [FunctionsNetHost] {message}");
        }
    }
}
