using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public static class ServerUtility
    {
        public static string GetServerFilePath()
        {
            string scriptPath = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                scriptPath = Application.BotConfig.WindowsServerPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                scriptPath = Application.BotConfig.LinuxServerPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                scriptPath = Application.BotConfig.UnixServerPath;
            }
            return scriptPath;
        }

        public static string GetServerIniPath()
        {
            string serverName = Application.BotConfig.ServerName;

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

        public static async Task RestartServer(IMessageChannel channel, List<uint> restartTimers)
        {
            restartTimers.Distinct().OrderByDescending(x => x).ToList();
            int restartTimersCount = restartTimers.Count;

            if (restartTimersCount == 0)
            {
                LogFile.WriteLine($"[ServerUtility] Error: RestartTimers are not configured...");
                await channel.SendMessageAsync($"❌ RestartTimers are not configured...");
                return;
            }

            if (channel != null)
            {
                for(int i=0; i<restartTimersCount; i++) {

                    int countdown = (int)(restartTimers[i] / 60000);


                    LogFile.WriteLine($"[ServerUtility] Restarting server in {countdown} minutes...");
                    await channel.SendMessageAsync($"🔄 Restarting server in {countdown} minutes...");
                    await RconManager.SendCommandAsync($"servermsg \"Server will restart in {countdown} minute(s). Please find a safe place.\"");

                    // if restartTimers is [600000,300000,60000], send messages and wait for [600000-300000=300000, 300000-60000=240000, 60000] miliseconds
                    if (i == restartTimers.Count - 1) {
                        await Task.Delay((int)restartTimers[i]);
                    }
                    else
                    {
                        await Task.Delay((int)(restartTimers[i] - restartTimers[i + 1]));
                    }
                }



                LogFile.WriteLine("[ServerUtility] Saving server...");
                await channel.SendMessageAsync("💾 Saving server...");
                await RconManager.SendCommandAsync("save");

                await Task.Delay(3000);

                LogFile.WriteLine("[ServerUtility] Shutting down server...");
                await channel.SendMessageAsync("⏳ Shutting down server. Wait patiently...");

                await RconManager.SendCommandAsync("quit");
                await Task.Delay(6000);
                await ServerProcessManager.WaitForExitAsync();

                LogFile.WriteLine("[ServerUtility] Restarting Server");
                await channel.SendMessageAsync("🚀 Restarting Server. Wait patiently...");
            }

            ServerProcessManager.StartServer();
        }

        public static async Task ShutdownServer(IMessageChannel channel)
        {
            if (channel != null)
            {
                LogFile.WriteLine("[ServerUtility] Saving server...");
                await channel.SendMessageAsync("💾 Saving server...");
                await RconManager.SendCommandAsync("save");

                await Task.Delay(3000);

                LogFile.WriteLine("[ServerUtility] Shutting down server...");
                await channel.SendMessageAsync("⏳ Shutting down server...");
                await RconManager.SendCommandAsync("quit");

                await ServerProcessManager.WaitForExitAsync();

            }
        }
    }
}
