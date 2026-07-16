using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class SetLanguageAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            string userInput = autocompleteInteraction.Data.Current.Value.ToString();

            IEnumerable<AutocompleteResult> results = Messages.TranslationMetadata
                .Where(lang => lang.Key.StartsWith(userInput, StringComparison.OrdinalIgnoreCase))
                .Select(lang => new AutocompleteResult(lang.Value, lang.Key))
                .Take(25);

            return AutocompletionResult.FromSuccess(results);
        }
    }
}
