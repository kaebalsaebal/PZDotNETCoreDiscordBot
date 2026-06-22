using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class WindowsProcess : ServerProcess
    {
        public WindowsProcess(BotConfig botConfig, string scriptPath) : base(botConfig, scriptPath)
        {
        }

        protected override void ExtractOSParams(string line)
        {
            if(line.Contains("-servername", StringComparison.OrdinalIgnoreCase))
            {
                string key = "-servername";
                int index = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    string temp = line.Substring(index + key.Length).Trim();
                    string[] values = temp.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (values.Length > 0)
                    {
                        string servername = values[0].Replace("\"", "").Trim();

                        if (!string.IsNullOrEmpty(servername))
                        {
                            _botConfig.ServerName = servername;
                        }
                    }
                }
            }
        }

        public override void SetupProcessStartInfo(ProcessStartInfo startInfo, string serverName)
        {
            startInfo.FileName = _scriptPath;
        }
    }
}
