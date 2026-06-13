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
    public static class ServerServiceManager
    {
        // Semaphore for Preventing Race Condition on Save
        private static readonly SemaphoreSlim SaveLock = new SemaphoreSlim(1, 1);

        public static string GetServerFilePath()
        {
            string scriptPath = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                scriptPath = Application.BotConfig.ServerLocationSettings.WindowsServerPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                scriptPath = Application.BotConfig.ServerLocationSettings.LinuxServerPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                scriptPath = Application.BotConfig.ServerLocationSettings.UnixServerPath;
            }
            return scriptPath;
        }

        public static string GetServerIniPath()
        {
            string serverName = Application.BotConfig.ServerLocationSettings.ServerName;

            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string targetDirectory = Path.Combine(homePath, "Zomboid", "Server");

            return Path.Combine(targetDirectory, $"{serverName}.ini");
        }

        public static string GetValueFromIni(string iniPath, string key)
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

        public static IMessageChannel? GetChannel(DiscordSocketClient client, ulong channelId)
        {
            return client.GetChannel(channelId) as IMessageChannel;
        }

        public static async Task StartServer(DiscordSocketClient client, ulong channelId)
        {
            var channel = GetChannel(client, channelId);

            if (channel != null)
            {

                ServerProcessManager.ServerStarted = new TaskCompletionSource<bool>();

                // serverStartedTrigger
                ServerProcessManager.StartServerProcess();

                await ServerProcessManager.ServerStarted.Task;

                await channel.SendMessageAsync($"@everyone 🔥 Server Started!!!");
            }
        }

        public static async Task SaveServer(DiscordSocketClient client, ulong channelId)
        {

            var channel = GetChannel(client, channelId);

            if (channel != null)
            {

                await SaveLock.WaitAsync();

                try
                {
                    LogFile.WriteLine("[ServerUtility] Saving server...");
                    await channel.SendMessageAsync("💾 Saving server...");

                    ServerProcessManager.ServerSaved = new TaskCompletionSource<bool>();

                    await RconManager.SendCommandAsync("save");

                    await ServerProcessManager.ServerSaved.Task;

                    await channel.SendMessageAsync("💾 Saving Finished");
                }
                finally
                {
                    SaveLock.Release();
                }
            }
        }

        public static async Task RestartServer(DiscordSocketClient client, ulong channelId, uint RestartTimer)
        {

            var channel = GetChannel(client, channelId);

            if (channel != null)
            {
                List<uint> RestartTimers = new List<uint>
                {
                    RestartTimer
                };

                if (RestartTimer > 60000) {
                    RestartTimers.Append((uint)60000);
                }

                RestartTimers.Distinct().OrderByDescending(x => x).ToList();
                int RestartTimesCount = RestartTimers.Count;

                if (RestartTimesCount == 0)
                {
                    LogFile.WriteLine($"[ServerUtility] Error: RestartTimes are not configured...");
                    await channel.SendMessageAsync($"❌ RestartTimes are not configured...");
                    return;
                }

                for (int i=0; i<RestartTimesCount; i++) {

                    int countdown = (int)(RestartTimers[i] / 60000);


                    LogFile.WriteLine($"[ServerUtility] Restarting server in {countdown} minutes...");
                    await channel.SendMessageAsync($"🔄 Restarting server in {countdown} minutes...");
                    await RconManager.SendCommandAsync($"servermsg \"Server will restart in {countdown} minute(s). Please find a safe place.\"");

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

                LogFile.WriteLine("[ServerUtility] Restarting Server");
                await channel.SendMessageAsync("🚀 Restarting Server. Wait patiently...");

                await StartServer(client, channelId);
            }
        }

        public static async Task ShutdownServer(DiscordSocketClient client, ulong channelId)
        {
            var channel = GetChannel(client, channelId);

            if (channel != null)
            {
                await SaveServer(client, channelId);

                await Task.Delay(3000);

                LogFile.WriteLine("[ServerUtility] Shutting down server...");
                await channel.SendMessageAsync("⏳ Shutting down server...");
                await RconManager.SendCommandAsync("quit");
                await Task.Delay(6000);

                await ServerProcessManager.WaitForExitAsync();

            }
        }
    }
}
