using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using QuaverBot.Core;
using QuaverBot.Entities;
using QuaverBot.Graphics;

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
            // if user was null, set it to the command user
            user ??= ctx.User;
            // get user and mode preference, then execute main command function
            var qUser = _config.Users.Find(x => x.Id == user.Id);
            if (qUser is not null)
            {
                if (mode == "4k")
                    mode = qUser.PreferredMode == GameMode.Key4 ? "4" : "7";
                await GetProfile(ctx, qUser.Name, mode);
            }
            else
                throw new CommandException("User has not set their account. Use qset [name] to set it.");
        }

        [Command("profile"), Aliases("p"), Priority(2)]
        public async Task GetProfile(CommandContext ctx, string username = "", string mode = "4k")
        {
            // get quaver id
            string qid;
            var gm = GameMode.Key4;
            if (string.IsNullOrEmpty(username))
            {
                var user = _config.Users.Find(x => x.Id == ctx.User.Id);
                if (user == null)
                    throw new CommandException("User has not set their account. Use qset [name] to set it.");
                gm = user.PreferredMode;
                qid = user.QuaverId;
            }
            else
                qid = await Util.NameToQid(username);

            if (mode.Contains("7"))
                gm = GameMode.Key7;

            // get the two needed responses
            var fullInfo = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/users/full/{qid}")).user;
            var graphData = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/users/graph/rank?id={qid}&mode=" + (gm == GameMode.Key4 ? "1" : "2"))).statistics;

            // create a representation of the users rank in the past 10 days
            var graph = JsonConvert.DeserializeObject<List<RankAtTime>>($"{graphData}");
            MemoryStream img = null;
            var useImg = true;
            try
            {
                img = RankGraph.CreateGraphBanner(graph,
                    gm == GameMode.Key4 ? (long) fullInfo.keys4.globalRank : (long) fullInfo.keys7.globalRank);
            }
            catch (Exception)
            {
                useImg = false;
            }

            // create embed & send it
            var info = fullInfo.info;
            var eb = new DiscordEmbedBuilder()
                .WithAuthor($"{info.username}'s Profile")
                .WithDescription(
                    $"[Quaver](https://quavergame.com/user/{qid}) | [Steam](https://steamcommunity.com/profile/{info.steam_id})")
                .WithColor(ctx.Member.Color)
                .WithThumbnail($"{info.avatar_url}")
                .WithFooter("Currently " + (bool.Parse($"{info.online}") ? "online" : "offline"));
            AddModeStats(ref eb, gm, fullInfo);

            var reply = new DiscordMessageBuilder();

            if (useImg && img is not null)
            {
                reply.WithFile("graph.png", img);
                eb.WithImageUrl("attachment://graph.png");
            }

            await ctx.RespondAsync(reply.WithEmbed(eb.Build()));
        }

        private static void AddModeStats(ref DiscordEmbedBuilder eb, GameMode gm, dynamic info)
        {
            // add the actually relevant information to the embed
            var keys = info.keys7;
            if (gm == GameMode.Key4)
                keys = info.keys4;

            eb.AddField(gm == GameMode.Key4 ? "4K" : "7K",
                $"Rank: **#{keys.globalRank}** ({info.info.country} **#{keys.countryRank}**)\n" +
                $"Performance Rating: **{Math.Round((double) keys.stats.overall_performance_rating, 2)}**\n" +
                $"Accuracy: **{Math.Round((double) keys.stats.overall_accuracy, 2)}%**\n" +
                $"PlayCount: **{keys.stats.play_count}** (Fails: {keys.stats.fail_count}) 🔹 Success%: {Math.Round(100 - (int) keys.stats.fail_count * 100f / (int) keys.stats.play_count, 2)}%\n" +
                $"Judgements:\n**⚪ {keys.stats.total_marv} 🟡 {keys.stats.total_perf} 🟢 {keys.stats.total_great}\n" +
                $"🔵 {keys.stats.total_good} 🟣 {keys.stats.total_okay} 🔴 {keys.stats.total_miss}**\n");
        }
    }
}