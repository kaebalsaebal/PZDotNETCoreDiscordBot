using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class BotSetupModal : IModal
    {

        public string Title => "Config Settings Menu";

        [InputLabel("Save Logs As File??? (true / false)")]
        [ModalTextInput("save_as_file", TextInputStyle.Short, "true/false", minLength: 4, maxLength: 5)]
        public string SaveAsFile { get; set; }

        [InputLabel("Server Restart Notification Timer(ms)")]
        [ModalTextInput("restart_timer", placeholder: "600000", maxLength: 15)]
        public string RestartTimer { get; set; }

        [InputLabel("Workshop Scheduler Running Interval(ms)")]
        [ModalTextInput("workshop_inverval", placeholder: "1800000", maxLength: 15)]
        public string WorkshopInterval { get; set; }
    }
}
