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
            await RespondAsync("☠PONG☠", ephemeral: true);
        }

        [SlashCommand("set_configs", "Configures other settings")]
        public async Task SetConfigs()
        {
            try
            {
                var current = _botConfig;

                var modal = new BotSetupModal
                {
                    SaveAsFile = current.SaveAsFile.ToString().ToLower(),
                    RestartTimer = current.ServerScheduleSettings.RestartTimer.ToString(),
                    WorkshopInterval = current.ServerScheduleSettings.WorkshopItemUpdateSchedule.ToString(),
                };

                await RespondWithModalAsync<BotSetupModal>("set_configs", modal);
            } catch(Exception e)
            {
                await RespondAsync($"COMMAND ERROR: {e.Message}", ephemeral: true);
            }
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
                await RespondAsync("SaveAsFile value must be 'true' or 'false'", ephemeral: true);
                return;
            }

            if (uint.TryParse(modal.RestartTimer, out uint parsedTimer))
            {
                _botConfig.ServerScheduleSettings.RestartTimer = parsedTimer;
            }
            else
            {
                await RespondAsync("RestartTimer value must be consisted of digits", ephemeral: true);
                return;
            }

            if (uint.TryParse(modal.WorkshopInterval, out uint parsedInterval))
            {
                _botConfig.ServerScheduleSettings.WorkshopItemUpdateSchedule = parsedInterval;
            }
            else
            {
                await RespondAsync("WorkshopInterval value must be consisted of digits", ephemeral: true);
                return;
            }

            await _botConfig.Save();
            await RespondAsync("Config has been Updated", ephemeral: true);
        }

        [SlashCommand("save_server", "Saves Server")]
        public async Task Save()
        {
            try
            {
                await DeferAsync();

                await _serverService.SaveServer(Context.Client, _botConfig.LogChannelId);

                await FollowupAsync("Save completed...", ephemeral: true);
            }
            catch (Exception e)
            {
                await FollowupAsync($"COMMAND ERROR: {e.Message}", ephemeral: true);
            }
        }

        [SlashCommand("restart_server", "Restarts server")]
        public async Task RestartServer([Summary("minutes", "Restarts server after minutes")] uint minutes)
        {
            await DeferAsync();

            _ = Task.Run(async () =>
            {
                await _serverService.RestartServer(
                    Context.Client,
                    _botConfig.LogChannelId,
                    minutes * 60000);
            });

            await FollowupAsync($"Restarting server after {minutes} minutes...", ephemeral: true);
        }

        [SlashCommand("restart_cancel", "Cancel scheduled restart")]
        public async Task CancelRestart()
        {
            try
            {
                await DeferAsync();

                bool isCancelled = await _serverService.CancelRestart(Context.Client, _botConfig.LogChannelId);

                if (isCancelled)
                {
                    await FollowupAsync("✅ Scheduled restart cancelled", ephemeral: true);
                }
                else
                {
                    await FollowupAsync("⚠️ There is no scheduled restart", ephemeral: true);
                }
            }
            catch (Exception e)
            {
                await FollowupAsync($"COMMAND ERROR: {e.Message}", ephemeral: true);
            }
        }

        [SlashCommand("shutdown_server", "Shuts down server immediately")]
        public async Task ShutdownServer()
        {
            try
            {
                await DeferAsync();

                await _serverService.ShutdownServer(Context.Client, _botConfig.LogChannelId);

                await FollowupAsync("Server has shut down...", ephemeral: true);
            } catch(Exception e)
            {
                await FollowupAsync($"COMMAND ERROR: {e.Message}", ephemeral: true);
            }
        }

        // For Debug
        [SlashCommand("check_workshop_mods", "Your Server Mods Info")]
        public async Task CheckWorkshopItems()
        {
            try
            {
                await DeferAsync();

                await FollowupAsync("Checking Mod Update Date...", ephemeral: true);

                string[] ids = Array.Empty<string>();

                string configFilePath = Tools.GetServerIniPath(_botConfig.ServerName);
                if (!File.Exists(configFilePath))
                {
                    await FollowupAsync($"Failed to Get {configFilePath} File...");
                    return;
                }
                string workshopString = Tools.GetValueFromIni(configFilePath, "WorkshopItems");
                ids = workshopString.Split(';', StringSplitOptions.RemoveEmptyEntries);

                var modDetails = await _steamApi.GetWorkshopItemDetails(ids);

                if (modDetails == null || modDetails.Count == 0)
                {
                    await FollowupAsync("Failed to Fetch Mod Data...", ephemeral: true);
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

                await FollowupAsync(sb.ToString());
            } catch (Exception e)
            {
                await FollowupAsync($"COMMAND ERROR: {e.Message}", ephemeral: true);
            }            
        }
    }
}
