using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QuaverBot.Entities;

namespace QuaverBot
{
    public class Config
    {
        public string Token { get; set; }
        public string BaseUrl { get; } = "https://api.quavergame.com/v1";
        public List<User> Users { get; set; }

        public void Save()
            => File.WriteAllText("config.json", JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}