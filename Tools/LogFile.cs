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
    public static class LogFile
    {
        private static string _location
        {
            get
            {
                string logDate = DateTime.Now.ToString("yyMMdd");

                return Path.Combine(AppContext.BaseDirectory, "PZBot_Logs", $"PZBot.log.{logDate}");
            }
        }

        private static readonly SemaphoreSlim _logLock = new SemaphoreSlim(1, 1);

        private static IServiceProvider _services;

        public static void LoadService(IServiceProvider services)
        {
            _services = services;
        }

        public static string GetDateTime()
        {
            return DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        public static async Task WriteLine(string log, ulong? channelId = null)
        {
            var msg = "(" + GetDateTime() + ") " + log;
            Console.WriteLine(msg);

            var config = _services?.GetService<BotConfig>();
            bool isSaveAsFile = (config != null) ? config.SaveAsFile : true;

            if (isSaveAsFile)
            {
                await _logLock.WaitAsync();

                try
                {
                    string logPath = Path.GetDirectoryName(_location);

                    if (!string.IsNullOrEmpty(logPath))
                    {
                        Directory.CreateDirectory(logPath);
                    }

                    using (var file = File.AppendText(_location))
                    {
                        await file.WriteLineAsync(msg);
                    }
                } 
                catch(Exception e)
                {
                    Console.WriteLine($"[LogFile] Log File Write Error: {e.Message}");
                }
                finally
                {
                    _logLock.Release();
                }

                if (_services != null && channelId.HasValue)
                {
                    _ = SendLogToDiscord(log, channelId.Value);
                }
            }
        }

        private static async Task SendLogToDiscord(string log, ulong channelId)
        {
            try
            {
                var client = _services.GetService<DiscordSocketClient>();

                if (client != null && channelId != 0)
                {
                    var channel = client.GetChannel(channelId) as IMessageChannel;
                    if (channel != null)
                    {
                        await channel.SendMessageAsync($"📝 `{GetDateTime()}` {log}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[LogFile] Discord Log Send Error: {e.Message}");
            }
        }
    }
}
