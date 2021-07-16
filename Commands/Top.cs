using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json;
using QuaverBot.Core;
using QuaverBot.Entities;


namespace QuaverBot.Commands
{
    public class Top : BaseCommandModule
    {
        private readonly Config _config;
        public Top(Config config) => _config = config;

        [Command("top"), Aliases("best", "t", "b")]
        public async Task TopCommand(CommandContext ctx, string username = "", string mode = "4k")
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
            var info = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                $"/users?id={qid}")).users[0];

            List<dynamic> best;
            try
            {
                var response = JsonConvert.DeserializeObject<dynamic>(await Util.ApiCall(_config.BaseUrl +
                    $"/users/scores/best?id={qid}&mode={(mode.Contains("4") ? "1" : "2")}"));
                best = JsonConvert.DeserializeObject<List<dynamic>>($"{response.scores}");
            }
            catch (Exception)
            {
                throw new CommandException("Error occured retrieving queried user/gamemode.");
            }

            var pages = GeneratePages(best, ctx,
                new DiscordEmbedBuilder()
                    .WithAuthor(
                        $"{username}'s top plays", $"https://quavergame.com/user/{qid}", $"{info.avatar_url}")
                    .WithColor(ctx.Member.Color));

            await ctx.Client.GetInteractivity().SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
        }

        private static IEnumerable<Page> GeneratePages(IReadOnlyList<dynamic> best, CommandContext ctx, DiscordEmbedBuilder template)
        {
            var result = new List<Page>();
            var scores = new List<string>();
            for (var i = 0; i < best.Count; i++)
                scores.Add(ScoreToEntry(best[i], i + 1, ctx));

            for (var index = 0; index < scores.Count / 5; index++)
            {
                var combined = scores.Skip(index * 5).Take(5);
                template.WithDescription(string.Join("\n\n", combined));
                result.Add(new Page(embed: template));
            }

            return result;
        }

        private static string ScoreToEntry(dynamic score, int place, CommandContext ctx)
            =>
                $"**#{place} [{score.map.title}](https://quavergame.com/mapset/{score.map.id})**" +
                $" [[{score.map.difficulty_name}](https://quavergame.com/mapset/map/{score.map.id})]" +
                ($"{score.mods_string}" == "None" ? "" : $" {score.mods_string}") +
                $"\n🔹 {DiscordEmoji.FromName(ctx.Client, $":{score.grade}_Rank:")} 🔹 " +
                $"**{Math.Round((double) score.performance_rating, 2)}QR** 🔹 {Math.Round((double) score.accuracy, 2)}%" +
                $"\n🔹 Achieved on {DateTime.Parse($"{score.time}"):f}";
    }
}