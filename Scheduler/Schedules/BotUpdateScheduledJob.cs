using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot.Scheduler
{
    public class BotUpdateScheduledJob : IScheduledJob
    {

        private readonly DiscordSocketClient _client;
        private readonly BotConfig _botConfig;
        private readonly ILogFile _logFile;
        private readonly IServerServiceManager _serverService;
        private readonly IWebAPIManager _webApi;

        public BotUpdateScheduledJob(DiscordSocketClient client, BotConfig botConfig, ILogFile logFile, IServerServiceManager serverService, IWebAPIManager webApi)
        {
            _client = client;
            _botConfig = botConfig;
            _logFile = logFile;
            _serverService = serverService;
            _webApi = webApi;
        }

        public async Task ExecuteAsync(CancellationToken token)
        {
            uint intervalMs = _botConfig.ServerScheduleSettings.BotUpdateSchedule;
            uint RestartTimer = _botConfig.ServerScheduleSettings.RestartTimer;
            TimeSpan interval = TimeSpan.FromMilliseconds(intervalMs);

            using var timer = new PeriodicTimer(interval);
            _logFile.WriteLine(Messages.Get("bot_scheduler_running"));

            try
            {
                var tools = new Tools();

                while (await timer.WaitForNextTickAsync(token))
                {
                    bool updateFound = false;

                    var assembly = Assembly.GetExecutingAssembly();
                    Version localVersion = assembly.GetName().Version;

                    Version remoteVersion = await _webApi.GetBotVersion(localVersion.ToString());

                    if (remoteVersion > localVersion)
                    {
                        updateFound = true;
                    }

                    if (updateFound)
                    {
                        _logFile.WriteLine(Messages.Get("bot_scheduler_update_found").KeyFormat(("version", remoteVersion.ToString())), _botConfig.LogChannelId);

                        await _serverService.RestartServer(_client, _botConfig.LogChannelId, RestartTimer);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logFile.WriteLine(Messages.Get("bot_scheduler_stop"));
            }
            catch (Exception e)
            {
                _logFile.WriteLine(Messages.Get("bot_scheduler_error").KeyFormat(("error", e.Message)));
            }
        }
    }
}
