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
        private readonly IWebAPIManager _webApi;
        private readonly BotConfig _botConfig;

        public PublicSlashCommands(IRconManager rconManager, IWebAPIManager webApi, BotConfig botConfig)
        {
            _rconManager = rconManager;
            _webApi = webApi;
            _botConfig = botConfig;
        }

        [SlashCommand("players", "Gets players joined")]
        public async Task CheckPlayers()
        {
            string result = await _rconManager.SendCommandAsync("players");

            await RespondAsync($"```\n{result}\n```");
        }

        [SlashCommand("check_workshop_mods", "Workshop mods in your server")]
        public async Task CheckWorkshopItems([Summary("items", "Show mods top n descending by update")] uint items = 0)
        {
            await DeferAsync();

            string[] ids = Array.Empty<string>();

            var tools = new Tools();

            string configFilePath = tools.GetServerIniPath(_botConfig.ServerName);
            if (!File.Exists(configFilePath))
            {
                await FollowupAsync(Messages.Get("slash_workshop_file_failed").KeyFormat(("config", $"'''{configFilePath}'''")), ephemeral: true);
                return;
            }
            string workshopString = tools.GetValueFromIni(configFilePath, "WorkshopItems");
            ids = workshopString.Split(';', StringSplitOptions.RemoveEmptyEntries);

            var modDetails = await _webApi.GetWorkshopItemDetails(ids);

            if (modDetails == null)
            {
                await FollowupAsync(Messages.Get("slash_workshop_api_failed"), ephemeral: true);
                return;
            }
            else if (modDetails.Count == 0)
            {
                await FollowupAsync(Messages.Get("slash_workshop_no_mods"), ephemeral: true);
                return;
            }

            if (items == 0) items = (uint)modDetails.Count;

            var sb = new StringBuilder();
            sb.AppendLine(Messages.Get("slash_workshop_title").KeyFormat(("servername", _botConfig.ServerName), ("count", $"**{modDetails.Count}**")));

            var sortedMods = modDetails.OrderByDescending(m => m.TimeUpdated).ToList();

            sb.AppendLine("```");
            foreach (var mod in sortedMods.Take((int)items))
            {
                DateTime lastUpdate = DateTimeOffset.FromUnixTimeSeconds(mod.TimeUpdated).LocalDateTime;

                sb.AppendLine($"- {mod.Title} (ID: {mod.PublishedFileId})");
                sb.AppendLine($"  Last Updated Date: {lastUpdate:yyyy-MM-dd HH:mm:ss}");
            }
            sb.AppendLine("```");

            if (sortedMods.Count > items)
            {
                sb.AppendLine($"...**{sortedMods.Count - items}** Mods and so more...");
            }

            await FollowupAsync(sb.ToString(), ephemeral: true);
        }
    }
}
