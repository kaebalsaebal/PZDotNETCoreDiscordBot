using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class DiscordHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _service;
        private readonly ILogFile _logFile;

        public DiscordHandler(
            DiscordSocketClient client,
            CommandService commands,
            InteractionService interactions,
            IServiceProvider services,
            ILogFile logFile)
        {
            _client = client;
            _commandService = commands;
            _interactionService = interactions;
            _service = services;
            _logFile = logFile;
        }

        public async Task Initialize()
        {
            await _commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _service);
            await _interactionService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _service);

            _client.MessageReceived += HandleCommand;
            _client.InteractionCreated += HandleInteraction;
            _interactionService.SlashCommandExecuted += HandleInteractionResult;

            _client.Log += DiscordLogHandler;
            _commandService.Log += DiscordLogHandler;
            _interactionService.Log += DiscordLogHandler;
        }

        private async Task HandleCommand(SocketMessage socketMessage)
        {
            try
            {
                var message = socketMessage as SocketUserMessage;
                if (message == null) return;

                int argPos = 0;

                if (!(message.HasCharPrefix('!', ref argPos)
                    || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                    || message.Author.IsBot) return;

                var context = new SocketCommandContext(_client, message);

                var result = await _commandService.ExecuteAsync(
                    context: context,
                    argPos: argPos,
                    services: _service);

                if (!result.IsSuccess)
                {
                    if (result.Error != CommandError.UnknownCommand)
                    {
                        _logFile.WriteLine(Messages.Get("command_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")), context.Channel.Id);
                    }

                }
            }
            catch (Exception e)
            {
                _logFile.WriteLine(Messages.Get("unknown_error").KeyFormat(("error", e.Message)));
            }
        }

        // Send slash command to discord
        private async Task HandleInteraction(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_client, interaction);

            await _interactionService.ExecuteCommandAsync(
                context: context,
                services: _service);

        }

        // Get slash command result from discord(divided get/set methods because of interaction's async mechanism)
        private async Task HandleInteractionResult(SlashCommandInfo info, IInteractionContext context, Discord.Interactions.IResult result)
        {
            if (result.IsSuccess) return;

            try
            {
                if (result.Error == InteractionCommandError.UnmetPrecondition)
                {
                    await RespondOrFollowup(context.Interaction, Messages.Get("precondition_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")), true);

                    _logFile.WriteLine(Messages.Get("precondition_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")));
                }
                else if (result.Error == InteractionCommandError.Exception)
                {
                    string msg = Messages.Get("exception_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}"));

                    var execResult = result as Discord.Interactions.ExecuteResult?;


                    if (execResult != null && execResult.Value.Exception != null)
                    {
                        msg += $"\n{execResult.Value.Exception.InnerException.Message}";
                    }

                    await RespondOrFollowup(context.Interaction, msg, true);
                    _logFile.WriteLine(msg);
                }
                else if (result.Error == InteractionCommandError.UnknownCommand)
                {
                    await RespondOrFollowup(context.Interaction, Messages.Get("exception_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")), true);

                    _logFile.WriteLine(Messages.Get("command_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")));
                }
                else
                {
                    await RespondOrFollowup(context.Interaction, Messages.Get("interaction_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")), true);

                    _logFile.WriteLine(Messages.Get("unknown_error").KeyFormat(("error", $"{context.User.Username}\n{result.ErrorReason}")));
                }

            }
            catch (Exception e)
            {
                _logFile.WriteLine(Messages.Get("unknown_error").KeyFormat(("error", e.Message)));
            }
        }

        private async Task RespondOrFollowup(IDiscordInteraction interaction, string message, bool ephemeral)
        {
            if (interaction.HasResponded)
            {
                await interaction.FollowupAsync(message, ephemeral: ephemeral);
            }
            else
            {
                await interaction.RespondAsync(message, ephemeral: ephemeral);
            }
        }

        private Task DiscordLogHandler(LogMessage msg)
        {
            _logFile.WriteLine(Messages.Get("discord_log").KeyFormat(("log", $"{msg.Message ?? msg.Exception?.Message}")));

            return Task.CompletedTask;
        }
    }
}
