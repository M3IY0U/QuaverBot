using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace QuaverBot.Entities
{
    public class Guild
    {
        public ulong Id { get; set; }
        public ulong QuaverChannel { get; set; }
        public bool NewRankedMapsUpdates { get; set; }

        [JsonIgnore] private ConcurrentDictionary<ulong, long> MapLog = new();

        public void UpdateChartInChannel(ulong channel, long chartId)
            => MapLog.AddOrUpdate(channel, chartId, (_, _) => chartId);
    }
}