using Discord;
using Discord.Commands;
using Discord.Interactions;
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
        private static InteractionService _interactionService;
        private static IServiceProvider _services;

        public static BotConfig BotConfig { get; set; } = new BotConfig();
        private static bool _servicesStarted = false;
        public static void Main(string[] _) => MainAsync(_).GetAwaiter().GetResult();

        private static async Task MainAsync(string[] param)
        {
            // Stop Zomboid Dedi if bot process down
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                ServerProcessManager.KillServerProcess();
            };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                ServerProcessManager.KillServerProcess();
                eventArgs.Cancel = false;
            };

            // parse servername if linux
            for (int i = 0; i < param.Length; i++)
            {
                if (param[i].ToLower() == "-servername" && i + 1 < param.Length)
                {
                    string servername = param[i + 1].Replace("\"", "").Trim();

                    if (!string.IsNullOrEmpty(servername))
                    {
                        BotConfig.ServerLocationSettings.ServerName = servername;
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

            _interactionService = new InteractionService(_discordSocketClient, new InteractionServiceConfig
            {
                LogLevel = LogSeverity.Info,
                DefaultRunMode = Discord.Interactions.RunMode.Async
            });
            _interactionService.Log += DiscordLog;

            _services = new ServiceCollection()
                .AddSingleton(_discordSocketClient)
                .AddSingleton(_commands)
                .AddSingleton(_interactionService)
                .BuildServiceProvider();

            _interactionService = _services.GetRequiredService<InteractionService>();

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _services);
            await _interactionService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _services);

            // Handle Command/Slash Command
            _discordSocketClient.MessageReceived += HandleCommand;
            _discordSocketClient.InteractionCreated += HandleInteraction;

            // Load Discord Token
            try
            {
                string token = Token.GetToken();
                if (string.IsNullOrEmpty(token)) return;

                // Start If discordSocketClient has been logined and started
                _discordSocketClient.Ready += async () =>
                {
                    // add Interaction Service to All Guilds
                    await _interactionService.RegisterCommandsGloballyAsync();
                    LogFile.WriteLine("[Program] Interaction Command has been applied...");

                    // Init bot conf if conf file not exists
                    if (File.Exists(BotConfig.SettingsFile))
                    {
                        BotConfig = JsonConvert.DeserializeObject<BotConfig>(
                                        File.ReadAllText(BotConfig.SettingsFile),
                                        new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
                    }

                    await StartBotService();
                };

                await _discordSocketClient.LoginAsync(TokenType.Bot, token);
                await _discordSocketClient.StartAsync();

                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                LogFile.WriteLine($"[Program] Token Error: {e.Message}");
            }
        }

        public static async Task StartBotService()
        {
            if (BotConfig.PublicChannelId != 0 &&
                BotConfig.CommandChannelId != 0 &&
                BotConfig.LogChannelId != 0)
            {
                if (_servicesStarted) return;
                _servicesStarted = true;

                LogFile.WriteLine("[Program] All channels configured! Starting Server and Scheduler...");

                await ServerServiceManager.StartServer(_discordSocketClient, BotConfig.PublicChannelId);
                Scheduler.StartAll(_discordSocketClient);
            }
            else
            {
                LogFile.WriteLine("[Program] Config File Incomplete or Not Found. Waiting for setup commands...");

                var bot = await _discordSocketClient.GetApplicationInfoAsync();
                await bot.Owner.SendMessageAsync("✨Bot Config Not Found or Incomplete. Run \n `/set_public_channel`, \n `/set_command_channel`, \n `/set_log_channel` \n Command In Your Server✨");
            }
        }
        private static async Task HandleCommand(SocketMessage socketMessage)
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
                    LogFile.WriteLine($"[Program] Command Error: {context.User.Username}: {result.ErrorReason}");

                    await context.Channel.SendMessageAsync($"🚫 Command Error: {result.ErrorReason}");
                }
            }
        }

        private static async Task HandleInteraction(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_discordSocketClient, interaction);

            var result = await _interactionService.ExecuteCommandAsync(
                context: context,
                services: _services);

            if (!result.IsSuccess)
            {
                if (result.Error != InteractionCommandError.UnknownCommand)
                {
                    LogFile.WriteLine($"[Program] Slash Command Error: {context.User.Username}: {result.ErrorReason}");

                    await interaction.RespondAsync($"🚫 Slash Command Error: {result.ErrorReason}", ephemeral: true);
                }
            }
        }

        private static async Task DiscordLog(LogMessage msg)
        {
            LogFile.WriteLine($"[Discord] {msg.Message ?? msg.Exception?.Message}");
        }

    }
}