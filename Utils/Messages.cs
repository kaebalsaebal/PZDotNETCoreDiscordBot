using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{

    public static class Messages
    {
        public static Dictionary<string, string>? TranslatedMessages = null;
        public static Dictionary<string, string>? TranslationMetadata = null;

        private static readonly string _folderPath = Path.Combine(AppContext.BaseDirectory, "PZBot_Translations");

        private static readonly Dictionary<string, string> _defaultMessages = new Dictionary<string, string>
        {
            {"language_name", "English(United States)" },
            {"date_format", "MM/dd/yy" },

            // Program
            {"update_translations", "[Program] Updating translation file..." },
            {"load_config", "[Program] Loading config file..." },
            {"load_config_error", "[Program] Config file load error: {error}" },
            {"discord_service_init", "[Program] Initialized discord client components"},
            {"load_services_collection", "[Program] Loaded services collection" },
            {"kill_process", "[Program] Process has been killed" },
            {"init_condition_error", "[Program] Check bot initialize condition error: {error}" },
            {"ready_handler_error", "[Program] Discord ready handler error: {error}" },
            {"discord_token_error", "[Program] Discord token error: {error}" },
            {"init_condition_ready", "[Program] Bot is ready! Starting server process and schedulers..." },
            {"bot_config_incomplete", "✨Bot config incomplete. Run \n `/set_public_channel`, \n `/set_command_channel`, \n `/set_log_channel` \n command in your discord server✨" },
            {"command_error", "🚫 Command error: {error}" },
            {"precondition_error", "🚫 Precondition error: {error}" },
            {"exception_error", "🚫 Exception error: {error}" },
            {"unknown_error", "🚫 unknown error: {error}" },
            {"discord_log", "[Discord] {log}" },

            // ServerServiceManager
            {"server_started_notification", "@everyone 🔥 Server Started!!!"},
            {"save_server_notification", "💾 Saving server..."},
            {"server_saved_notification", "💾 Saving finished"},
            {"restart_config_error", "[ServerServiceManager] Error: RestartTime is not configured"},
            {"restart_server_notification", "[ServerServiceManager] Restarting server in {minutes} minutes"},
            {"restart_server_notification_rcon", "Server will restart in {minutes} minute(s). Please find a safe place."},
            {"restart_server", "[ServerServiceManager] Restarting server. Wait patiently" },
            {"restart_server_canceled", "[ServerServiceManager] Restart task has been canceled"},
            {"restart_server_canceled_rcon", "The scheduled server restart has been canceled." },
            {"shutdown_server", "[ServerServiceManager] Shutting down server"},

            // ServerProcessManager
            {"server_already_started", "[ServerProcessManager] Server already started"},
            {"servername_configured", "[ServerProcessManager] Servername has been configured: {servername}" },
            {"process_manager_error", "[ServerProcessManager] Error: {error}" },

            // ServerProcess
            {"parse_script_modify", "[ServerProcess] Removed pause/read in Server Script File" },
            {"custom_location_found", "[ServerProcess] Custom Location Found: {location}" },

            //PublicSlashCommands
            {"help_title", "📋[PZDotNETCoreDiscordBot Commands List]📋"},
            {"help_parameters", "Parameters:"},
            {"help_public_commands", "Public commands: Anyone can use them in Public/Command channels"},
            {"help_authed_commands", "Admin commands: Only authorized users can use them in the Command channel."},

            //CommandSlashCommands
            {"slash_modal_value_error1", "SaveAsFile value must be 'true' or 'false'"},
            {"slash_modal_value_error2", "RestartTimer value must consist of digits" },
            {"slash_modal_value_error3", "Interval value must consist of digits" },
            {"slash_modal_updated", "Config has been Updated" },

            {"slash_grant_already_exists","{user} has already been granted."},
            {"slash_grant", "Granted command permission to {user}."},
            {"slash_revoke_owner", "Revoking bot owner is not allowed." },
            {"slash_revoke_already_exists", "```{user}``` has already been revoked." },
            {"slash_revoke", "Revoked command permission to {user}" },
            {"slash_show_granted_title","✔️[Command Channel Granted User List]✔️" },

            {"slash_restart_server", "Restarting server after {minutes} minutes..." },
            {"slash_restart_canceled", "Scheduled restart canceled"},
            {"slash_no_restart", "There is no scheduled restart." },
            {"slash_shutdown_server", "Server has shut down." },

            {"slash_get_usage_title", "💻[CPU and Memory Current Usage]🖥️"},

            {"slash_workshop_file_failed", "Failed to Get {config} File..."},
            {"slash_workshop_api_failed", "Failed to get workshop mods info by SteamAPI..." },
            {"slash_workshop_no_mods", "There are no mods on your server." },
            {"slash_workshop_title", "💎[Workshop Mods in {servername} - Total {count}]💎" },

            {"slash_server_msg", "Sent message to server: {msg}"},

            //ChannelSlashCommands
            {"slash_public_channel", "Set Public Channel to {channel}"},
            {"slash_command_channel", "Set Command Channel to {channel}"},
            {"slash_log_channel", "Set Log Channel to {channel}"},

            //Preconditions
            {"auth_user_only", "This command is limited to authorized users" },
            {"command_channel_only", "This command is limited to command channel" },
            {"public_channel_only", "This command is limited to public channel" },

            //SchedulerService
            {"init_scheduler", "[SchedulerService] Initializing background schedules..." },
            {"stop_scheduler", "[SchedulerService] All schedules have been canceled" },

            //WorkshopUpdateScheduledJob
            {"workshop_scheduler_running", "[Workshop Update Check Scheduler] Running..." },
            {"workshop_scheduler_update_found", "[Workshop Update Check Scheduler] Mod update found!!! ({mods})" },
            {"workshop_scheduler_stop", "[Workshop Update Check Scheduler] Scheduler has been canceled." },
            {"workshop_scheduler_error", "[Workshop Update Check Scheduler] Error: {error}" },

            //BotUpdateScheduledJob
            {"bot_scheduler_running", "[Bot Update Check Scheduler] Running..." },
            {"bot_scheduler_update_found", "[Bot Update Check Scheduler] Bot update found!!! (Version: {version}, Release: {name})\nYou'd better get a new version at {url}" },
            {"bot_scheduler_stop", "[Bot Update Check Scheduler] Scheduler has been canceled." },
            {"bot_scheduler_error", "[Bot Update Check Scheduler] Error: {error}" },

            //LogFile
            {"logfile_error", "[LogFile] Error: {error}"},

            //RconManager
            {"rcon_error",  "[RconManager] Error: {error}"},

            //WebAPIManager
            {"web_api_error", "[WebAPIManager] Error: {error}"},

            //tokenfile
            {"token_not_found", "Token not found. Make sure to locate the bot_token.txt file in the game directory."},
            {"token_read_failed", "Failed to read bot_token.txt file."},

            //Translations
            {"translations_language_set", "[Translations] Bot language is now: {lang}" },
            {"translations_using_default", "[Translations] Failed to load language file. Using built-in English data..."},
            {"translations_language_unavilable", "[Translations] This language is not supported: {lang}" }
        };

        public static string GetLocation()
        {
            return _folderPath;
        }

        public static void MakeMetadata()
        {
            TranslationMetadata = new Dictionary<string, string>();

            if (Directory.Exists(_folderPath))
            {
                List<string> jsonFiles = Directory.GetFiles(_folderPath, "*.json", SearchOption.TopDirectoryOnly).ToList();

                foreach(string jsonFile in jsonFiles)
                {
                    string jsonString = File.ReadAllText(jsonFile);
                    JObject parsedJson = JObject.Parse(jsonString);

                    if (parsedJson["language_name"] != null && Path.GetFileName(jsonFile) != null)
                    {
                        TranslationMetadata[Path.GetFileName(jsonFile).ToString()] = parsedJson["language_name"].ToString();
                    }
                }
            }

        }

        public static void SetLanguage(string langCode)
        {
            TranslatedMessages = new Dictionary<string, string>();

            string filePath = Path.Combine(_folderPath, $"{langCode}.json");

            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                JObject parsedJson = JObject.Parse(jsonString);
                TranslatedMessages = parsedJson.ToObject<Dictionary<string, string>>();
            }
        }

        public static string Get(string key)
        {
            if (TranslatedMessages != null && TranslatedMessages.ContainsKey(key))
            {
                return TranslatedMessages[key];
            }
            else if (_defaultMessages.ContainsKey(key))
            {
                return _defaultMessages[key];
            }
            else {
                return $"[Translations] Message not exists: {key}";
            }
        }

        public static string KeyFormat(this string str, params (string, object)[] formatPair)
        {
            if (formatPair.Length < 1) return str;

            foreach ((string, object) pair in formatPair)
            {

                str = str.Replace("{" + pair.Item1 + "}", pair.Item2.ToString());
            }

            return str;
        }
    }
}
