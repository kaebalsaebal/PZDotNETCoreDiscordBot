using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DotNETCoreDiscordBot
{
    public interface ILogFile
    {
        void WriteLine(string log, ulong? channelId = null);
    }

    public class LogFile: ILogFile
    {
        private readonly SemaphoreSlim _logLock = new SemaphoreSlim(1, 1);

        private readonly BotConfig _config;
        private readonly DiscordSocketClient _client;

        public LogFile(BotConfig config, DiscordSocketClient client)
        {
            _config = config;
            _client = client;
        }

        private string GetLocation()
        {
            string logDate = DateTime.Now.ToString(Messages.Get("date_format").Replace("/",""));
            return Path.Combine(AppContext.BaseDirectory, "PZBot_Logs", $"PZBot_log_{logDate}.txt");
        }

        private string GetDateTime()
        {
            return DateTime.Now.ToString($"{Messages.Get("date_format")} HH:mm:ss");
        }

        private async Task SendLogToDiscord(string log, ulong channelId)
        {
            try
            {
                if (_client.GetChannel(channelId) is IMessageChannel channel)
                {
                    await channel.SendMessageAsync($"📝 `{GetDateTime()}` {log}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Messages.Get("logfile_error").KeyFormat(("error", e.Message)));
            }
        }

        public void WriteLine(string log, ulong? channelId = null)
        {
            var msg = "(" + GetDateTime() + ") " + log;
            Console.WriteLine(msg);

            bool isSaveAsFile = _config != null ? _config.SaveAsFile : true;

            if (isSaveAsFile)
            {
                _ = Task.Run(async() =>
                {
                    await _logLock.WaitAsync();

                    try
                    {
                        string location = GetLocation();
                        string logPath = Path.GetDirectoryName(location);

                        if (!string.IsNullOrEmpty(logPath))
                        {
                            Directory.CreateDirectory(logPath);
                        }

                        using (var file = File.AppendText(location))
                        {
                            await file.WriteLineAsync(msg);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(Messages.Get("logfile_error").KeyFormat(("error", e.Message)));
                    }
                    finally
                    {
                        _logLock.Release();
                    }

                    if (_client != null && channelId.HasValue && channelId.Value != 0)
                    {
                        await SendLogToDiscord(log, channelId.Value);
                    }
                });
                
            }
        }
    }
}
