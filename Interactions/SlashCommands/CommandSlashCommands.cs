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

        [SlashCommand("ping", "Do you like watching me")]
        public async Task Ping()
        {
            await RespondAsync("☠PONG☠");
        }

        [SlashCommand("set_configs", "Configures other settings")]
        public async Task SetConfigs()
        {
            var current = Application.BotConfig;

            var modal = new BotSetupModal
            {
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
            if (uint.TryParse(modal.RestartTimer, out uint parsedTimer))
            {
                Application.BotConfig.ServerScheduleSettings.RestartTimer = parsedTimer;
            }
            else
            {
                await RespondAsync("⚠️RestartTimer value must be consisted of digits", ephemeral: true);
                return;
            }

            Application.BotConfig.RCONSettings.IP = modal.RCONIP;
            Application.BotConfig.RCONSettings.Password = modal.RCONPassword ?? "";

            if (ushort.TryParse(modal.RCONPort, out ushort parsedPort))
            {
                Application.BotConfig.RCONSettings.Port = parsedPort;
            }
            else
            {
                await RespondAsync("⚠️RCON Port value must be consisted of digits", ephemeral: true);
                return;
            }

            Application.BotConfig.ServerProcessSettings.WindowsServerFile = modal.WindowsServerFile ?? "server.bat";
            Application.BotConfig.ServerProcessSettings.LinuxServerFile = modal.LinuxServerFile ?? "server.sh";
            Application.BotConfig.ServerProcessSettings.UnixServerFile = modal.UnixServerFile ?? "server.sh";

            await Application.BotConfig.Save();
            await RespondAsync("💾Config has been Updated", ephemeral: true);
        }

        [SlashCommand("save", "Saves Server")]
        public async Task Save()
        {
            await ServerServiceManager.SaveServer(Context.Client, Application.BotConfig.LogChannelId);
        }

        [SlashCommand("restart_server", "Restarts server")]
        public async Task RestartServer([Summary("minutes", "Restarts server after minutes")] uint minutes)
        {
            if(minutes < 1 || minutes > 60)
            {
                await RespondAsync("Please enter 1~60");
                return;
            }

            await ServerServiceManager.RestartServer(Context.Client, Application.BotConfig.LogChannelId, minutes*60000);
        }

        [SlashCommand("shutdown_server", "Shuts down server immediately")]
        public async Task ShutdownServer()
        {
            await ServerServiceManager.ShutdownServer(Context.Client, Application.BotConfig.LogChannelId);
        }

        // For Debug
        [SlashCommand("checkmodupdate", "For debug")]
        public async Task CheckWorkshopItems()
        {

            await RespondAsync("🔍 Checking Mod Update Date...");

            string[] ids = Array.Empty<string>();

            string configFilePath = ServerServiceManager.GetServerIniPath();
            if (!File.Exists(configFilePath))
            {
                await RespondAsync($"❌ Failed to Get {configFilePath} File...");
            }
            string workshopString = ServerServiceManager.GetValueFromIni(configFilePath, "WorkshopItems");
            ids = workshopString.Split(';', StringSplitOptions.RemoveEmptyEntries);

            var modDetails = await SteamWebAPI.GetWorkshopItemDetailsAsync(ids);

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
