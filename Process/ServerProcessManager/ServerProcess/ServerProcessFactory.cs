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
        public static ServerProcess Create(BotConfig botConfig)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return new WindowsProcess(botConfig);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { 
                return new LinuxProcess(botConfig);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new LinuxProcess(botConfig);
            }

            return new WindowsProcess(botConfig);
        }
    }
}
