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

        [InputLabel("ServerRestartTimer")]
        [ModalTextInput("Server Restart Notification Timer(ms: 1000ms=1s)", placeholder: "600000", maxLength: 15)]
        public string RestartTimer { get; set; }

        [InputLabel("RCON IP")]
        [ModalTextInput("rcon_ip", placeholder: "127.0.0.1", maxLength: 15)]
        public string RCONIP { get; set; }

        [InputLabel("RCON Port")]
        [ModalTextInput("rcon_port", placeholder: "27015", maxLength: 5)]
        public string RCONPort { get; set; }

        [InputLabel("RCON Password")]
        [ModalTextInput("rcon_pw", style: TextInputStyle.Short)]
        public string? RCONPassword { get; set; }
    }
}
