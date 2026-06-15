using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    //[RequireUserPermission(GuildPermission.Administrator)]
    public class ChannelSlashCommands : InteractionModuleBase<SocketInteractionContext>
    {

		private readonly BotConfig _botConfig;

		public ChannelSlashCommands(BotConfig botConfig)
		{
			_botConfig = botConfig;
		}

		[SlashCommand("set_public_channel", "Sets Public Channel")]
        public async Task SetPublicChannel([ChannelTypes(ChannelType.Text)] IChannel channel)
        {
            _botConfig.PublicChannelId = channel.Id;
            await _botConfig.Save();
            await RespondAsync($"Set Public Channel to {channel.Name}", ephemeral: true);

            await Application.CheckBotInitCondition(_botConfig);
        }

        [SlashCommand("set_command_channel", "Sets Command Channel")]
        public async Task SetCommandChannel([ChannelTypes(ChannelType.Text)] IChannel channel)
        {
            _botConfig.CommandChannelId = channel.Id;
            await _botConfig.Save();
            await RespondAsync($"Set Command Channel to {channel.Name}", ephemeral: true);

            await Application.CheckBotInitCondition(_botConfig);
        }

        [SlashCommand("set_log_channel", "Sets Log Channel")]
        public async Task SetLogChannel([ChannelTypes(ChannelType.Text)] IChannel channel)
        {
            _botConfig.LogChannelId = channel.Id;
            await _botConfig.Save();
            await RespondAsync($"Set Log Channel to {channel.Name}", ephemeral: true);

            await Application.CheckBotInitCondition(_botConfig);
        }
    }
}
