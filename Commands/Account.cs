using QuaverBot.Entities;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using QuaverBot.Core;

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
            // if no username was provided, 2 options: 1. get current name 2. usage error
            if (string.IsNullOrEmpty(username))
            {
                if (user is not null)
                    await ctx.RespondAsync($"Current username: `{user.Name}`.");
                else
                    throw new CommandException("Missing username.");
                return;
            }
            
            // -> username was provided
            if (user is null)
            {
                // -> new user
                // check if user exists on quaver
                var qid = await Util.NameToQid(username);
                if (string.IsNullOrEmpty(qid))
                    throw new CommandException($"No user with name `{username}` was found on Quaver.");
                
                // add user to config
                _config.Users.Add(new User {Id = ctx.User.Id, Name = username, QuaverId = qid});
                await ctx.RespondAsync($"Username set to: `{username}` ({qid}).");
            }
            else
            {
                // -> update user
                var qid = await Util.NameToQid(username);
                if (string.IsNullOrEmpty(qid))
                    throw new CommandException($"No user with name `{username}` was found on Quaver.");
 
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
            if (user is null)
                throw new CommandException("Entry not present, can't remove.");

            _config.Users.Remove(user);
            await ctx.RespondAsync("Entry removed.");
            _config.Save();
        }

        [Command("mode")]
        public async Task Mode(CommandContext ctx, string choice = "")
        {
            var user = _config.Users.Find(x => x.Id == ctx.User.Id);
            if (user is null)
                throw new CommandException("No Username set.");
            switch (choice)
            {
                case "7":
                case "7k":
                    user.PreferredMode = GameMode.Key7;
                    break;
                case "4":
                case "4k":
                    user.PreferredMode = GameMode.Key4;
                    break;
                default:
                    await ctx.RespondAsync($"Current preferred GameMode: {Util.ModeString(user.PreferredMode)}");
                    return;
            }

            await ctx.RespondAsync($"Updated preferred GameMode to {Util.ModeString(user.PreferredMode)}.");
            _config.Save();
        }
    }
}