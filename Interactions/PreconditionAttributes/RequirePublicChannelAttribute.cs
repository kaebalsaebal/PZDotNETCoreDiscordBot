using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class RequirePublicChannelAttribute : PreconditionAttribute
    {

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            var botConfig = services.GetRequiredService<BotConfig>();
            ulong pubChannelId = botConfig.PublicChannelId;

            if (pubChannelId == 0 || context.Channel.Id == pubChannelId)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError(Messages.Get("public_channel_only")));
            }
        }
    }
}
