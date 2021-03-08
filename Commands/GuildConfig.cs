using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using QuaverBot.Core;
using QuaverBot.Entities;

namespace QuaverBot.Commands
{
    public class GuildConfig : BaseCommandModule
    {
        private Config _config;
        public GuildConfig(Config config) => _config = config;

        [Command("channel")]
        public async Task SetQuaverChannel(CommandContext ctx, string channel = "")
        {
            var guild = _config.GetGuild(ctx.Guild.Id);
            if (string.IsNullOrEmpty(channel))
            {
                await ctx.RespondAsync(guild?.QuaverChannel == 0
                    ? "No Quaver channel set."
                    : $"Current Quaver channel: <#{guild?.QuaverChannel}>");
            }
            else
            {
                if (ctx.Message.MentionedChannels.Count > 0)
                {
                    var chn = ctx.Message.MentionedChannels[0];
                    guild.QuaverChannel = chn.Id;
                    await ctx.RespondAsync($"Quaver channel was set to: {chn.Mention}");
                }
                else
                {
                    guild.QuaverChannel = 0;
                    await ctx.RespondAsync("No channel mentioned, disabled Quaver channel.");
                }

                _config.Save();
            }
        }

        [Command("rankedupdates"), Aliases("ru")]
        public async Task EnableRankedUpdates(CommandContext ctx, string setting = "")
        {
            var guild = _config.GetGuild(ctx.Guild.Id);
            if (string.IsNullOrEmpty(setting))
            {
                await ctx.RespondAsync(
                    $"Updates for new ranked maps is currently set to: {guild.NewRankedMapsUpdates}");
                return;
            }

            try
            {
                guild.NewRankedMapsUpdates = bool.Parse(setting);
            }
            catch (Exception e)
            {
                throw new CommandException(e.Message);
            }

            await ctx.RespondAsync($"Set ranked map updates to: {guild.NewRankedMapsUpdates}");
            _config.Save();
        }
    }
}