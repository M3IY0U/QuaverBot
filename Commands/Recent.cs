using System;
using System.IO;
using Newtonsoft.Json;
using QuaverBot.Graphics;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using System.Collections.Generic;
using DSharpPlus.CommandsNext.Attributes;

namespace QuaverBot.Commands
{
    public class Recent : BaseCommandModule
    {
        private Config _config;
        public Recent(Config config) => _config = config;

        [Command("recent"), Aliases("r", "rs")]
        public async Task GetRecent(CommandContext ctx, string username = "", string gamemode = "4")
        {
            string qid;
            if (string.IsNullOrEmpty(username))
            {
                var user = _config.Users.Find(x => x.Id == ctx.User.Id);
                if (user == null)
                    throw new Exception("No Username set.");
                username = user.Name;
                qid = user.QuaverId;
            }
            else
                qid = await Util.NameToQid(username);

            var recent = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/users/scores/recent?id={qid}&mode={(gamemode == "4" ? "1" : "2")}")).scores[0];

            var map = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/maps/{recent.map.id}")).map;

            var info = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/users?id={qid}")).users[0];

            var useBanner = true;
            MemoryStream banner = null;
            try
            {
                var data = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                    $"/scores/data/{recent.id}"));
                var hitdata = JsonConvert.DeserializeObject<List<long>>($"{data.hits}".Replace("L", ""));
                banner = RecentGraph.CreateGraphBanner($"https://cdn.quavergame.com/mapsets/{map.mapset_id}.jpg",
                    hitdata);
            }
            catch (Exception)
            {
                useBanner = false;
            }

            var grade = $"{DiscordEmoji.FromName(ctx.Client, $":{recent.grade}_Rank:")}";
            var acc = $"{Math.Round((double) recent.accuracy, 2)}%";
            var pp = $"**{Math.Round((double) recent.performance_rating, 2)}**";
            var hits =
                $"[{recent.count_marv}/{recent.count_perf}/{recent.count_great}/{recent.count_good}/{recent.count_okay}/{recent.count_miss}]";
            var combo = $"**{recent.max_combo}x**/{map.count_hitobject_normal + map.count_hitobject_long * 2}x";
            var ratio = $"{Math.Round((double) recent.ratio, 2)}";
            var score = $"{recent.total_score}";
            var pb = recent.personal_best == true ? "Personal Best" : "";
            var mods = recent.mods_string == "None" ? "" : recent.mods_string as string;

            var eb = new DiscordEmbedBuilder()
                .WithAuthor(
                    $"{map.title} [{map.difficulty_name}] ({Math.Round((double) map.difficulty_rating, 2)})",
                    $"https://quavergame.com/mapset/map/{recent.map.id}", $"{info.avatar_url}")
                .AddField("Grade", grade, true)
                .AddField("Accuracy", acc, true)
                .AddField("Performance Rating", pp, true)
                .AddField("Hits", hits, true)
                .AddField("Combo", combo, true)
                .AddField("Ratio", ratio, true)
                .AddField("Score", score, true)
                .WithImageUrl(useBanner
                    ? "attachment://banner.png"
                    : $"https://cdn.quavergame.com/mapsets/{map.mapset_id}.jpg")
                .WithColor(Util.DiffToColor((double) map.difficulty_rating))
                .WithFooter($"Played on {DateTime.Parse($"{recent.time}"):f}");

            if (!string.IsNullOrEmpty(mods))
                eb.AddField("Mods", mods, true);
            if (!string.IsNullOrEmpty(pb))
                eb.Footer.Text += $" | {pb}";
            
            var reply = new DiscordMessageBuilder()
                .WithContent($"**Most recent Quaver play for {username}:**")
                .WithEmbed(eb.Build());
            
            if (useBanner && banner is not null)
                reply.WithFile("banner.png", banner);
            
            await ctx.RespondAsync(reply);
        }
    }
}