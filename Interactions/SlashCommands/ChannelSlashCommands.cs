using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    [RequireAuthorizedUser]
    public class ChannelSlashCommands : InteractionModuleBase<SocketInteractionContext>
    {

		private readonly BotConfig _botConfig;
        private readonly ILogFile _logFile;
        private readonly IServiceProvider _service;

        public ChannelSlashCommands(BotConfig botConfig, ILogFile logFile, IServiceProvider service)
        {
            _botConfig = botConfig;
            _logFile = logFile;
            _service = service;
        }

        [SlashCommand("set_public_channel", "Sets Public Channel")]
        public async Task SetPublicChannel([ChannelTypes(ChannelType.Text)] IChannel channel)
        {
            _botConfig.PublicChannelId = channel.Id;
            await _botConfig.Save(_logFile);
            await RespondAsync(Messages.Get("slash_public_channel").KeyFormat(("channel", $"**{channel.Name}**")), ephemeral: true);

            await Application.CheckBotInitCondition(_botConfig, _service, Context.Client, _logFile);
        }

        [SlashCommand("set_command_channel", "Sets Command Channel")]
        public async Task SetCommandChannel([ChannelTypes(ChannelType.Text)] IChannel channel)
        {
            _botConfig.CommandChannelId = channel.Id;
            await _botConfig.Save(_logFile);
            await RespondAsync(Messages.Get("slash_command_channel").KeyFormat(("channel", $"**{channel.Name}**")), ephemeral: true);

            await Application.CheckBotInitCondition(_botConfig, _service, Context.Client, _logFile);
        }

        [SlashCommand("set_log_channel", "Sets Log Channel")]
        public async Task SetLogChannel([ChannelTypes(ChannelType.Text)] IChannel channel)
        {
            _botConfig.LogChannelId = channel.Id;
            await _botConfig.Save(_logFile);
            await RespondAsync(Messages.Get("slash_log_channel").KeyFormat(("channel", $"**{channel.Name}**")), ephemeral: true);

            await Application.CheckBotInitCondition(_botConfig, _service, Context.Client, _logFile);
        }
    }
}
