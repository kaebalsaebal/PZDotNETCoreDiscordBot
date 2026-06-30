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

        public CommandSlashCommands(IServerServiceManager serverService, BotConfig botConfig)
        {
            _serverService = serverService;
            _botConfig = botConfig;
        }

        [SlashCommand("ping", "Do you like watching me")]
        public async Task Ping()
        {
            await RespondAsync("☠PONG☠", ephemeral: true);
        }

        [SlashCommand("set_configs", "Configures other settings")]
        public async Task SetConfigs()
        {
            var current = _botConfig;

            var modal = new BotSetupModal
            {
                SaveAsFile = current.SaveAsFile.ToString().ToLower(),
                RestartTimer = current.ServerScheduleSettings.RestartTimer.ToString(),
                WorkshopInterval = current.ServerScheduleSettings.WorkshopItemUpdateSchedule.ToString(),
            };

            await RespondWithModalAsync<BotSetupModal>("set_configs", modal);
        }
        [ModalInteraction("set_configs")]
        public async Task OnModalSubmit(BotSetupModal modal)
        {
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

            if (uint.TryParse(modal.WorkshopInterval, out uint parsedInterval))
            {
                _botConfig.ServerScheduleSettings.WorkshopItemUpdateSchedule = parsedInterval;
            }
            else
            {
                await RespondAsync(Messages.Get("slash_modal_value_error3"), ephemeral: true);
                return;
            }

            await _botConfig.Save();
            await RespondAsync(Messages.Get("slash_modal_updated"), ephemeral: true);
        }

        [SlashCommand("save_server", "Saves Server")]
        public async Task Save()
        {

            await DeferAsync();

            await _serverService.SaveServer(Context.Client, _botConfig.LogChannelId);

            await FollowupAsync("Save completed...", ephemeral: true);
        }

        [SlashCommand("restart_server", "Restarts server after n minutes")]
        public async Task RestartServer([Summary("minutes", "Restarts server after n minutes")] uint minutes)
        {
            await DeferAsync();

            _ = Task.Run(async () =>
            {
                await _serverService.RestartServer(
                    Context.Client,
                    _botConfig.LogChannelId,
                    minutes * 60000);
            });

            await FollowupAsync(Messages.Get("slash_restart_server").KeyFormat(("minutes", minutes)), ephemeral: true);
        }

        [SlashCommand("restart_cancel", "Cancel scheduled restart")]
        public async Task CancelRestart()
        {
            await DeferAsync();

            bool isCancelled = _serverService.CancelRestart(Context.Client, _botConfig.LogChannelId);

            if (isCancelled)
            {
                await FollowupAsync(Messages.Get("slash_restart_canceled"), ephemeral: true);
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

            await FollowupAsync(Messages.Get("slash_shutdown_server"), ephemeral: true);
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
            await _botConfig.Save();

            await RespondAsync(Messages.Get("slash_grant").KeyFormat(("user", $"**{user.Username}**")), ephemeral: true);
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
            await _botConfig.Save();

            await RespondAsync(Messages.Get("slash_revoke").KeyFormat(("user", $"**{user.Username}**")), ephemeral: true);
        }

        [SlashCommand("show_auth", "Show granted users who can use commands")]
        public async Task ShowAuth()
        {
            await DeferAsync();

            var sb = new StringBuilder();
            sb.AppendLine(Messages.Get("slash_show_granted_title"));

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

        [SlashCommand("get_cpu_ram", "Get server's cpu and ram usage")]
        public async Task GetUsage()
        {
            await DeferAsync();

            double[] result = await _serverService.GetUsage();

            var sb = new StringBuilder();
            sb.AppendLine(Messages.Get("slash_get_usage_title"));
            sb.AppendLine("```");
            sb.AppendLine($"CPU: {String.Format("{0:N2}", result[0])}%");
            sb.AppendLine($"RAM: {String.Format("{0:N2}", result[1])}%");
            sb.AppendLine("```");

            await FollowupAsync(sb.ToString(), ephemeral: true);
        }
    }
}
