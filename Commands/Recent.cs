using System;
using System.IO;
using Newtonsoft.Json;
using QuaverBot.Graphics;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using System.Collections.Generic;
using DSharpPlus.CommandsNext.Attributes;
using QuaverBot.Core;
using QuaverBot.Entities;

namespace QuaverBot.Commands
{
    public class Recent : BaseCommandModule
    {
        private readonly Config _config;
        public Recent(Config config) => _config = config;

        // Overload to support @mentions
        [Command("recent"), Priority(1)]
        public async Task GetRecentDiscordUser(CommandContext ctx, DiscordUser user = null, string mode = "4k")
        {
            // if user was null, set it to the command user
            user ??= ctx.User;
            // get user and mode preference, then execute main command function
            var qUser = _config.Users.Find(x => x.Id == user.Id);
            if (qUser is not null)
            {
                if (mode == "4k")
                    mode = qUser.PreferredMode == GameMode.Key4 ? "4" : "7";
                await GetRecent(ctx, qUser.Name, mode);
            }
            else
                throw new CommandException("User has not set their account. Use qset [name] to set it.");
        }

        [Command("recent"), Aliases("r", "rs"), Priority(2)]
        public async Task GetRecent(CommandContext ctx, string username = "", string mode = "4k")
        {
            // get quaver id
            string qid;
            if (string.IsNullOrEmpty(username))
            {
                var user = _config.Users.Find(x => x.Id == ctx.User.Id);
                if (user == null)
                    throw new CommandException("No Username set. Use qset [name] to set it.");
                username = user.Name;
                qid = user.QuaverId;
            }
            else
                qid = await Util.NameToQid(username);

            // get needed responses
            dynamic recent;
            try
            {
                recent = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                    $"/users/scores/recent?id={qid}&mode={(mode.Contains("4") ? "1" : "2")}")).scores[0];
            }
            catch (Exception)
            {
                throw new CommandException("No recent plays found for queried user/gamemode.");
            }

            var map = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/maps/{recent.map.id}")).map;

            var info = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/users?id={qid}")).users[0];

            // collect all the stats
            var grade = $"{DiscordEmoji.FromName(ctx.Client, $":{recent.grade}_Rank:")}";
            var acc = $"{Math.Round((double) recent.accuracy, 2)}%";
            var pp = $"**{Math.Round((double) recent.performance_rating, 2)}**";
            var hits =
                $"[{recent.count_marv}/{recent.count_perf}/{recent.count_great}/{recent.count_good}/{recent.count_okay}/{recent.count_miss}]";
            var combo = $"**{recent.max_combo}x**/{map.count_hitobject_normal + map.count_hitobject_long * 2}x";
            var ratio = $"{Math.Round((double) recent.ratio, 2)}";
            var score = $"{recent.total_score}";
            var pb = recent.personal_best == true ? "Personal Best" : "";
            var mods = $"{recent.mods_string}" == "None" ? "" : $"{recent.mods_string}";
            var mapBannerUrl = $"https://cdn.quavergame.com/mapsets/{map.mapset_id}.jpg";
            var progress = Math.Round((float) (recent.count_marv +
                                               recent.count_perf +
                                               recent.count_great +
                                               recent.count_good +
                                               recent.count_okay +
                                               recent.count_miss) /
                (float) (map.count_hitobject_normal + map.count_hitobject_long * 2f) * 100, 2);

            // setup for the map banner
            var useBanner = true;
            MemoryStream banner = null;
            var hitdata = new List<long>();
            try
            {
                var data = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                    $"/scores/data/{recent.id}"));
                hitdata = JsonConvert.DeserializeObject<List<long>>($"{data.hits}".Replace("L", ""));
            }
            catch (Exception)
            {
                /* if this fails the banner will work but wont have hits on it*/
            }

            try
            {
                banner = RecentGraph.CreateGraphBanner(mapBannerUrl, hitdata,
                    (int) recent.count_miss > 0, progress);
            }
            catch (Exception)
            {
                // if this fails, no banner will be returned and it's just going to be the default map banner
                useBanner = false;
            }

            var eb = new DiscordEmbedBuilder()
                .WithAuthor(
                    $"{username}'s most recent play", $"https://quavergame.com/user/{qid}", $"{info.avatar_url}")
                .WithTitle($"{map.title} [{map.difficulty_name}] ({Math.Round((double) map.difficulty_rating, 2)})")
                .WithUrl($"https://quavergame.com/mapset/map/{recent.map.id}")
                .WithColor(Util.DiffToColor((double) map.difficulty_rating))
                .AddField("Grade", grade, true)
                .AddField("Accuracy", acc, true)
                .AddField("Performance Rating", pp, true)
                .AddField("Combo", combo, true)
                .AddField("Ratio", ratio, true)
                .AddField("Score", score, true)
                .AddField("Judgements", hits, true)
                .WithImageUrl(useBanner
                    ? "attachment://banner.png"
                    : mapBannerUrl)
                .WithFooter($"Played on {DateTime.Parse($"{recent.time}"):f}");

            // add optional fields
            if (!string.IsNullOrEmpty(mods))
                eb.AddField("Mods", mods, true);
            if (!string.IsNullOrEmpty(pb))
                eb.Footer.Text += $" | {pb}";
            if (progress < 100)
                eb.AddField("Map Completion", $"{progress}%", true);

            var reply = new DiscordMessageBuilder()
                .WithEmbed(eb.Build());

            if (useBanner && banner is not null)
                reply.WithFile("banner.png", banner);

            await ctx.RespondAsync(reply);
            _config.GetGuild(ctx.Guild.Id).UpdateChartInChannel(ctx.Channel.Id, (long) recent.map.id, false);
        }
    }
}