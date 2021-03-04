using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Newtonsoft.Json;

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
                throw new Exception("User not found on Quaver.");
            }
        }

        public static DiscordColor DiffToColor(double diff)
            => diff switch
            {
                < 1 => new DiscordColor("#CBF7F3"),
                > 1 and <= 3.5 => new DiscordColor("#5CF972"),
                > 3.5 and <= 8 => new DiscordColor("#5BBEF7"),
                > 8 and <= 19 => new DiscordColor("#B54A45"),
                > 19 and <= 28 => new DiscordColor("#CFFDF8"),
                > 28 => new DiscordColor("#CFFDF8"),
                _ => DiscordColor.Black
            };
    }
}