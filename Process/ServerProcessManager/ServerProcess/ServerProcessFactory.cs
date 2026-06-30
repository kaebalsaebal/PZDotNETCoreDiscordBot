using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class ServerProcessFactory
    {
        public IServerProcess Create(BotConfig botConfig)
        {

            string windowsLoc = botConfig.ServerProcessSettings.WindowsServerFile;
            string linuxLoc = botConfig.ServerProcessSettings.LinuxServerFile;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return new WindowsProcess(botConfig, windowsLoc);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { 
                return new LinuxProcess(botConfig, linuxLoc);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new LinuxProcess(botConfig, linuxLoc);
            }

            return new WindowsProcess(botConfig, windowsLoc);
        }
    }
}
