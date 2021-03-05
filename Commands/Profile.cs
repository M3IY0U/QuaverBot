using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using QuaverBot.Entities;

namespace QuaverBot.Commands
{
    public class Profile : BaseCommandModule
    {
        private readonly Config _config;
        public Profile(Config config) => _config = config;

        // Overload to support @mentions
        [Command("profile"), Aliases("p"), Priority(1)]
        public async Task GetDiscordUserProfile(CommandContext ctx, DiscordUser user = null, string mode = "4k")
        {
            user ??= ctx.User;
            var qUser = _config.Users.Find(x => x.Id == user.Id);
            if (qUser is not null)
            {
                if (mode == "4k")
                    mode = qUser.PreferredMode == GameMode.Key4 ? "4" : "7";
                await GetProfile(ctx, qUser.Name, mode);
            }
            else
                throw new CommandException("User has not set their account.");
        }
        
        [Command("profile"), Aliases("p")]
        public async Task GetProfile(CommandContext ctx, string username = "", string mode = "4k")
        {
            string qid;
            var gm = GameMode.Key4;
            if (string.IsNullOrEmpty(username))
            {
                var user = _config.Users.Find(x => x.Id == ctx.User.Id);
                if (user == null)
                    throw new CommandException("No Username set.");
                gm = user.PreferredMode;
                qid = user.QuaverId;
            }
            else
                qid = await Util.NameToQid(username);

            if (mode.Contains("7"))
                gm = GameMode.Key7;

            var fullInfo = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/users/full/{qid}")).user;
            var graph = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/users/graph/rank?id={qid}&mode=1")).statistics;

            var info = fullInfo.info;

            var eb = new DiscordEmbedBuilder()
                .WithAuthor($"{info.username}'s Quaver profile", $"https://quavergame.com/user/{qid}")
                .WithColor(ctx.Member.Color)
                .WithThumbnail($"{info.avatar_url}")
                .WithFooter("Currently " + (bool.Parse($"{info.online}") ? "online" : "offline"));
            AddModeStats(ref eb, gm, fullInfo);

            await ctx.RespondAsync(eb.Build());
        }

        private static void AddModeStats(ref DiscordEmbedBuilder eb, GameMode gm, dynamic info)
        {
            switch (gm)
            {
                case GameMode.Key4:
                    eb.AddField("4K",
                        $"Rank » #{info.keys4.globalRank} ({info.country} #{info.keys4.countryRank})\n" +
                        $"Rating » {info.keys4.stats.overall_performance_rating}\n" +
                        $"PlayCount » {info.keys4.stats.play_count} (FailCount » {info.keys4.stats.fail_count}) | Fail% » {Math.Round((int) info.keys4.stats.fail_count * 100f / (int) info.keys4.stats.play_count, 2)}%\n" +
                        $"Total Hits » [{info.keys4.stats.total_marv}/{info.keys4.stats.total_perf}/{info.keys4.stats.total_great}/{info.keys4.stats.total_good}/{info.keys4.stats.total_okay}/{info.keys4.stats.total_miss}]\n" +
                        $"Total Pauses » {info.keys4.stats.total_pauses} 😔", true);
                    break;
                case GameMode.Key7:
                    eb.AddField("7K", $"Rank » #{info.keys4.globalRank} ({info.country} #{info.keys4.countryRank})\n" +
                                      $"Rating » {info.keys7.stats.overall_performance_rating}\n" +
                                      $"PlayCount » {info.keys7.stats.play_count} (FailCount » {info.keys7.stats.fail_count}) | Fail% » {Math.Round((int) info.keys7.stats.fail_count * 100f / (int) info.keys7.stats.play_count, 2)}%\n" +
                                      $"Total Hits » [{info.keys7.stats.total_marv}/{info.keys7.stats.total_perf}/{info.keys7.stats.total_great}/{info.keys7.stats.total_good}/{info.keys7.stats.total_okay}/{info.keys7.stats.total_miss}]\n" +
                                      $"Total Pauses » {info.keys7.stats.total_pauses} 😔", true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gm), gm, null);
            }
        }
    }
}