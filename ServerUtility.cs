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

        public static async Task RestartServer(IMessageChannel channel, uint restartTimerMs)
        {
            int restartTimerMinutes = (int)(restartTimerMs / 60000);

            if (channel != null)
            {
                LogFile.WriteLine($"[ServerUtility] Restarting server in {restartTimerMinutes} minutes...");
                await channel.SendMessageAsync($"🔄 Restarting server in {restartTimerMinutes} minutes...");

                await RconManager.SendCommandAsync($"servermsg \"Server will restart in {restartTimerMinutes} minute(s). Please find a safe place.\"");

                await Task.Delay((int)restartTimerMs);


                LogFile.WriteLine("[ServerUtility] Saving server...");
                await channel.SendMessageAsync("💾 Saving server...");

                await RconManager.SendCommandAsync("save");
                await Task.Delay(3000);

                await RconManager.SendCommandAsync("quit");


                LogFile.WriteLine("[ServerUtility] Shutting down server...");
                await channel.SendMessageAsync("⏳ Shutting down server. Wait patiently...");

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

                await RconManager.SendCommandAsync("quit");


                LogFile.WriteLine("[ServerUtility] Shutting down server...");
                await channel.SendMessageAsync("⏳ Shutting down server...");

                await ServerProcessManager.WaitForExitAsync();

            }
        }
    }
}
