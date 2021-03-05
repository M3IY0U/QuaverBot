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
        [Command("profile"), Priority(1)]
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
            var keys = info.keys7;
            if (gm == GameMode.Key4)
                keys = info.keys4;

            eb.AddField(gm == GameMode.Key4 ? "4K" : "7K",
                $"Rank » #{keys.globalRank} ({info.info.country} #{keys.countryRank})\n" +
                $"Rating » {Math.Round((double) keys.stats.overall_performance_rating, 2)}\n" +
                $"PlayCount » {keys.stats.play_count} (FailCount » {keys.stats.fail_count}) | Fail% » {Math.Round((int) keys.stats.fail_count * 100f / (int) keys.stats.play_count, 2)}%\n" +
                $"Total Hits » [{keys.stats.total_marv}/{keys.stats.total_perf}/{keys.stats.total_great}/{keys.stats.total_good}/{keys.stats.total_okay}/{keys.stats.total_miss}]\n" +
                $"Total Pauses » {keys.stats.total_pauses} 😔", true);
        }
    }
}