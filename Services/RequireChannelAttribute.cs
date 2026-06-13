using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class RequireCommandChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            ulong cmdChannelId = Application.BotConfig.CommandChannelId;

            if (cmdChannelId == 0 || context.Channel.Id == cmdChannelId)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("This command is limited to command channel"));
            }
        }
    }
    public class RequirePublicChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            ulong pubChannelId = Application.BotConfig.PublicChannelId;

            if (pubChannelId == 0 || context.Channel.Id == pubChannelId)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("This command is limited to public channel"));
            }
        }
    }
}
