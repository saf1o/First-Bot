using dotenv.net;
using System.Threading.Tasks;

namespace Bot1
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { "C:/FirstBot/Bot1/.env" }));
            var bot = new Bot();
            await bot.RunAsync();
        }
    }
}