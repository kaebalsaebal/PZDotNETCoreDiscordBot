using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot.Interactions.SlashCommands
{
    //[RequireUserPermission(GuildPermission.Administrator)]
    public class ChannelSlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("set_public_channel", "Sets Public Channel")]
        public async Task SetPublicChannel([ChannelTypes(ChannelType.Text)] IChannel channel)
        {
            Application.BotConfig.PublicChannelId = channel.Id;
            await Application.BotConfig.Save();
            await RespondAsync($"Set Public Channel to {channel.Name}", ephemeral: true);

            await Application.CheckBotInitCondition();
        }

        [SlashCommand("set_command_channel", "Sets Command Channel")]
        public async Task SetCommandChannel([ChannelTypes(ChannelType.Text)] IChannel channel)
        {
            Application.BotConfig.CommandChannelId = channel.Id;
            await Application.BotConfig.Save();
            await RespondAsync($"Set Command Channel to {channel.Name}", ephemeral: true);

            await Application.CheckBotInitCondition();
        }

        [SlashCommand("set_log_channel", "Sets Log Channel")]
        public async Task SetLogChannel([ChannelTypes(ChannelType.Text)] IChannel channel)
        {
            Application.BotConfig.LogChannelId = channel.Id;
            await Application.BotConfig.Save();
            await RespondAsync($"Set Log Channel to {channel.Name}", ephemeral: true);

            await Application.CheckBotInitCondition();
        }
    }
}
