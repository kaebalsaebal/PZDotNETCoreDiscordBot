using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class LinuxServerStrategy : ServerProcessStrategy
    {

        private readonly BotConfig _botConfig;

        public LinuxServerStrategy(BotConfig botConfig)
        {
            _botConfig = botConfig;
            _scriptPath = Path.Combine(AppContext.BaseDirectory, botConfig.ServerProcessSettings.LinuxServerFile);
        }

        protected override void ExtractOSParams(string line)
        {
            string[] args = _botConfig.Linuxparams;

            if (args == null || args.Length == 0) return;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-servername" && i + 1 < args.Length)
                {
                    string servername = args[i + 1].Replace("\"", "").Trim();
                    if (!string.IsNullOrEmpty(servername))
                    {
                        _botConfig.ServerName = servername;
                    }
                    break;
                }
            }
        }

        public override void SetupProcessStartInfo(ProcessStartInfo startInfo, string serverName)
        {
            Process.Start("chmod", $"+x \"{_scriptPath}\"")?.WaitForExit();

            startInfo.FileName = "/bin/bash";
            startInfo.Arguments = $"\"{_scriptPath}\" -servername \"{serverName}\"";
        }
    }
}
