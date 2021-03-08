using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QuaverBot.Entities;

namespace QuaverBot.Core
{
    public class Config
    {
        public List<string> Prefixes;
        public string Token;
        public string BaseUrl { get; } = "https://api.quavergame.com/v1";
        public List<User> Users;
        public List<Guild> Guilds;

        public Guild GetGuild(ulong id)
        {
            if (!Guilds.Exists(x => x.Id == id))
                Guilds.Add(new Guild {Id = id, QuaverChannel = 0, NewRankedMapsUpdates = false});
            return Guilds.Find(x => x.Id == id);
        }

        public void Save()
            => File.WriteAllText("config.json", JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}