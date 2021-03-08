using QuaverBot.Core;

namespace QuaverBot
{
    class Program
    {
        static void Main()
        {
            using var b = new Bot();
            b.RunAsync().Wait();
        }
    }
}