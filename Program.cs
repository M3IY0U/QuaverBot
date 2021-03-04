using System;

namespace QuaverBot
{
    class Program
    {
        static void Main(string[] args)
        {
            using var b = new Bot();
            b.RunAsync().Wait();
        }
    }
}