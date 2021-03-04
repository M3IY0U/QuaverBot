using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace QuaverBot
{
    public class Bot : IDisposable
    {
        private readonly DiscordClient _client;
        private CommandsNextExtension _commandsNext;
        private Config Config { get; }

        public Bot()
        {
            if (File.Exists("config.json"))
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            else
            {
                Config = new Config();
                Config.Save();
                Console.WriteLine(
                    "No config file was found, created a default one, please change the token before starting the Bot again.");
                Environment.Exit(0);
            }

            var services = new ServiceCollection()
                .AddSingleton(Config)
                .BuildServiceProvider();

            _client = new DiscordClient(new DiscordConfiguration
            {
                Token = Config.Token
            });

            _commandsNext = _client.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDms = false,
                StringPrefixes = new[] {"q", "q!"},
                Services = services
            });

            _commandsNext.CommandErrored += async (_, e) =>
            {
                if (e.Exception.Message.Contains("command was not found"))
                    return;
                await e.Context.RespondAsync($"Error: `{e.Exception.Message}`");
            };
            _commandsNext.RegisterCommands(Assembly.GetEntryAssembly());
        }

        public void Dispose()
        {
            _client.Dispose();
            _commandsNext = null;
        }

        public async Task RunAsync()
        {
            await _client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}