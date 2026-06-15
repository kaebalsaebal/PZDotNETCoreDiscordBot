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
        public static ServerProcessStrategy Create(BotConfig botConfig)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return new WindowsServerStrategy(botConfig);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { 
                return new LinuxServerStrategy(botConfig);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new LinuxServerStrategy(botConfig);
            }

            return new WindowsServerStrategy(botConfig);
        }
    }
}
