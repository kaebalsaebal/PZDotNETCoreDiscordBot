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

        private static bool _botReady = false;

        public static void Main(string[] _) => MainAsync(_).GetAwaiter().GetResult();
        private static async Task MainAsync(string[] param)
        {
            BotConfig botConfig = new BotConfig();
            botConfig.Linuxparams = param;

            // Initialize discord components
            var discordConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };
            var client = new DiscordSocketClient(discordConfig);

            var commandService = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false
            });
            var interactionService = new InteractionService(client, new InteractionServiceConfig
            {
                LogLevel = LogSeverity.Info,
                DefaultRunMode = Discord.Interactions.RunMode.Async
            });

            // Add instances to ServiceCollection
            var servicesCollection = new ServiceCollection();

            // discord
            servicesCollection.AddSingleton(client);
            servicesCollection.AddSingleton(commandService);
            servicesCollection.AddSingleton(interactionService);
            servicesCollection.AddSingleton<DiscordHandler>();

            // config, log
            servicesCollection.AddSingleton(botConfig);
            servicesCollection.AddSingleton<ILogFile, LogFile>();

            // process
            servicesCollection.AddSingleton<IServerProcessManager, ServerProcessManager>();
            servicesCollection.AddSingleton<IServerServiceManager, ServerServiceManager>();

            // scheduler
            servicesCollection.AddTransient<IScheduledJob, WorkshopUpdateScheduledJob>();
            servicesCollection.AddTransient<IScheduledJob, BotUpdateScheduledJob>();
            servicesCollection.AddSingleton<ISchedulerService, SchedulerService>();

            // utils
            servicesCollection.AddHttpClient<IWebAPIManager, WebAPIManager>();
            servicesCollection.AddSingleton<IRconManager, RconManager>();

            var service = servicesCollection.BuildServiceProvider();
            ILogFile logFile = service.GetRequiredService<ILogFile>();

            // Load Config File
            try
            {
                logFile.WriteLine(Messages.Get("load_config"));

                if (File.Exists(botConfig.GetConfLocation()))
                {
                    /*
                    botConfig = JsonConvert.DeserializeObject<BotConfig>(
                                    File.ReadAllText(botConfig.GetConfLocation()),
                                    new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
                    */
                    JsonConvert.PopulateObject(File.ReadAllText(botConfig.GetConfLocation()), botConfig);
                }

                // Get Localization
                if (botConfig.Language != null)
                {
                    Messages.SetLanguage(botConfig.Language);
                    logFile.WriteLine(Messages.Get("translations_language_set").KeyFormat(("lang", Messages.TranslatedMessages["language_name"])));
                }
                else
                {
                    logFile.WriteLine(Messages.Get("translations_using_default"));
                }

                // Update translation files from repository
                logFile.WriteLine(Messages.Get("update_translations"));
                var webAPIManager = service.GetRequiredService<IWebAPIManager>();
                await webAPIManager.UpdateTranslations();

                Messages.MakeMetadata();

            }
            catch (Exception e)
            {
                logFile.WriteLine(Messages.Get("load_config_error").KeyFormat(("error", e.Message)));
                return;
            }
            logFile.WriteLine(Messages.Get("load_services_collection"));

            var commandHandler = service.GetRequiredService<DiscordHandler>();
            await commandHandler.Initialize();
            logFile.WriteLine(Messages.Get("discord_service_init"));

            // Load Discord Token
            try
            {
                string token = new DiscordToken().GetToken();
                if (string.IsNullOrEmpty(token)) return;

                // Start If discordSocketClient has been logined and started
                client.Ready += async () =>
                {
                    try
                    {
                        // add Interaction Service to All Guilds
                        await interactionService.RegisterCommandsGloballyAsync();

                        try
                        {
                            await GrantFirstAuth(botConfig, client, logFile);
                            #if !DEBUG
                            await CheckBotInitCondition(botConfig, service, client, logFile);
                            #endif
                        }
                        catch (Exception e)
                        {
                            logFile.WriteLine(Messages.Get("init_condition_error").KeyFormat(("error", e.Message)));
                        }

                    } catch(Exception e)
                    {
                        logFile.WriteLine(Messages.Get("ready_handler_error").KeyFormat(("error", e.Message)));
                    }

                    await Task.CompletedTask;
                };

                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();

            }
            catch (Exception e)
            {
                logFile.WriteLine(Messages.Get("discord_token_error").KeyFormat(("error", e.Message)));
            }

            // Stop Program If Force Stop
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                logFile.WriteLine(Messages.Get("kill_process"));
                var processManager = service.GetRequiredService<IServerProcessManager>();
                processManager.KillServerProcess();
            };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                logFile.WriteLine(Messages.Get("kill_process"));
                var processManager = service.GetRequiredService<IServerProcessManager>();
                processManager.KillServerProcess();
                eventArgs.Cancel = false;
            };

            await Task.Delay(-1);
        }

        // Grant command interaction authority to bot owner
        public static async Task GrantFirstAuth(BotConfig botConfig, DiscordSocketClient client, ILogFile logFile)
        {
            var appInfo = await client.GetApplicationInfoAsync();
            ulong ownerId = appInfo.Owner.Id;

            if (!botConfig.AuthorizedUsers.Contains(ownerId))
            {
                botConfig.AuthorizedUsers.Add(ownerId);
                await botConfig.Save(logFile);

            }
        }

        public static async Task CheckBotInitCondition(BotConfig botConfig, IServiceProvider service, DiscordSocketClient client, ILogFile logFile)
        {
            if (botConfig.PublicChannelId != 0 &&
                botConfig.CommandChannelId != 0 &&
                botConfig.LogChannelId != 0)
            {
                if (_botReady) return;
                _botReady = true;

                logFile.WriteLine(Messages.Get("init_condition_ready"), botConfig.LogChannelId);

                var serverService = service.GetRequiredService<IServerServiceManager>();
                var schedulerService = service.GetRequiredService<ISchedulerService>();

                await serverService.StartServer(client, botConfig.PublicChannelId);
                await schedulerService.StartAll();
            }
            else
            {
                logFile.WriteLine(Messages.Get("bot_config_incomplete"));

                var bot = await client.GetApplicationInfoAsync();
                await bot.Owner.SendMessageAsync(Messages.Get("bot_config_incomplete"));
            }
        }
    }
}