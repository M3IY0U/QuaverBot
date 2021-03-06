using System;
using System.IO;
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
                > 28 => new DiscordColor("#CFFDF8"), // wtf -> purple
                _ => DiscordColor.Black
            };

        public static string ModeString(GameMode mode)
            => mode switch
            {
                GameMode.Key4 => "4K",
                GameMode.Key7 => "7K",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
    }
}