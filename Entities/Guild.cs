using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace QuaverBot.Entities
{
    public class Guild
    {
        public ulong Id { get; set; }
        public ulong QuaverChannel { get; set; }
        public bool NewRankedMapsUpdates { get; set; }

        [JsonIgnore] private ConcurrentDictionary<ulong, long> ChartLog = new();

        public long GetLatestMap(ulong channel)
            => ChartLog[channel];
        public void UpdateChartInChannel(ulong channel, long chartId)
            => ChartLog.AddOrUpdate(channel, chartId, (_, _) => chartId);
    }
}