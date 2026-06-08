using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public static class Scheduler
    {
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static DateTime _lastCheckTime = DateTime.UtcNow;

        public static void StartAll(DiscordSocketClient client)
        {
            LogFile.WriteLine("[Scheduler] Initialising Background Schedules...");

            _ = CheckModUpdateScheduler(client, _cts.Token);
        }
        public static void StopAll()
        {
            _cts.Cancel();

            LogFile.WriteLine("[Scheduler] All Background Scheduler Stopped...");
        }


        private static async Task CheckModUpdateScheduler(DiscordSocketClient client, CancellationToken token)
        {
            uint intervalMs = Application.BotConfig.ServerScheduleSettings.WorkshopItemUpdateSchedule;
            uint RestartMs = Application.BotConfig.ServerScheduleSettings.WorkshopItemUpdateRestartTimer;
            TimeSpan interval = TimeSpan.FromMilliseconds(intervalMs);

            using var timer = new PeriodicTimer(interval);

            LogFile.WriteLine($"[Workshop Item Update Scheduler] Workshop Item Update Scheduler Running...");

            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    string iniPath = ServerUtility.GetServerIniPath();
                    string workshopIdValue = ServerUtility.GetValueFromIni(iniPath, "WorkshopItems");
                    string[] ids = workshopIdValue.Split(';', StringSplitOptions.RemoveEmptyEntries);

                    if (ids.Length == 0) return;

                    var modDetails = await SteamWebAPI.GetWorkshopItemDetailsAsync(ids);
                    if (modDetails == null || modDetails.Count == 0) return;

                    bool updateFound = false;
                    List<string> updatedModNames = new List<string>();

                    foreach (var mod in modDetails)
                    {
                        DateTime modUpdateTime = DateTimeOffset.FromUnixTimeSeconds(mod.TimeUpdated).UtcDateTime;
                        if (modUpdateTime > _lastCheckTime)
                        {
                            updateFound = true;
                            updatedModNames.Add(mod.Title);
                        }
                    }

                    _lastCheckTime = DateTime.UtcNow;

                    if (updateFound)
                    {
                        LogFile.WriteLine($"[Workshop Item Update Scheduler] Mod Update Found!!! ({string.Join(", ", updatedModNames)})");

                        var guild = client.GetGuild(Application.BotConfig.GuildId);
                        var publicChannel = guild?.GetTextChannel(Application.BotConfig.PublicChannelId);

                        if (publicChannel != null)
                        {
                            await publicChannel.SendMessageAsync($"🚨 [Workshop Item Update Scheduler] Mod Update Found!!! ({string.Join(", ", updatedModNames)})");
                        }

                        List<uint> tempTimers = [RestartMs, Math.Max(RestartMs / 2, 60000), 60000];

                        await ServerUtility.RestartServer(publicChannel, tempTimers);
                    }

                }
            }
            catch (OperationCanceledException)
            {
                LogFile.WriteLine("[Workshop Item Update Scheduler] Workshop Item Update Scheduler Cancelled...");
            } catch(Exception e)
            {
                LogFile.WriteLine($"[Workshop Item Update Scheduler] Scheduler Error: {e.Message}");
            }
        }
    }
}
