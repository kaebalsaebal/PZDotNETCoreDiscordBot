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

        [InputLabel("Your Windows Server Script File Name")]
        [ModalTextInput("windows_server_file", placeholder: "server.bat")]
        public string? WindowsServerFile { get; set; }

        [InputLabel("Your Linux Server Script File Name")]
        [ModalTextInput("linux_server_file", placeholder: "server.sh")]
        public string? LinuxServerFile { get; set; }

        [InputLabel("Your MacOS Server Script File Name")]
        [ModalTextInput("unix_server_file", placeholder: "server.sh")]
        public string? UnixServerFile { get; set; }
    }
}
