using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public static class Application
    {

        private static DiscordSocketClient _discordSocketClient;
        private static CommandService _commands;
        private static IServiceProvider _services;

        public static BotConfig BotConfig { get; private set; }

        public static void Main(string[] _) => MainAsync(_).GetAwaiter().GetResult();

        private static async Task MainAsync(string[] param)
        {
            // Stop Zomboid Dedi if bot process down
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                ServerProcessManager.StopServer();
            };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                ServerProcessManager.StopServer();
                eventArgs.Cancel = false;
            };

            // Load conf file
            if (!File.Exists(BotConfig.SettingsFile))
            {
                LogFile.WriteLine("[Program] Config File Not Found. Making New Config...");
                BotConfig = new BotConfig();
                BotConfig.Save();
            }
            else
            {
                LogFile.WriteLine("[Program] Config File Found! Loading Config...");
                BotConfig = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(BotConfig.SettingsFile),
                    new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
            }

            for (int i = 0; i < param.Length; i++)
            {
                if (param[i].ToLower() == "-servername" && i + 1 < param.Length)
                {
                    string servername = param[i + 1].Replace("\"", "").Trim();

                    if (!string.IsNullOrEmpty(servername))
                    {
                        // 로드된 설정 파일의 값보다 명령줄 인수가 우선순위를 가집니다.
                       BotConfig.ServerName = servername;
                        LogFile.WriteLine($"[Program] Servername Has Configured: {servername}...");
                    }
                    break;
                }
            }

            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            // Initialise Discord Client
            _discordSocketClient = new DiscordSocketClient(config);

            _discordSocketClient.Log += DiscordLog;

            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false
            });
            _commands.Log += DiscordLog;

            _services = new ServiceCollection().AddSingleton(_discordSocketClient)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _services);

            // Do HandleCommandAsync Method when Bot Received Message
            _discordSocketClient.MessageReceived += HandleCommandAsync;

            // Load Discord Token
            try
            {
                string token = Token.GetToken();
                if (string.IsNullOrEmpty(token)) return;

                await _discordSocketClient.LoginAsync(TokenType.Bot, token);
                await _discordSocketClient.StartAsync();

                // Start Server
                ServerProcessManager.StartServer();

                // Start All Schedules
                Scheduler.StartAll(_discordSocketClient);

                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                LogFile.WriteLine($"[Program] Token Error: {e.Message}");
            }
        }

        private static async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            if (!(message.HasCharPrefix('!', ref argPos)
                || message.HasMentionPrefix(_discordSocketClient.CurrentUser, ref argPos))
                || message.Author.IsBot) return;

            var context = new SocketCommandContext(_discordSocketClient, message);

            var result = await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);

            if (!result.IsSuccess)
            {
                if (result.Error != CommandError.UnknownCommand)
                {
                    LogFile.WriteLine($"[Program] Command prohibited: {context.User.Username}: {result.ErrorReason}");

                    await context.Channel.SendMessageAsync($"🚫 {result.ErrorReason}");
                }
            }
        }

        private static Task DiscordLog(LogMessage msg)
        {
            LogFile.WriteLine($"[Discord] {msg.Message ?? msg.Exception?.Message}");
            return Task.CompletedTask;
        }

    }
}