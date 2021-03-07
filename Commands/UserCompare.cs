using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using QuaverBot.Entities;
using QuaverBot.Graphics;

namespace QuaverBot.Commands
{
    public class UserCompare : BaseCommandModule
    {
        private readonly Config _config;
        public UserCompare(Config config) => _config = config;

        [Command("usercompare"), Aliases("uc")]
        public async Task CompareUsers(CommandContext ctx, string otherUser, string username = "")
        {
            // get quaver ids
            string qid;
            var mode = GameMode.Key4;
            if (string.IsNullOrEmpty(username))
            {
                var user = _config.Users.Find(x => x.Id == ctx.User.Id);
                if (user == null)
                    throw new CommandException("No Username set.");
                qid = user.QuaverId;
                mode = user.PreferredMode;
            }
            else
                qid = await Util.NameToQid(username);
            var qid2 = await Util.NameToQid(otherUser);
            
            // get user info
            var u1 = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl + 
                                                                               $"/users/full/{qid}")).user;
            var u2 = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                                                                               $"/users/full/{qid2}")).user;
            // set the correct usernames
            username = $"{u1.info.username}";
            otherUser = $"{u2.info.username}";

            // create a "banner" with the two avatars in the corners 
            var banner = CompareBanner.CreateBannerImage($"{u1.info.avatar_url}", $"{u2.info.avatar_url}");
            
            // update dynamic with correct mode
            if (mode == GameMode.Key4)
            {
                u1 = u1.keys4;
                u2 = u2.keys4;
            }
            else
            {
                u1 = u1.keys7;
                u2 = u2.keys7;
            }
            
            // collect all relevant stats
            var stats = new Dictionary<string, KeyValuePair<double, double>>
            {
                {"Rank",        KeyValuePair.Create((double) u1.globalRank, (double) u2.globalRank)},
                {"Score",       KeyValuePair.Create((double) u1.stats.total_score, (double) u2.stats.total_score)},
                {"Accuracy",    KeyValuePair.Create((double) u1.stats.overall_accuracy, (double) u2.stats.overall_accuracy)},
                {"Performance", KeyValuePair.Create((double) u1.stats.overall_performance_rating, (double) u2.stats.overall_performance_rating)},
                {"PlayCount",   KeyValuePair.Create((double) u1.stats.play_count, (double) u2.stats.play_count)},
                {"Max Combo",   KeyValuePair.Create((double) u1.stats.max_combo, (double) u2.stats.max_combo)},
                {"Marv",        KeyValuePair.Create((double) u1.stats.total_marv, (double) u2.stats.total_marv)},
                {"Perf",        KeyValuePair.Create((double) u1.stats.total_perf, (double) u2.stats.total_perf)},
                {"Great",       KeyValuePair.Create((double) u1.stats.total_great, (double) u2.stats.total_great)},
                {"Good",        KeyValuePair.Create((double) u1.stats.total_good, (double) u2.stats.total_good)},
                {"Okay",        KeyValuePair.Create((double) u1.stats.total_okay, (double) u2.stats.total_okay)},
                {"Miss",        KeyValuePair.Create((double) u1.stats.total_miss, (double) u2.stats.total_miss)}
            };

            // strings for the users and the stat names
            var stats1 = "";
            var stats2 = "";
            var names = "";
            
            // loop through all stats and add them accordingly
            foreach (var (stat, (f, s)) in stats)
            {
                names += $"{stat}\n";
                if (Compare(stat, f ,s))
                {
                    stats1 += $"**{Math.Round(f, 2)}**\n";
                    stats2 += $"{Math.Round(s, 2)}\n";
                }
                else
                {
                    stats1 += $"{Math.Round(f, 2)}\n";
                    stats2 += $"**{Math.Round(s, 2)}**\n";
                }
            }

            // create embed & send it
            var eb = new DiscordEmbedBuilder()
                .AddField(username, stats1, true)
                .AddField("​    vs.", names, true)
                .AddField(otherUser, stats2, true)
                .WithImageUrl("attachment://banner.png");

            var reply = new DiscordMessageBuilder()
                .WithFile("banner.png", banner)
                .WithEmbed(eb.Build());

            await ctx.RespondAsync(reply);
        }

        // necessary because less rank = more good
        private static bool Compare(string stat, double first, double second) 
            => stat == "Rank" ? first < second : second < first;
    }
}