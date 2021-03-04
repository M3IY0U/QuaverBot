using QuaverBot.Entities;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace QuaverBot.Commands
{
    public class Account : BaseCommandModule
    {
        private Config _config;
        public Account(Config config) => _config = config;

        [Command("set")]
        public async Task Set(CommandContext ctx, string username = "")
        {
            var user = _config.Users.Find(x => x.Id == ctx.User.Id);
            if (string.IsNullOrEmpty(username))
            {
                if (user != null)
                    await ctx.RespondAsync($"Current username: `{user.Name}`.");
                else
                    await ctx.RespondAsync("Error: Missing username.");
                return;
            }

            if (user == null)
            {
                var qid = await Util.NameToQid(username);
                if (string.IsNullOrEmpty(qid))
                {
                    await ctx.RespondAsync($"Error: No user with name `{username}` was found on Quaver.");
                    return;
                }

                _config.Users.Add(new User {Id = ctx.User.Id, Name = username, QuaverId = qid});
                await ctx.RespondAsync($"Username set to: `{username}` ({qid}).");
            }
            else
            {
                var qid = await Util.NameToQid(username);
                if (string.IsNullOrEmpty(qid))
                {
                    await ctx.RespondAsync($"Error: No user with name `{username}` was found on Quaver.");
                    return;
                }

                user.Name = username;
                user.QuaverId = qid;
                await ctx.RespondAsync($"Username updated to: `{username}` ({qid}).");
            }

            _config.Save();
        }

        [Command("unset")]
        public async Task UnSet(CommandContext ctx)
        {
            var user = _config.Users.Find(x => x.Id == ctx.User.Id);
            if (user == null)
            {
                await ctx.RespondAsync("Error: Entry not present, can't remove.");
            }
            else
            {
                _config.Users.Remove(user);
                await ctx.RespondAsync("Entry removed.");
            }

            _config.Save();
        }
    }
}