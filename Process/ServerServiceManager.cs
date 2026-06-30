using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DotNETCoreDiscordBot
{

    public interface IServerServiceManager
    {
        IMessageChannel? GetChannel(DiscordSocketClient client, ulong channelId);
        Task StartServer(DiscordSocketClient client, ulong channelId);
        Task SaveServer(DiscordSocketClient client, ulong channelId);
        Task RestartServer(DiscordSocketClient client, ulong channelId, uint RestartTimer);
        bool CancelRestart(DiscordSocketClient client, ulong channelId);
        Task ShutdownServer(DiscordSocketClient client, ulong channelId);
        Task<double[]> GetUsage();
    }
    public class ServerServiceManager: IServerServiceManager
    {
        private readonly IServerProcessManager _serverProcess;
        private readonly BotConfig _botConfig;
        private readonly IRconManager _rconManager;

        // Token for Cancel Commands
        private CancellationTokenSource? _restartCts;
        // Semaphore for Preventing Race Condition on SaveServer
        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);

        public ServerServiceManager(IServerProcessManager serverProcess, BotConfig botConfig, IRconManager rconManager)
        {
            _serverProcess = serverProcess;
            _botConfig = botConfig;
            _rconManager = rconManager;
        }

        public IMessageChannel? GetChannel(DiscordSocketClient client, ulong channelId)
        {
            return client.GetChannel(channelId) as IMessageChannel;
        }

        public async Task StartServer(DiscordSocketClient client, ulong channelId)
        {
            try
            {
                await _rconManager.LoadRCONConfig();

                var channel = GetChannel(client, channelId);

                if (channel != null)
                {
                    await _serverProcess.StartServerProcess();

                    await _serverProcess.WaitForServerStart();

                    await channel.SendMessageAsync(Messages.Get("server_started_notification"));
                }
            } catch(Exception e)
            {
                throw;
            }
            
        }

        public async Task SaveServer(DiscordSocketClient client, ulong channelId)
        {
            try
            {
                var channel = GetChannel(client, channelId);

                if (channel != null)
                {

                    await _saveLock.WaitAsync();

                    try
                    {
                        await channel.SendMessageAsync(Messages.Get("save_server_notification"));

                        await _rconManager.SendCommandAsync("save");

                        await _serverProcess.WaitForServerSave();

                        await channel.SendMessageAsync(Messages.Get("server_saved_notification"));
                    }
                    finally
                    {
                        _saveLock.Release();
                    }
                }
            } catch (Exception e)
            {
                throw;
            }
        }

        public async Task RestartServer(DiscordSocketClient client, ulong channelId, uint RestartTimer)
        {
            try
            {
                var channel = GetChannel(client, channelId);

                if (channel == null) return;

                // Cancel Token if another RestartServer Task Executed
                _restartCts?.Cancel();
                _restartCts = new CancellationTokenSource();
                var token = _restartCts.Token;

                try
                {
                    List<uint> RestartTimers = new List<uint>
                        {
                            RestartTimer
                        };

                    if (RestartTimer > 60000)
                    {
                        RestartTimers.Add((uint)60000);
                    }

                    int RestartTimesCount = RestartTimers.Count;

                    if (RestartTimesCount == 0)
                    {
                        LogFile.WriteLine(Messages.Get("restart_config_error"), channelId);
                        return;
                    }

                    for (int i = 0; i < RestartTimesCount; i++)
                    {
                        // goto catch(OperationCanceledException) if token has activated
                        token.ThrowIfCancellationRequested();

                        int countdown = (int)(RestartTimers[i] / 60000);

                        LogFile.WriteLine(Messages.Get("restart_server_notification").KeyFormat(("minutes",  countdown)), channelId);
                        await _rconManager.SendCommandAsync("servermsg \""+Messages.Get("restart_server_notification_rcon").KeyFormat(("minutes", countdown))+"\"");

                        // if RestartTimes is [600000,60000], send messages and wait for [600000-60000=540000] miliseconds
                        if (i == (RestartTimesCount - 1))
                        {
                            await Task.Delay((int)RestartTimers[i]);
                        }
                        else
                        {
                            await Task.Delay((int)(RestartTimers[i] - RestartTimers[i + 1]));
                        }
                    }

                    await ShutdownServer(client, channelId);
                    LogFile.WriteLine(Messages.Get("restart_server"), channelId);
                    await StartServer(client, channelId);

                }
                catch (OperationCanceledException)
                {
                    LogFile.WriteLine(Messages.Get("restart_server_canceled"), channelId);
                    await _rconManager.SendCommandAsync("servermsg \""+Messages.Get("restart_server_canceled_rcon")+"\"");
                }
                finally
                {
                    _restartCts.Dispose();
                    _restartCts = null;
                }
            } catch(Exception e)
            {
                throw;
            }
            

        }
        public bool CancelRestart(DiscordSocketClient client, ulong channelId)
        {
            try
            {
                // If there is no scheduled restart
                if (_restartCts == null || _restartCts.IsCancellationRequested)
                {
                    return false;
                }

                _restartCts.Cancel();

                return true;
            } catch(Exception e)
            {
                throw;
            }
        }

        public async Task ShutdownServer(DiscordSocketClient client, ulong channelId)
        {
            try
            {
                var channel = GetChannel(client, channelId);

                if (channel != null)
                {
                    await SaveServer(client, channelId);

                    await Task.Delay(3000);

                    LogFile.WriteLine(Messages.Get("shutdown_server"), channelId);
                    await _rconManager.SendCommandAsync("quit");
                    await Task.Delay(6000);

                    await _serverProcess.WaitForExitAsync();

                }
            } catch (Exception e)
            {
                throw;
            }
            
        }

        public async Task<double[]> GetUsage()
        {
            try
            {
                double[] result = await _serverProcess.GetProcessUsage();
                return result;

            } catch(Exception e)
            {
                throw;
            }
        }
    }
}
