using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QuaverBot.Entities;

namespace QuaverBot.Core
{
    public class Config
    {
        public List<string> Prefixes { get; set; } = new() {"q", "q!"};
        public string Token;
        public string BaseUrl { get; } = "https://api.quavergame.com/v1";
        public List<User> Users;
        public List<Guild> Guilds;

        public void Save()
            => File.WriteAllText("config.json", JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}