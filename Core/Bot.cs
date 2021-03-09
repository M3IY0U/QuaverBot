using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuaverBot.Entities;

namespace QuaverBot.Core
{
    public class Bot : IDisposable
    {
        private readonly DiscordClient _client;
        private CommandsNextExtension _commandsNext;
        private static readonly Regex MapSetRegex = new(@"https?://quavergame\.com/mapset/(\d)+/?");
        private static readonly Regex MapRegex = new(@"https?://quavergame\.com/mapset/map/(\d)+/?");
        private Config Config { get; }

        public Bot()
        {
            // config setup
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

            // make config available for commands through DI
            var services = new ServiceCollection()
                .AddSingleton(Config)
                .BuildServiceProvider();

            // setup client
            _client = new DiscordClient(new DiscordConfiguration
            {
                Token = Config.Token
            });

            // setup commands
            _commandsNext = _client.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDms = false,
                StringPrefixes = Config.Prefixes,
                Services = services
            });

            _client.UseInteractivity();
            // hook command error event to print stacktrace only on unintended exceptions  
            _commandsNext.CommandErrored += async (_, e) =>
            {
                if (e.Exception.Message.Contains("command was not found")) return;
                await e.Context.RespondAsync(
                    $"Error: `{e.Exception.Message}`\n{(e.Exception is CommandException ? "" : $"```{e.Exception.StackTrace}```")}");
            };

            // hook message event to log chart id if link was sent
            _client.MessageCreated += (_, args) =>
            {
                if (args.Author.IsBot || string.IsNullOrEmpty(args.Message.Content)) return Task.CompletedTask;
                var content = args.Message.Content;
                string id;

                if (MapRegex.IsMatch(content))
                    id = MapRegex.Match(args.Message.Content).Value;
                else if (MapSetRegex.IsMatch(content))
                    id = MapSetRegex.Match(args.Message.Content).Value;
                else
                    return Task.CompletedTask;

                Config.GetGuild(args.Guild.Id)
                    .UpdateChartInChannel(args.Channel.Id, Convert.ToInt64(id.Substring(id.LastIndexOf('/') + 1)),
                        false);

                return Task.CompletedTask;
            };

            _client.Ready += (sender, _) =>
            {
                Config.Guilds ??= new List<Guild>();
                foreach (var (id, _) in sender.Guilds)
                {
                    if (!Config.Guilds.Exists(g => g.Id == id))
                        Config.Guilds.Add(new Guild {Id = id, NewRankedMapsUpdates = false, QuaverChannel = 0});
                }

                Config.Save();
                return Task.CompletedTask;
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