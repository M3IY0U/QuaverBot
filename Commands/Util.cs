using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using QuaverBot.Entities;

namespace QuaverBot.Commands
{
    public class Util
    {
        public static Task<string> ApiCall(string url)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            var response = (HttpWebResponse) request.GetResponse();
            using var sr = new StreamReader(response.GetResponseStream());
            return Task.FromResult(sr.ReadToEnd());
        }

        public static async Task<string> NameToQid(string username)
        {
            var result = await ApiCall($"https://api.quavergame.com/v1/users/search/{username}");
            var dyn = JsonConvert.DeserializeObject<dynamic>(result);
            try
            {
                return (string) dyn.users[0].id;
            }
            catch (Exception)
            {
                throw new CommandException("User not found on Quaver.");
            }
        }

        public static DiscordColor DiffToColor(double diff)
            => diff switch
            {
                < 1 => new DiscordColor("#CBF7F3"), // beginner -> very light blue
                > 1 and <= 3.5 => new DiscordColor("#5CF972"), // easy -> light green
                > 3.5 and <= 8 => new DiscordColor("#5BBEF7"), // normal -> blue
                > 8 and <= 19 => new DiscordColor("#B54A45"), // hard -> orange
                > 19 and <= 28 => new DiscordColor("#CFFDF8"), // insane -> red
                > 28 => new DiscordColor("#9152DE"), // wtf -> purple
                _ => DiscordColor.Black
            };

        public static string ModeString(GameMode mode)
            => mode switch
            {
                GameMode.Key4 => "4K",
                GameMode.Key7 => "7K",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

        public static async Task<DiscordEmbed> GetMapSetInfo(long id)
        {
            var set =
                JsonConvert.DeserializeObject<dynamic>(
                    await ApiCall($"https://api.quavergame.com/v1/mapsets/{id}")).mapset;
            var maps = JsonConvert.DeserializeObject<List<dynamic>>($"{set.maps}")
                .OrderBy(x => (double) x.difficulty_rating).ToList();

            var ranked = maps.All(x => (int) x.ranked_status == 2) ? "🟩 Ranked" : "🟥 Unranked";

            string keys;
            var gameModes = maps.Select(x => (int) x.game_mode).ToList();
            if (gameModes.All(x => x == 1))
                keys = "4K";
            else if (gameModes.All(x => x == 2))
                keys = "7K";
            else
                keys = "4/7K";

            var desc =
                $"{ranked} ❙ {keys}\nLength: **{TimeSpan.FromMilliseconds((double) maps.First().length):mm\\:ss}** ❙ BPM: **{maps.First().bpm} ♪**";

            var diffs = maps.Aggregate("",
                (current, map) =>
                    current + $"» **[{map.difficulty_name}](https://api.quavergame.com/d/web/map/{map.id})** " +
                    $"({Math.Round((double) map.difficulty_rating, 2)}) ❙ " +
                    $"Combo: **{map.count_hitobject_normal + map.count_hitobject_long * 2}x** ❙ " +
                    $"QR for SS: **{Math.Round((double) map.difficulty_rating * Math.Pow(100d / 98, 6), 2)}**\n");

            var eb = new DiscordEmbedBuilder()
                .WithTitle($"{set.artist} - {set.title}")
                .WithUrl($"https://quavergame.com/mapset/{set.id}")
                .WithColor(DiffToColor((double) maps.First().difficulty_rating))
                .WithDescription(
                    $"Mapset by: [{set.creator_username}](https://quavergame.com/user/{set.creator_id})\n{desc}")
                .AddField("Difficulties", diffs)
                .WithImageUrl($"https://cdn.quavergame.com/mapsets/{set.id}.jpg")
                .WithFooter($"last updated on {DateTime.Parse($"{set.date_last_updated}"):f}");

            return eb.Build();
        }

        public static async Task<DiscordEmbed> GetMapInfo(long id)
        {
            var map = JsonConvert.DeserializeObject<dynamic>(await ApiCall($"https://api.quavergame.com/v1/maps/{id}"))
                .map;
            var difficulty = (double) map.difficulty_rating;
            var eb = new DiscordEmbedBuilder()
                .WithTitle($"{map.artist} - {map.title} [{map.difficulty_name}]")
                .WithColor(DiffToColor((double) map.difficulty_rating))
                .WithUrl($"https://quavergame.com/mapset/map/{map.id}")
                .WithDescription(
                    $"Mapped by: [{map.creator_username}](https://quavergame.com/user/{map.creator_id})\n" +
                    $"Difficulty: **{Math.Round(difficulty, 2)}** » BPM: **{map.bpm}♪**\n" +
                    $"Max Combo: {map.count_hitobject_normal + map.count_hitobject_long * 2}x » Length: **{TimeSpan.FromMilliseconds((double) map.length):mm\\:ss}**\n" +
                    $"PlayCount: {map.play_count} » Success%: {Math.Round(100 - (int) map.fail_count * 100f / (int) map.play_count, 2)}%\n" +
                    $"[Download](https://api.quavergame.com/d/web/map/{map.id})")
                .AddField("Potential Rating",
                    $"100%: {Math.Round(difficulty * Math.Pow(100d / 98, 6), 2)}QR » " +
                    $"95%: {Math.Round(difficulty * Math.Pow(95d / 98, 6), 2)}QR » " +
                    $"90%: {Math.Round(difficulty * Math.Pow(90d / 98, 6), 2)}QR", true)
                .WithImageUrl($"https://cdn.quavergame.com/mapsets/{map.mapset_id}.jpg")
                .WithFooter($"Map last updated on {DateTime.Parse($"{map.date_last_updated}"):f}");


            return eb.Build();
        }
    }
}