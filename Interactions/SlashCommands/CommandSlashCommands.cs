using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    [RequireAuthorizedUser, RequireCommandChannel]
    public class CommandSlashCommands : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly IServerServiceManager _serverService;
        private readonly BotConfig _botConfig;
        private readonly ILogFile _logFile;

        public CommandSlashCommands(IServerServiceManager serverService, BotConfig botConfig, ILogFile logFile)
        {
            _serverService = serverService;
            _botConfig = botConfig;
            _logFile = logFile;
        }

        [SlashCommand("ping", "Do You Like Watching Me")]
        public async Task Ping()
        {
            await RespondAsync("☠PONG☠", ephemeral: true);
        }

        [SlashCommand("set_configs", "Configures other settings")]
        public async Task SetConfigs()
        {

            var curConf = _botConfig;

            var modal = new BotSetupModal
            {
                SaveAsFile = curConf.SaveAsFile.ToString().ToLower(),
                RestartTimer = curConf.ServerScheduleSettings.RestartTimer.ToString(),
                WorkshopUpdateInterval = curConf.ServerScheduleSettings.WorkshopUpdateSchedule.ToString(),
				BotUpdateInterval = curConf.ServerScheduleSettings.BotUpdateSchedule.ToString()
			};

            await RespondWithModalAsync<BotSetupModal>("set_configs_modal_submit", modal);
        }
        [ModalInteraction("set_configs_modal_submit")]
        public async Task OnModalSubmit(BotSetupModal modal)
        {
            await DeferAsync();

            if (bool.TryParse(modal.SaveAsFile.Trim(), out bool parsedSaveAsFile))
            {
                _botConfig.SaveAsFile = parsedSaveAsFile;
            }
            else
            {
                await RespondAsync(Messages.Get("slash_modal_value_error1"), ephemeral: true);
                return;
            }

            if (uint.TryParse(modal.RestartTimer, out uint parsedTimer))
            {
                _botConfig.ServerScheduleSettings.RestartTimer = parsedTimer;
            }
            else
            {
                await RespondAsync(Messages.Get("slash_modal_value_error2"), ephemeral: true);
                return;
            }

            if (uint.TryParse(modal.WorkshopUpdateInterval, out uint parsedWorkshopUpdateInterval))
            {
                _botConfig.ServerScheduleSettings.WorkshopUpdateSchedule = parsedWorkshopUpdateInterval;
            }
            else
            {
                await RespondAsync(Messages.Get("slash_modal_value_error3"), ephemeral: true);
                return;
            }

			if (uint.TryParse(modal.BotUpdateInterval, out uint parsedBotUpdateInterval))
			{
				_botConfig.ServerScheduleSettings.BotUpdateSchedule = parsedBotUpdateInterval;
			}
			else
			{
				await RespondAsync(Messages.Get("slash_modal_value_error3"), ephemeral: true);
				return;
			}

			await _botConfig.Save(_logFile);
            await FollowupAsync(Messages.Get("slash_modal_updated"), ephemeral: false);
        }

        [SlashCommand("save_server", "Saves Server")]
        public async Task Save()
        {

            await DeferAsync();

            await _serverService.SaveServer(Context.Client, _botConfig.LogChannelId);

            await FollowupAsync("Save completed...", ephemeral: false);
        }

        [SlashCommand("restart_server", "Restarts server after n minutes")]
        public async Task RestartServer(uint minutes)
        {
            await DeferAsync();

            _ = Task.Run(async () =>
            {
                await _serverService.RestartServer(
                    Context.Client,
                    _botConfig.LogChannelId,
                    minutes * 60000);
            });

            await FollowupAsync(Messages.Get("slash_restart_server").KeyFormat(("minutes", minutes)), ephemeral: false);
        }

        [SlashCommand("restart_cancel", "Cancel scheduled restart")]
        public async Task CancelRestart()
        {
            await DeferAsync();

            bool isCancelled = _serverService.CancelRestart(Context.Client, _botConfig.LogChannelId);

            if (isCancelled)
            {
                await FollowupAsync(Messages.Get("slash_restart_canceled"), ephemeral: false);
            }
            else
            {
                await FollowupAsync(Messages.Get("slash_no_restart"), ephemeral: true);
            }
        }

        [SlashCommand("shutdown_server", "Shuts down server immediately")]
        public async Task ShutdownServer()
        {
            await DeferAsync();

            await _serverService.ShutdownServer(Context.Client, _botConfig.LogChannelId);

            await FollowupAsync(Messages.Get("slash_shutdown_server"), ephemeral: false);
        }

        [SlashCommand("grant_auth", "Grant user permission to command")]
        public async Task GrantAuth(IUser user)
        {
            if (_botConfig.AuthorizedUsers.Contains(user.Id))
            {
                await RespondAsync(Messages.Get("slash_grant_already_exists").KeyFormat(("user", $"**{user.Username}**")), ephemeral: true);
                return;
            }

            _botConfig.AuthorizedUsers.Add(user.Id);
            await _botConfig.Save(_logFile);

            await RespondAsync(Messages.Get("slash_grant").KeyFormat(("user", $"**{user.Username}**")), ephemeral: false);
        }

        [SlashCommand("revoke_auth", "Revoke user permission to command")]
        public async Task RevokeAuth(IUser user)
        {
            var appInfo = await Context.Client.GetApplicationInfoAsync();
            if (user.Id == appInfo.Owner.Id)
            {
                await RespondAsync(Messages.Get("slash_revoke_owner"), ephemeral: true);
                return;
            }

            if (!_botConfig.AuthorizedUsers.Contains(user.Id))
            {
                await RespondAsync(Messages.Get("slash_revoke_already_exists").KeyFormat(("user", $"**{user.Username}**")), ephemeral: true);
                return;
            }

            _botConfig.AuthorizedUsers.Remove(user.Id);
            await _botConfig.Save(_logFile);

            await RespondAsync(Messages.Get("slash_revoke").KeyFormat(("user", $"**{user.Username}**")), ephemeral: false);
        }

        [SlashCommand("show_auth", "Show granted users who can use commands")]
        public async Task ShowAuth()
        {
            await DeferAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"**{Messages.Get("slash_show_granted_title")}**");

            sb.AppendLine("```");
            foreach (ulong id in _botConfig.AuthorizedUsers)
            {
                // Get user data from bot cache
                IUser user = Context.Client.GetUser(id);

                // If not, get user by rest api
                if (user == null)
                {
                    user = await Context.Client.Rest.GetUserAsync(id);
                }
                sb.AppendLine($"{user} ({id})");
            }
            sb.AppendLine("```");

            await FollowupAsync(sb.ToString(), ephemeral: true);
        }

        [SlashCommand("server_msg", "Send global message in server")]
        public async Task ServerMsg(string msg)
        {
            await DeferAsync();

            await _serverService.ServerMsg(msg);

            await FollowupAsync(Messages.Get("slash_server_msg").KeyFormat(("msg", msg)), ephemeral: true);
        }

        [SlashCommand("set_language", "Set language")]
        public async Task SetLanguage([Autocomplete(typeof(SetLanguageAutocompleteHandler))] string langCode)
        {
            if (Messages.TranslationMetadata[langCode] == null)
            {
                await RespondAsync(Messages.Get("translations_language_unavilable").KeyFormat(("lang", Messages.TranslatedMessages["language_name"])), ephemeral: true);
                return;
            }

            _botConfig.Language = langCode;
            await _botConfig.Save(_logFile);

            Messages.SetLanguage(langCode);

            await RespondAsync(Messages.Get("translations_language_set").KeyFormat(("lang", Messages.TranslatedMessages["language_name"])), ephemeral: false);
        }
    }
}
