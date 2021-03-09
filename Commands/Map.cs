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
    public class Map : BaseCommandModule
    {
        private readonly Config _config;
        public Map(Config config) => _config = config;

        [Command("map"), Aliases("chart")]
        public async Task MapGroupCommand(CommandContext ctx, [RemainingText] string search = "")
        {
            var pages = await SearchForMapset(search, ctx);
            // don't waste resources paginating a message that only has 1 page anyway
            if (pages.Count > 1)
            {
                var interactivity = ctx.Client.GetInteractivity();
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
            }
            else
            {
                await ctx.RespondAsync(pages.First().Embed);
            }
        }

        private async Task<List<Page>> SearchForMapset(string search, CommandContext ctx)
        {
            // setup 
            var response = JsonConvert
                .DeserializeObject<dynamic>(
                    await Util.ApiCall(_config.BaseUrl + $"/mapsets/maps/search?search={search}"));
            var mapSets = JsonConvert.DeserializeObject<List<dynamic>>($"{response.mapsets}");
            if (mapSets == null || !mapSets.Any())
            {
                throw new CommandException($"No mapsets found using search term: {search}");
            }

            var result = new List<Page>();
            var count = 1;
            mapSets = mapSets.Take(5).ToList();

            // add a page for each found set
            foreach (var mapset in mapSets)
            {
                var eb = new DiscordEmbedBuilder()
                    .WithAuthor($"Mapsets matching {search}");

                // get bpm
                var bpms = JsonConvert.DeserializeObject<List<double>>($"{mapset.bpms}");
                var bpmstring = bpms != null && bpms.Count > 1
                    ? $"{bpms.Min()} - {bpms.Max()} ({bpms.Average()})"
                    : $"{bpms!.First()}";

                // get maps in set
                var setResponse =
                    JsonConvert.DeserializeObject<dynamic>(
                        await Util.ApiCall(_config.BaseUrl + $"/mapsets/{mapset.id}"));
                var mapsInset = JsonConvert.DeserializeObject<List<dynamic>>($"{setResponse.mapset.maps}")
                    .OrderBy(x => (double) x.difficulty_rating);
                var desc = "__Difficulties__\n";
                // collect difficulties in set
                desc = mapsInset.Aggregate(desc,
                    (current, map) => current +
                                      $"» **[{map.difficulty_name}](https://api.quavergame.com/d/web/map/{map.id})**" +
                                      $" ({Math.Round((double) map.difficulty_rating, 2)})\n");

                // add info to embed
                eb.WithTitle($"{mapset.artist} - {mapset.title} charted by {mapset.creator_username}")
                    .WithDescription(desc)
                    .WithUrl($"https://quavergame.com/mapset/{mapset.id}")
                    .AddField("Length", $"{TimeSpan.FromSeconds((double) mapset.min_length_seconds):mm\\:ss}", true)
                    .AddField("BPM", bpmstring, true)
                    .AddField("Tags", $"{mapset.tags}")
                    .WithFooter(
                        $"Last updated on {DateTime.Parse($"{mapset.max_date_last_updated}"):f} | Map {count++}/{mapSets.Count}",
                        ctx.User.AvatarUrl)
                    .WithImageUrl($"https://cdn.quavergame.com/mapsets/{mapset.id}.jpg");

                result.Add(new Page(embed: eb));
            }

            return result.ToList();
        }
    }
}