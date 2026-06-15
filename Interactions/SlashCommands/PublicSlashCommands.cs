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

        private readonly IRconManager _rconManager;

        public PublicSlashCommands(IRconManager rconManager)
        {
            _rconManager = rconManager;
        }

        [SlashCommand("players", "Gets players joined")]
        public async Task CheckPlayers()
        {
            string result = await _rconManager.SendCommandAsync("players");

            await RespondAsync($"```\n{result}\n```");
        }
    }
}
