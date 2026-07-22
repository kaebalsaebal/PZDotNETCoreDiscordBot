using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    //[RequirePublicChannel]
    public class PublicSlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IServerServiceManager _serverService;
        private readonly IRconManager _rconManager;
        private readonly IWebAPIManager _webApi;
        private readonly BotConfig _botConfig;
        private readonly InteractionService _interactionService;
        private readonly Tools _tools;

        public PublicSlashCommands(IServerServiceManager serverService, IRconManager rconManager, IWebAPIManager webApi, BotConfig botConfig, InteractionService interactionService)
        {
            _serverService = serverService;
            _rconManager = rconManager;
            _webApi = webApi;
            _botConfig = botConfig;
            _interactionService = interactionService;
            _tools = new Tools();
        }

        [SlashCommand("players", "Gets players joined")]
        public async Task CheckPlayers()
        {
            string result = await _rconManager.SendCommandAsync("players");

            await RespondAsync($"```\n{result}\n```");
        }

        [SlashCommand("check_workshop_mods", "Get workshop mods in your server")]
        public async Task CheckWorkshopItems([Summary("items", "Show mods top n descending by update")] int items = -1)
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

            if (items == -1) items = modDetails.Count;

            var sb = new StringBuilder();
            sb.AppendLine($"**{Messages.Get("slash_workshop_title").KeyFormat(("servername", _botConfig.ServerName), ("count", $"{modDetails.Count}"))}**");

            var sortedMods = modDetails.OrderByDescending(m => m.TimeUpdated).ToList();

            foreach (var mod in sortedMods.Take((int)items))
            {
                DateTime lastUpdate = DateTimeOffset.FromUnixTimeSeconds(mod.TimeUpdated).LocalDateTime;

                sb.AppendLine($"- **{mod.Title}** (ID: `{mod.PublishedFileId}`)\nLast Updated Date: `{lastUpdate:yyyy-MM-dd HH:mm:ss}`");
            }

            if (sortedMods.Count > items)
            {
                sb.AppendLine($"...**{sortedMods.Count - items}** Mods and so more...");
            }

            List<StringBuilder> sbList = _tools.SplitSB(sb);

            foreach(StringBuilder sbItem in sbList)
            {
                await FollowupAsync(sbItem.ToString(), ephemeral: true);
            }
        }

        [SlashCommand("get_cpu_ram", "Get server's cpu and ram usage")]
        public async Task GetUsage()
        {
            await DeferAsync();

            double[] result = await _serverService.GetUsage();

            var sb = new StringBuilder();
            sb.AppendLine($"**{Messages.Get("slash_get_usage_title")}**");
            sb.AppendLine("```");
            sb.AppendLine($"CPU: {String.Format("{0:N2}", result[0])}%");
            sb.AppendLine($"RAM: {String.Format("{0:N2}", result[1])}%");
            sb.AppendLine("```");

            await FollowupAsync(sb.ToString(), ephemeral: true);
        }

        [SlashCommand("help", "Commands and description")]
        public async Task Help()
        {
            await DeferAsync();

            var sb = new StringBuilder();
            var commandSB = new StringBuilder();
            var publicSB = new StringBuilder();

            foreach (var module in _interactionService.Modules)
            {
                foreach(var cmd in module.SlashCommands)
                {
                    string commandLine = $"• `/{cmd.Name}` - {Messages.Get($"{cmd.Name}_desc")}\n";

                    if (module.Name.Contains("Public"))
                    {
                        publicSB.Append(commandLine);
                    }
                    else if(module.Name.Contains("Command") || module.Name.Contains("Channel"))
                    {
                        commandSB.Append(commandLine);
                    }
                }
            }

            sb.AppendLine($"**{Messages.Get("help_title")}**\n");
            if (publicSB.Length > 0)
            {
                sb.AppendLine($"**{Messages.Get("help_public_commands")}**");
                sb.Append(publicSB.ToString());
            }
            if (commandSB.Length > 0)
            {
                sb.AppendLine($"\n**{Messages.Get("help_authed_commands")}**");
                sb.Append(commandSB.ToString());
            }

            List<StringBuilder> sbList = _tools.SplitSB(sb);

            foreach (StringBuilder sbItem in sbList)
            {
                await FollowupAsync(sbItem.ToString(), ephemeral: true);
            }
        }
    }
}
