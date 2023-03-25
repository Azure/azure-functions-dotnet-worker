using static System.Net.Mime.MediaTypeNames;

namespace ManagedLoader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            new ManagedAppLoader().Start();
        }
    }
}
