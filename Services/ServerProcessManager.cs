using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public static class ServerProcessManager
    {
        private static Process serverProcess = null;

        private static bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static string BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Zomboid");

        //Task Trigger
        public static TaskCompletionSource<bool> ServerStarted;
        public static TaskCompletionSource<bool> ServerSaved;

        private static async Task ParseServerScript(string scriptPath)
        {

            if (!File.Exists(scriptPath)) return;

            string[] lines = File.ReadAllLines(scriptPath);
            bool needModify = false;
            List<string> newLines = new List<string>();

            foreach (string line in lines)
            {
                if (line.Contains("java") || line.Contains("zomboid.steam"))
                {
                    string[] parameters = line.Split(new string[] { " -" }, StringSplitOptions.None);

                    foreach (string param in parameters)
                    {
                        if (param.Contains("user.home"))
                        {
                            string customHome = param.Split('=').Last().Replace("\"", "");

                            BasePath = customHome;

                            if (Directory.Exists(Path.Combine(BasePath, "Zomboid")))
                            {
                                BasePath = Path.Combine(BasePath, "Zomboid");
                            }
                            LogFile.WriteLine($"[ServerProcessManager] Custom Location Found: {BasePath}");
                        }

                        if (isWindows && param.StartsWith("servername "))
                        {
                            string[] nameParts = param.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                            if (nameParts.Length > 1)
                            {
                                string servername = nameParts[1].Replace("\"", "").Trim();

                                Application.BotConfig.ServerProcessSettings.ServerName = servername;

                                LogFile.WriteLine($"[ServerProcessManager] Servername has been configured: {servername}");

                                await Application.BotConfig.Save();
                            }
                        }
                    }

                    newLines.Add(line);
                }

                if (line.Trim().ToLower().Contains("pause") || line.Trim().ToLower().StartsWith("read "))
                {
                    needModify = true;
                }
            }

            if (needModify)
            {
                File.WriteAllLines(scriptPath, newLines);
                LogFile.WriteLine($"[ServerProcessManager] Removed pause/read in Server Script File");
            }
        }

        public static void StartServerProcess()
        {

            if (serverProcess != null && !serverProcess.HasExited)
            {
                LogFile.WriteLine("[ServerProcessManager] Error: Server already started");
                return;
            }

            string scriptPath = "";

            scriptPath = ServerServiceManager.GetServerFilePath();
            if (string.IsNullOrEmpty(scriptPath))
            {
                LogFile.WriteLine("[ServerProcessManager] Error: This OS is not supported");
                return;
            }

            //For Debug
            //scriptPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Project Zomboid Dedicated Server\\server.bat";

            try
            {
                ParseServerScript(scriptPath);

                serverProcess = new Process();

                if (isWindows)
                {
                    serverProcess.StartInfo.FileName = scriptPath;
                }
                else
                {
                    Process.Start("chmod", $"+x \"{scriptPath}\"")?.WaitForExit();
                    serverProcess.StartInfo.FileName = "/bin/bash";

                    serverProcess.StartInfo.Arguments = $"\"{scriptPath}\" -servername \"{Application.BotConfig.ServerProcessSettings.ServerName}\"";
                }

                serverProcess.StartInfo.UseShellExecute = false;
                serverProcess.StartInfo.CreateNoWindow = true;
                serverProcess.StartInfo.RedirectStandardInput = true;
                serverProcess.StartInfo.RedirectStandardOutput = true;
                serverProcess.StartInfo.RedirectStandardError = true;
                serverProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(scriptPath);

                serverProcess.OutputDataReceived += async (sender, e) =>
                {
                    // Handle Process Log Script
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        LogFile.WriteLine($"[PZ_SERVER] {e.Data}");

                        // Callback to send Discord Message on Other Classes
                        if (e.Data.Contains("SERVER STARTED"))
                        {
                            ServerStarted.TrySetResult(true);
                        }

                        // Callback2: If Save finished
                        if (e.Data.Contains("Saving finish"))
                        {
                            ServerSaved.TrySetResult(true);
                        }
                    }
                };

                serverProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        LogFile.WriteLine($"[PZ_ERROR] {e.Data}");
                };

                serverProcess.Start();
                serverProcess.BeginOutputReadLine();
                serverProcess.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                LogFile.WriteLine($"[ServerProcessManager] Error: {e.Message}");
            }
        }

        public static async Task WaitForExitAsync()
        {
            if (serverProcess == null || serverProcess.HasExited)
            {
                return;
            }

            await serverProcess.WaitForExitAsync();
        }

        // Shutting Down Server(Force)
        public static void KillServerProcess()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {

                try
                {
                    serverProcess.Kill(true);
                    serverProcess.WaitForExit();
                }
                catch (Exception ex)
                {
                    LogFile.WriteLine($"[ServerProcessManager] Error While Stopping Server: {ex.Message}");
                }
            }
        }
    }
}
