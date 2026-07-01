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

        Task<double[]> GetProcessUsage();
    }

    public class ServerProcessManager : IServerProcessManager
    {
        private readonly BotConfig _botConfig;
        private readonly ILogFile _logFile;
        private Process? _serverProcess = null;
        private IServerProcess _curOS;

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

        public ServerProcessManager(BotConfig botConfig, ILogFile logFile)
        {
            _botConfig = botConfig;
            _logFile = logFile;
        }

        public async Task StartServerProcess()
        {

            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _logFile.WriteLine(Messages.Get("server_already_started"));
                return;
            }

            try
            {
                var factory = new ServerProcessFactory();
                _curOS = factory.Create(_botConfig, _logFile);

                await _curOS.ParseServerScript();

                _logFile.WriteLine(Messages.Get("servername_configured").KeyFormat(("servername", _botConfig.ServerName)), _botConfig.LogChannelId);
                await _botConfig.Save(_logFile);

                _serverProcess = new Process();

                _curOS.SetupProcessStartInfo(
                    _serverProcess.StartInfo,
                    _botConfig.ServerName
                );

                _serverProcess.StartInfo.UseShellExecute = false;
                _serverProcess.StartInfo.CreateNoWindow = true;
                _serverProcess.StartInfo.RedirectStandardInput = true;
                _serverProcess.StartInfo.RedirectStandardOutput = true;
                _serverProcess.StartInfo.RedirectStandardError = true;
                _serverProcess.StartInfo.WorkingDirectory = _curOS.GetDirectory();

                _serverProcess.OutputDataReceived += async (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logFile.WriteLine($"[PZ_SERVER] {e.Data}");

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
                        _logFile.WriteLine($"[PZ_ERROR] {e.Data}");
                };

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                _logFile.WriteLine(Messages.Get("process_manager_error").KeyFormat(("error", e.Message)));
                throw;
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
                catch (Exception e)
                {
                    _logFile.WriteLine(Messages.Get("process_manager_error").KeyFormat(("error", e.Message)));
                    throw;
                }
            }
        }

        public async Task<double[]> GetProcessUsage()
        {
            if (_curOS == null) return [0.0,0.0];

            double cpu = await _curOS.GetCPUUsage();
            double ram = _curOS.GetRAMUsage();

            return [ cpu, ram ];
        }
    }
}
