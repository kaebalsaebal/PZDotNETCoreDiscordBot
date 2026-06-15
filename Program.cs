using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DotNETCoreDiscordBot.Scheduler;
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
        private static CommandService _commandService;
        private static InteractionService _interactionService;
        private static IServiceProvider _services;

        private static bool _botReady = false;

        public static void Main(string[] _) => MainAsync(_).GetAwaiter().GetResult();
        private static async Task MainAsync(string[] param)
        {
            var botConfig = new BotConfig();
            botConfig.Linuxparams = param;

            // Load Config File
            try
            {
                await LogFile.WriteLine($"[Program] Loading Config File...");

                if (File.Exists(botConfig.GetConfLocation()))
                {
                    botConfig = JsonConvert.DeserializeObject<BotConfig>(
                                    File.ReadAllText(botConfig.GetConfLocation()),
                                    new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });

                    botConfig.Linuxparams = param;
                }

            } catch(Exception e)
            {
                await LogFile.WriteLine($"[Program] Config File Load Error: {e.Message}");
            }


            // Initialise Discord Client
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            _discordSocketClient = new DiscordSocketClient(config);
            _discordSocketClient.Log += DiscordLog;

            _commandService = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false
            });
            _commandService.Log += DiscordLog;

            _interactionService = new InteractionService(_discordSocketClient, new InteractionServiceConfig
            {
                LogLevel = LogSeverity.Info,
                DefaultRunMode = Discord.Interactions.RunMode.Async
            });
            _interactionService.Log += DiscordLog;

            // Add Services to do Singleton
            var servicesCollection = new ServiceCollection();

            // discord
            servicesCollection.AddSingleton(_discordSocketClient);
            servicesCollection.AddSingleton(_commandService);
            servicesCollection.AddSingleton(_interactionService);

            // process
            servicesCollection.AddSingleton<BotConfig>(botConfig);
            servicesCollection.AddSingleton<IServerProcessManager, ServerProcessManager>();
            servicesCollection.AddSingleton<IServerServiceManager, ServerServiceManager>();

            // scheduler
            servicesCollection.AddTransient<IScheduledJob, WorkshopUpdateScheduledJob>();
            servicesCollection.AddSingleton<ISchedulerService, SchedulerService>();

            // utils
            servicesCollection.AddHttpClient<ISteamWebAPI, SteamWebAPI>();
            servicesCollection.AddSingleton<IRconManager, RconManager>();

            _services = servicesCollection.BuildServiceProvider();
            LogFile.LoadService(_services);

            // Stop Program If Force Stop
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                var processManager = _services.GetRequiredService<IServerProcessManager>();
                processManager.KillServerProcess();
            };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                var processManager = _services.GetRequiredService<IServerProcessManager>();
                processManager.KillServerProcess();
                eventArgs.Cancel = false;
            };

            _commandService = _services.GetRequiredService<CommandService>();
            _interactionService = _services.GetRequiredService<InteractionService>();

            await _commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _services);
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
                    try
                    {
                        // add Interaction Service to All Guilds
                        await _interactionService.RegisterCommandsGloballyAsync();
                        await LogFile.WriteLine("[Program] Interaction Command has been applied...");

                        // Background Service Run
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await CheckBotInitCondition(botConfig);
                            }
                            catch (Exception e)
                            {
                                await LogFile.WriteLine($"[Program] Check Bot Init Condition Error: {e.Message}");
                            }
                        });
                    } catch(Exception e)
                    {
                        await LogFile.WriteLine($"[Program] Discord Ready Handler Error: {e.Message}");
                    }

                    await Task.CompletedTask;
                };

                await _discordSocketClient.LoginAsync(TokenType.Bot, token);
                await _discordSocketClient.StartAsync();

                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                await LogFile.WriteLine($"[Program] Token Error: {e.Message}");
            }
        }

        public static async Task CheckBotInitCondition(BotConfig botConfig)
        {
            if (botConfig.PublicChannelId != 0 &&
                botConfig.CommandChannelId != 0 &&
                botConfig.LogChannelId != 0)
            {
                if (_botReady) return;
                _botReady = true;

                await LogFile.WriteLine("[Program] Bot is Ready! Starting Server and Scheduler...", botConfig.LogChannelId);

                var serverService = _services.GetRequiredService<IServerServiceManager>();
                var schedulerService = _services.GetRequiredService<ISchedulerService>();
                
                await serverService.StartServer(_discordSocketClient, botConfig.PublicChannelId);
                await schedulerService.StartAll();
            }
            else
            {
                await LogFile.WriteLine("[Program] Config File Incomplete or Not Found. Waiting for setup commands...");

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

            var result = await _commandService.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);

            if (!result.IsSuccess)
            {
                if (result.Error != CommandError.UnknownCommand)
                {
                    await LogFile.WriteLine($"[Program] Command Error: {context.User.Username}: {result.ErrorReason}");

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
                    await LogFile.WriteLine($"[Program] Slash Command Error: {context.User.Username}: {result.ErrorReason}");

                    await interaction.RespondAsync($"🚫 Slash Command Error: {result.ErrorReason}", ephemeral: true);
                }
            }
        }

        private static async Task DiscordLog(LogMessage msg)
        {
            await LogFile.WriteLine($"[Discord] {msg.Message ?? msg.Exception?.Message}");
        }

    }
}