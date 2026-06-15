using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    [RequireCommandChannel]
    public class CommandSlashCommands : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly IServerServiceManager _serverService;
        private readonly ISteamWebAPI _steamApi;
        private readonly BotConfig _botConfig;

        public CommandSlashCommands(IServerServiceManager serverService, ISteamWebAPI steamApi, BotConfig botConfig)
        {
            _serverService = serverService;
            _steamApi = steamApi;
            _botConfig = botConfig;
        }

        [SlashCommand("ping", "Do you like watching me")]
        public async Task Ping()
        {
            await RespondAsync("☠PONG☠");
        }

        [SlashCommand("set_configs", "Configures other settings")]
        public async Task SetConfigs()
        {
            var current = _botConfig;

            var modal = new BotSetupModal
            {
                SaveAsFile = current.SaveAsFile.ToString().ToLower(),

                RestartTimer = current.ServerScheduleSettings.RestartTimer.ToString(),
                RCONIP = current.RCONSettings.IP,
                RCONPort = current.RCONSettings.Port.ToString(),
                RCONPassword = current.RCONSettings.Password,

                WindowsServerFile = current.ServerProcessSettings.WindowsServerFile,
                LinuxServerFile = current.ServerProcessSettings.LinuxServerFile,
                UnixServerFile = current.ServerProcessSettings.UnixServerFile,
            };

            await RespondWithModalAsync<BotSetupModal>("set_configs", modal);
        }
        [ModalInteraction("set_configs")]
        public async Task OnModalSubmit(BotSetupModal modal)
        {
            if (bool.TryParse(modal.SaveAsFile.Trim(), out bool parsedSaveAsFile))
            {
                _botConfig.SaveAsFile = parsedSaveAsFile;
            }
            else
            {
                await RespondAsync("⚠️SaveAsFile value must be 'true' or 'false'", ephemeral: true);
                return;
            }

            if (uint.TryParse(modal.RestartTimer, out uint parsedTimer))
            {
                _botConfig.ServerScheduleSettings.RestartTimer = parsedTimer;
            }
            else
            {
                await RespondAsync("⚠️RestartTimer value must be consisted of digits", ephemeral: true);
                return;
            }

            _botConfig.RCONSettings.IP = modal.RCONIP;
            _botConfig.RCONSettings.Password = modal.RCONPassword ?? "";

            if (ushort.TryParse(modal.RCONPort, out ushort parsedPort))
            {
                _botConfig.RCONSettings.Port = parsedPort;
            }
            else
            {
                await RespondAsync("⚠️RCON Port value must be consisted of digits", ephemeral: true);
                return;
            }

            _botConfig.ServerProcessSettings.WindowsServerFile = modal.WindowsServerFile ?? "server.bat";
            _botConfig.ServerProcessSettings.LinuxServerFile = modal.LinuxServerFile ?? "server.sh";
            _botConfig.ServerProcessSettings.UnixServerFile = modal.UnixServerFile ?? "server.sh";

            await _botConfig.Save();
            await RespondAsync("💾Config has been Updated", ephemeral: true);
        }

        [SlashCommand("save", "Saves Server")]
        public async Task Save()
        {
            await _serverService.SaveServer(Context.Client, _botConfig.LogChannelId);
        }

        [SlashCommand("restart_server", "Restarts server")]
        public async Task RestartServer([Summary("minutes", "Restarts server after minutes")] uint minutes)
        {
            if(minutes < 1 || minutes > 60)
            {
                await RespondAsync("Please enter 1~60");
                return;
            }

            await _serverService.RestartServer(Context.Client, _botConfig.LogChannelId, minutes*60000);
        }

        [SlashCommand("shutdown_server", "Shuts down server immediately")]
        public async Task ShutdownServer()
        {
            await _serverService.ShutdownServer(Context.Client, _botConfig.LogChannelId);
        }

        // For Debug
        [SlashCommand("checkmodupdate", "For debug")]
        public async Task CheckWorkshopItems()
        {

            await RespondAsync("🔍 Checking Mod Update Date...");

            string[] ids = Array.Empty<string>();

            string configFilePath = _serverService.GetServerIniPath();
            if (!File.Exists(configFilePath))
            {
                await RespondAsync($"❌ Failed to Get {configFilePath} File...");
            }
            string workshopString = _serverService.GetValueFromIni(configFilePath, "WorkshopItems");
            ids = workshopString.Split(';', StringSplitOptions.RemoveEmptyEntries);

            var modDetails = await _steamApi.GetWorkshopItemDetails(ids);

            if (modDetails == null || modDetails.Count == 0)
            {
                await RespondAsync("❌ Failed to Fetch Mod Data...");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"**[Mod Update Check Result - Total {modDetails.Count}]**");

            var sortedMods = modDetails.OrderByDescending(m => m.TimeUpdated).ToList();

            foreach (var mod in sortedMods.Take(10))
            {
                DateTime lastUpdate = DateTimeOffset.FromUnixTimeSeconds(mod.TimeUpdated).LocalDateTime;

                sb.AppendLine($"- **{mod.Title}** (ID: {mod.PublishedFileId})");
                sb.AppendLine($"  Last Update Date: {lastUpdate:yyyy-MM-dd HH:mm:ss}");
            }

            if (sortedMods.Count > 10)
            {
                sb.AppendLine($"...{sortedMods.Count - 10} Mods and so more...");
            }

            await RespondAsync(sb.ToString());
        }
    }
}
