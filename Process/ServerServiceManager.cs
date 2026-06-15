using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DotNETCoreDiscordBot
{

    public interface IServerServiceManager
    {
        string GetServerIniPath();
        string GetValueFromIni(string iniPath, string key);
        IMessageChannel? GetChannel(DiscordSocketClient client, ulong channelId);

        Task StartServer(DiscordSocketClient client, ulong channelId);
        Task SaveServer(DiscordSocketClient client, ulong channelId);
        Task RestartServer(DiscordSocketClient client, ulong channelId, uint RestartTimer);
        Task ShutdownServer(DiscordSocketClient client, ulong channelId);
    }
    public class ServerServiceManager: IServerServiceManager
    {
        private readonly IServerProcessManager _serverProcess;
        private readonly BotConfig _botConfig;
        private readonly IRconManager _rconManager;

        // Semaphore for Preventing Race Condition on SaveServer
        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);

        public ServerServiceManager(IServerProcessManager serverProcess, BotConfig botConfig, IRconManager rconManager)
        {
            _serverProcess = serverProcess;
            _botConfig = botConfig;
            _rconManager = rconManager;
        }

        public string GetServerIniPath()
        {
            string serverName = _botConfig.ServerName;

            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string targetDirectory = Path.Combine(homePath, "Zomboid", "Server");

            return Path.Combine(targetDirectory, $"{serverName}.ini");
        }

        public string GetValueFromIni(string iniPath, string key)
        {
            if (!File.Exists(iniPath)) return "";

            var lines = File.ReadAllLines(iniPath);
            foreach (var line in lines)
            {
                if (line.StartsWith($"{key}="))
                {
                    string value = line.Substring($"{key}=".Length);
                    return value;
                }
            }
            return "";
        }

        public IMessageChannel? GetChannel(DiscordSocketClient client, ulong channelId)
        {
            return client.GetChannel(channelId) as IMessageChannel;
        }

        public async Task StartServer(DiscordSocketClient client, ulong channelId)
        {
            var channel = GetChannel(client, channelId);

            if (channel != null)
            {

                await _serverProcess.StartServerProcess();

                await _serverProcess.WaitForServerStart();

                await channel.SendMessageAsync($"@everyone 🔥 Server Started!!!");
            }
        }

        public async Task SaveServer(DiscordSocketClient client, ulong channelId)
        {

            var channel = GetChannel(client, channelId);

            if (channel != null)
            {

                await _saveLock.WaitAsync();

                try
                {
                    await channel.SendMessageAsync("💾 Saving server...");

                    await _rconManager.SendCommandAsync("save");

                    await _serverProcess.WaitForServerSave();

                    await channel.SendMessageAsync("💾 Saving Finished");
                }
                finally
                {
                    _saveLock.Release();
                }
            }
        }

        public async Task RestartServer(DiscordSocketClient client, ulong channelId, uint RestartTimer)
        {

            var channel = GetChannel(client, channelId);

            if (channel != null)
            {
                List<uint> RestartTimers = new List<uint>
                {
                    RestartTimer
                };

                if (RestartTimer > 60000) {
                    RestartTimers.Add((uint)60000);
                }

                RestartTimers.Distinct().OrderByDescending(x => x).ToList();
                int RestartTimesCount = RestartTimers.Count;

                if (RestartTimesCount == 0)
                {
                    await LogFile.WriteLine($"[ServerUtility] Error: RestartTimes are not configured...");
                    await channel.SendMessageAsync($"❌ RestartTimes are not configured...");
                    return;
                }

                for (int i=0; i<RestartTimesCount; i++) {

                    int countdown = (int)(RestartTimers[i] / 60000);


                    await LogFile.WriteLine($"[ServerUtility] Restarting server in {countdown} minutes...");
                    await channel.SendMessageAsync($"🔄 Restarting server in {countdown} minutes...");
                    await _rconManager.SendCommandAsync($"servermsg \"Server will restart in {countdown} minute(s). Please find a safe place.\"");

                    // if RestartTimes is [600000,300000,60000], send messages and wait for [600000-300000=300000, 300000-60000=240000, 60000] miliseconds
                    if (i == RestartTimers.Count - 1) {
                        await Task.Delay((int)RestartTimers[i]);
                    }
                    else
                    {
                        await Task.Delay((int)(RestartTimers[i] - RestartTimers[i + 1]));
                    }
                }

                await ShutdownServer(client, channelId);

                await LogFile.WriteLine("[ServerUtility] Restarting Server");
                await channel.SendMessageAsync("🚀 Restarting Server. Wait patiently...");

                await StartServer(client, channelId);
            }
        }

        public async Task ShutdownServer(DiscordSocketClient client, ulong channelId)
        {
            var channel = GetChannel(client, channelId);

            if (channel != null)
            {
                await SaveServer(client, channelId);

                await Task.Delay(3000);

                await LogFile.WriteLine("[ServerUtility] Shutting down server...");
                await channel.SendMessageAsync("⏳ Shutting down server...");
                await _rconManager.SendCommandAsync("quit");
                await Task.Delay(6000);

                await _serverProcess.WaitForExitAsync();

            }
        }
    }
}
