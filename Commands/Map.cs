using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json;
using QuaverBot.Core;
using QuaverBot.Entities;
using QuaverBot.Graphics;

namespace QuaverBot.Commands
{
    public class Map : BaseCommandModule
    {
        private readonly Config _config;
        public Map(Config config) => _config = config;

        [Command("mapsearch"), Aliases("ms", "cs")]
        public async Task MapSearch(CommandContext ctx, [RemainingText] string search = "")
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

        [Command("mappreview"), Aliases("mp")]
        public async Task MapPreviewCommand(CommandContext ctx, string id, double percent, int length = 10000)
        {
            new WebClient().DownloadFile($"https://api.quavergame.com/d/web/map/{id}", $"{id}.qua");
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("♨"));
            await MapPreview.RenderMap($"{id}.qua", Convert.ToInt32(id), percent, length);
            await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("♨"));
            await ctx.RespondAsync(new DiscordMessageBuilder().WithFile($"{id}.mp4"));
            await Task.Delay(1000);
            try
            {
                GC.Collect();
                File.Delete($"{id}.qua");
                File.Delete($"{id}.mp4");
            }
            catch { /* ignored */}
        }
        
        [Command("map"), Aliases("m", "c")]
        public async Task MapInfo(CommandContext ctx, string input = "")
        {
            // setup
            var guild = _config.GetGuild(ctx.Guild.Id);
            long id;
            bool isSet;
            // whether an url was provided or the last map in the channel was meant
            if (string.IsNullOrEmpty(input))
            {
                (id, isSet) = guild.GetLatestMap(ctx.Channel.Id);
            }
            else
            {
                // get map(set) id from url
                string match;
                if (Bot.MapRegex.IsMatch(input))
                {
                    match = Bot.MapRegex.Match(input).Value;
                    isSet = false;
                }
                else if(Bot.MapSetRegex.IsMatch(input))
                {
                    match = Bot.MapSetRegex.Match(input).Value;
                    isSet = true;
                }
                else
                    throw new CommandException("Map link was not recognized");
                id = Convert.ToInt64(match.Substring(match.LastIndexOf('/') + 1));
            }
            // gather info & send
            var info = isSet ? await Util.GetMapSetInfo(id) : await Util.GetMapInfo(id);
            await ctx.RespondAsync(info);
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
                var bpmstring = bpms is {Count: > 1}
                    ? $"{bpms.Min()}♪ - {bpms.Max()}♪ ({bpms.Average()})♪"
                    : $"{bpms!.First()}♪";

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
                                      $"🔹 **[{map.difficulty_name}](https://api.quavergame.com/d/web/map/{map.id})**" +
                                      $" ({Math.Round((double) map.difficulty_rating, 2)})\n");

                // add info to embed
                eb.WithTitle($"{mapset.artist} - {mapset.title} mapped by {mapset.creator_username}")
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