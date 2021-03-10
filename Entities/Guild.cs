using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuaverBot.Entities
{
    public class Guild
    {
        public ulong Id { get; set; }
        public ulong QuaverChannel { get; set; }
        public bool NewRankedMapsUpdates { get; set; }
        public bool AutomaticMapInfo { get; set; }

        [JsonIgnore] private ConcurrentDictionary<ulong, KeyValuePair<long, bool>> ChartLog = new();

        public KeyValuePair<long, bool> GetLatestMap(ulong channel)
        {
            if (!ChartLog.ContainsKey(channel))
                throw new CommandException("No chart logged in this channel so far.");
            return ChartLog[channel];
        }

        public void UpdateChartInChannel(ulong channel, long chartId, bool isSet)
            => ChartLog.AddOrUpdate(channel, KeyValuePair.Create(chartId, isSet),
                (_, _) => KeyValuePair.Create(chartId, isSet));
    }
}