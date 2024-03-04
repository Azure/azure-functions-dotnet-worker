using System;
using System.Runtime.InteropServices;

namespace App
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($".NET version:{RuntimeInformation.FrameworkDescription}");
        }
    }
}
