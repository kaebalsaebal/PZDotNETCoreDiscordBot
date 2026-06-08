using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    [RequireTargetGuild]
    public class BotCommands : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [RequireCommandChannel]
        public async Task PingAsync()
        {
            await ReplyAsync("Ping success");
        }

        [Command("restart_server")]
        [RequireCommandChannel]
        public async Task RestartServerAsync()
        {
            List<uint> restartTimers = Application.BotConfig.ServerScheduleSettings.GetRestartTimers();
            await ServerUtility.RestartServer(Context.Channel, restartTimers);
        }

        [Command("shutdown_server")]
        [RequireCommandChannel]
        public async Task ShutdownServerAsync()
        {
            await ServerUtility.ShutdownServer(Context.Channel);
        }

        // For Debug
        [Command("checkmodupdate")]
        [RequireCommandChannel]
        public async Task CheckWorkshopItemsAsync()
        {

            await ReplyAsync("🔍 Checking Mod Update Date...");

            string[] ids = Array.Empty<string>();

            string configFilePath = ServerUtility.GetServerIniPath();
            if (!File.Exists(configFilePath))
            {
                await ReplyAsync($"❌ Failed to Get {configFilePath} File...");
            }
            string workshopString = ServerUtility.GetValueFromIni(configFilePath, "WorkshopItems");
            ids = workshopString.Split(';', StringSplitOptions.RemoveEmptyEntries);

            var modDetails = await SteamWebAPI.GetWorkshopItemDetailsAsync(ids);

            if (modDetails == null || modDetails.Count == 0)
            {
                await ReplyAsync("❌ Failed to Fetch Mod Data...");
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

            await ReplyAsync(sb.ToString());
        }

        [Command("hello")]
        [RequirePublicChannel]
        public async Task HelloAsync()
        {
            await ReplyAsync($"Hello, {Context.User.Username}!");
        }

        [Command("players")]
        [RequirePublicChannel]
        public async Task CheckPlayersAsync()
        {
            string result = await RconManager.SendCommandAsync("players");

            await ReplyAsync($"```\n{result}\n```");
        }
    }
}
