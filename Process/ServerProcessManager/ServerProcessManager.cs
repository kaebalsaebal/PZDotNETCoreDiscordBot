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

    public interface IServerProcessManager
    {
        Task WaitForServerStart();
        Task WaitForServerSave();

        Task StartServerProcess();
        Task WaitForExitAsync();
        void KillServerProcess();
    }

    public class ServerProcessManager : IServerProcessManager
    {
        private readonly BotConfig _botConfig;
        private Process _serverProcess = null;

        private TaskCompletionSource<bool> _serverStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource<bool> _serverSaved = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitForServerStart()
        {
            return _serverStarted.Task;
        }
        public Task WaitForServerSave()
        {
            return _serverSaved.Task;
        }

        public ServerProcessManager(BotConfig botConfig)
        {
            _botConfig = botConfig;
        }

        private ServerProcessStrategy GetCurrentOSStrategy()
        {
            return ServerProcessFactory.Create(_botConfig);
        }

        public async Task StartServerProcess()
        {

            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                await LogFile.WriteLine("[ServerProcessManager] Error: Server already started");
                return;
            }

            try
            {
                var strategy = GetCurrentOSStrategy();

                await strategy.ParseServerScript();

                await LogFile.WriteLine($"[ServerProcessManager] Servername has been configured: {_botConfig.ServerName}");
                await _botConfig.Save();

                _serverProcess = new Process();

                strategy.SetupProcessStartInfo(
                    _serverProcess.StartInfo,
                    _botConfig.ServerName
                );

                _serverProcess.StartInfo.UseShellExecute = false;
                _serverProcess.StartInfo.CreateNoWindow = true;
                _serverProcess.StartInfo.RedirectStandardInput = true;
                _serverProcess.StartInfo.RedirectStandardOutput = true;
                _serverProcess.StartInfo.RedirectStandardError = true;
                _serverProcess.StartInfo.WorkingDirectory = strategy.GetDirectory();

                _serverProcess.OutputDataReceived += async (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        await LogFile.WriteLine($"[PZ_SERVER] {e.Data}");

                        if (e.Data.Contains("SERVER STARTED"))
                        {
                            _serverStarted.TrySetResult(true);
                        }

                        if (e.Data.Contains("Saving finish"))
                        {
                            _serverSaved.TrySetResult(true);
                        }
                    }
                };

                _serverProcess.ErrorDataReceived += async (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        await LogFile.WriteLine($"[PZ_ERROR] {e.Data}");
                };

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                await LogFile.WriteLine($"[ServerProcessManager] Error: {e.Message}");
            }
        }

        public async Task WaitForExitAsync()
        {
            if (_serverProcess == null || _serverProcess.HasExited) return;
            await _serverProcess.WaitForExitAsync();
        }

        public async void KillServerProcess()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill(true);
                    await _serverProcess.WaitForExitAsync();
                }
                catch (Exception ex)
                {
                    await LogFile.WriteLine($"[ServerProcessManager] Error While Stopping Server: {ex.Message}");
                }
            }
        }
    }
}
