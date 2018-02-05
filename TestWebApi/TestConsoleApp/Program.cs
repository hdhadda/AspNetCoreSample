using Microsoft.AspNetCore.Hosting;
using System;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            IWebHost host = TestWebApi.HostEntry.BuildWebHost(null);
            host.Run();
            Console.WriteLine("Should not get hit!");
        }
    }
}
