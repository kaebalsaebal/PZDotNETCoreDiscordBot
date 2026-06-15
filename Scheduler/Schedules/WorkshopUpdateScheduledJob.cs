using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot.Scheduler
{
    public class WorkshopUpdateScheduledJob : IScheduledJob
    {
        public string JobName => "Workshop Item Update Check Scheduler";

        private readonly DiscordSocketClient _client;
        private readonly BotConfig _botConfig;
        private readonly IServerServiceManager _serverService;
        private readonly ISteamWebAPI _steamApi;

        private DateTime _lastCheckTime = DateTime.UtcNow;
        private readonly string _location = Path.Combine(AppContext.BaseDirectory, "needupdatefile.txt");
        private static readonly SemaphoreSlim _workshopLock = new SemaphoreSlim(1, 1);

        public WorkshopUpdateScheduledJob(DiscordSocketClient client, BotConfig botConfig, IServerServiceManager serverService, ISteamWebAPI steamApi)
        {
            _client = client;
            _botConfig = botConfig;
            _serverService = serverService;
            _steamApi = steamApi;
        }

        public async Task ExecuteAsync(CancellationToken token)
        {
            uint intervalMs = _botConfig.ServerScheduleSettings.WorkshopItemUpdateSchedule;
            uint RestartTimer = _botConfig.ServerScheduleSettings.RestartTimer;
            TimeSpan interval = TimeSpan.FromMilliseconds(intervalMs);

            using var timer = new PeriodicTimer(interval);
            await LogFile.WriteLine($"[{JobName}] Scheduler Running...");

            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    string iniPath = _serverService.GetServerIniPath();
                    string workshopIdValue = _serverService.GetValueFromIni(iniPath, "WorkshopItems");
                    string[] ids = workshopIdValue.Split(';', StringSplitOptions.RemoveEmptyEntries);

                    if (ids.Length == 0) continue;

                    var modDetails = await _steamApi.GetWorkshopItemDetails(ids);
                    if (modDetails == null || modDetails.Count == 0) continue;

                    bool updateFound = false;
                    List<string> updatedModNames = new List<string>();
                    List<string> updatedModIds = new List<string>();

                    foreach (var mod in modDetails)
                    {
                        DateTime modUpdateTime = DateTimeOffset.FromUnixTimeSeconds(mod.TimeUpdated).UtcDateTime;
                        if (modUpdateTime > _lastCheckTime)
                        {
                            updateFound = true;
                            updatedModNames.Add(mod.Title);
                            updatedModIds.Add(mod.PublishedFileId);
                        }
                    }

                    _lastCheckTime = DateTime.UtcNow;

                    if (updateFound)
                    {
                        await LogFile.WriteLine($"[{JobName}] Mod Update Found!!! ({string.Join(", ", updatedModNames)})");

                        await _workshopLock.WaitAsync();

                        try
                        {
                            string msg = string.Join("\n", updatedModIds);
                            using (var file = File.CreateText(_location))
                            {
                                await file.WriteLineAsync(msg);
                            }
                        }
                        finally
                        {
                            _workshopLock.Release();
                        }

                        var channel = _serverService.GetChannel(_client, _botConfig.LogChannelId);
                        if (channel != null)
                        {
                            await channel.SendMessageAsync($"🚨 [{JobName}] Mod Update Found!!! ({string.Join(", ", updatedModNames)})");
                            await _serverService.RestartServer(_client, channel.Id, RestartTimer);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                await LogFile.WriteLine($"[{JobName}] Scheduler Cancelled...");
            }
            catch (Exception e)
            {
                await LogFile.WriteLine($"[{JobName}] Error: {e.Message}");
            }
        }
    }
}
