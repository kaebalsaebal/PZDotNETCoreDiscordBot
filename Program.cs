using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DotNETCoreDiscordBot.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
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
                LogFile.WriteLine(Messages.Get("load_config"));

                if (File.Exists(botConfig.GetConfLocation()))
                {
                    botConfig = JsonConvert.DeserializeObject<BotConfig>(
                                    File.ReadAllText(botConfig.GetConfLocation()),
                                    new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });

                    botConfig.Linuxparams = param;
                }

            } catch(Exception e)
            {
                LogFile.WriteLine(Messages.Get("load_config_error").KeyFormat(("error", e.Message)));
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
            LogFile.WriteLine(Messages.Get("load_command_log"));

            _interactionService = new InteractionService(_discordSocketClient, new InteractionServiceConfig
            {
                LogLevel = LogSeverity.Info,
                DefaultRunMode = Discord.Interactions.RunMode.Async
            });
            _interactionService.Log += DiscordLog;
            LogFile.WriteLine(Messages.Get("load_interaction_log"));

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
            LogFile.WriteLine(Messages.Get("load_services_collection"));

            // Stop Program If Force Stop
            AppDomain.CurrentDomain.ProcessExit += async (sender, eventArgs) =>
            {
                LogFile.WriteLine(Messages.Get("kill_process"));
                var processManager = _services.GetRequiredService<IServerProcessManager>();
                processManager.KillServerProcess();
            };

            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                LogFile.WriteLine(Messages.Get("kill_process"));
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
                        // Handle Slash Command Result
                        _interactionService.SlashCommandExecuted += HandleInteractionResult;

                        // Background Service Run
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await GrantFirstAuth(botConfig);
                                #if !DEBUG
                                await CheckBotInitCondition(botConfig);
                                #endif
                            }
                            catch (Exception e)
                            {
                                LogFile.WriteLine(Messages.Get("init_condition_error").KeyFormat(("error", e.Message)));
                            }
                        });
                    } catch(Exception e)
                    {
                        LogFile.WriteLine(Messages.Get("ready_handler_error").KeyFormat(("error", e.Message)));
                    }

                    await Task.CompletedTask;
                };

                await _discordSocketClient.LoginAsync(TokenType.Bot, token);
                await _discordSocketClient.StartAsync();

                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                LogFile.WriteLine(Messages.Get("discord_token_error").KeyFormat(("error", e.Message)));
            }
        }

        // Grant command interaction authority to bot owner
        public static async Task GrantFirstAuth(BotConfig botConfig)
        {
            var appInfo = await _discordSocketClient.GetApplicationInfoAsync();
            ulong ownerId = appInfo.Owner.Id;

            if (!botConfig.AuthorizedUsers.Contains(ownerId))
            {
                botConfig.AuthorizedUsers.Add(ownerId);
                await botConfig.Save();

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

                LogFile.WriteLine(Messages.Get("init_condition_ready"), botConfig.LogChannelId);

                var serverService = _services.GetRequiredService<IServerServiceManager>();
                var schedulerService = _services.GetRequiredService<ISchedulerService>();
                
                await serverService.StartServer(_discordSocketClient, botConfig.PublicChannelId);
                await schedulerService.StartAll();
            }
            else
            {
                LogFile.WriteLine(Messages.Get("bot_config_incomplete"));

                var bot = await _discordSocketClient.GetApplicationInfoAsync();
                await bot.Owner.SendMessageAsync(Messages.Get("bot_config_incomplete"));
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
                    LogFile.WriteLine(Messages.Get("command_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")), context.Channel.Id);
                }
            }
        }

        // Send slash command to discord
        private static async Task HandleInteraction(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_discordSocketClient, interaction);

            await _interactionService.ExecuteCommandAsync(
                context: context,
                services: _services);

        }

        // Get slash command result from discord(divided get/set methods because of interaction's async mechanism)
        private static async Task HandleInteractionResult(SlashCommandInfo info, IInteractionContext context, Discord.Interactions.IResult result)
        {
            if (result.IsSuccess) return;

            try
            {
                if (result.Error == InteractionCommandError.UnmetPrecondition)
                {
                    await RespondOrFollowup(context.Interaction, Messages.Get("precondition_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")));

                    LogFile.WriteLine(Messages.Get("precondition_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")));
                }
                if (result.Error == InteractionCommandError.Exception)
                {
                    await RespondOrFollowup(context.Interaction, Messages.Get("exception_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")));

                    LogFile.WriteLine(Messages.Get("exception_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")));
                }
                if (result.Error != InteractionCommandError.UnknownCommand)
                {
                    await RespondOrFollowup(context.Interaction, Messages.Get("interaction_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")));

                    LogFile.WriteLine(Messages.Get("interaction_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")));
                }

            } catch(Exception e)
            {
                LogFile.WriteLine(Messages.Get("unknown_error").KeyFormat(("error", e.Message)));
            }

            
        }
        private static async Task RespondOrFollowup(IDiscordInteraction interaction, string message)
        {
            if (interaction.HasResponded)
            {
                await interaction.FollowupAsync(message, ephemeral: true);
            }
            else
            {
                await interaction.RespondAsync(message, ephemeral: true);
            }
        }

        private static async Task DiscordLog(LogMessage msg)
        {
            LogFile.WriteLine(Messages.Get("discord_log").KeyFormat(("log", $"{msg.Message ?? msg.Exception?.Message}")));
        }

    }
}