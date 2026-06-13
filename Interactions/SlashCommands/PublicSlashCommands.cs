using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    [RequirePublicChannel]
    public class PublicSlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("players", "Gets players joined")]
        [RequirePublicChannel]
        public async Task CheckPlayers()
        {
            string result = await RconManager.SendCommandAsync("players");

            await RespondAsync($"```\n{result}\n```");
        }
    }
}
