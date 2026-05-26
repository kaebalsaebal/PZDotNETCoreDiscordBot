using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class RequireCommandChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
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
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
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

    public class RequireLogChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            ulong logChannelId = Application.BotConfig.LogChannelId;

            if (logChannelId == 0 || context.Channel.Id == logChannelId)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("This command is limited to log channel"));
            }
        }
    }

    public class RequireTargetGuildAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Guild != null && context.Guild.Id == Application.BotConfig.GuildId)
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError("This bot only works in designated server"));
        }
    }
}
