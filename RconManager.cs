using CoreRCON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public static class RconManager
    {
        public static async Task<string> SendCommandAsync(string command)
        {
            string ip = Application.BotConfig.RCONSettings.IP;
            ushort port = Application.BotConfig.RCONSettings.Port;
            string password = Application.BotConfig.RCONSettings.Password;

            string iniFile = ServerUtility.GetServerIniPath();

            if (File.Exists(iniFile))
            {
                string tempPort = ServerUtility.GetValueFromIni(iniFile, "RCONPort");
                string tempPassword = ServerUtility.GetValueFromIni(iniFile, "RCONPassword");

                if (!string.IsNullOrEmpty(tempPort) && ushort.TryParse(tempPort, out ushort parsedPort))
                {
                    port = parsedPort;
                }

                if (tempPassword != null)
                {
                    password = tempPassword;
                }
            }

            try
            {
                IPAddress ipAddress = IPAddress.Parse(ip);

                using (var rcon = new RCON(ipAddress, port, password))
                {
                    await rcon.ConnectAsync();

                    string response = await rcon.SendCommandAsync(command);
                    return response;
                }
            }
            catch(Exception e)
            {
                LogFile.WriteLine($"[RconManager] Error: {e.Message}");
                return $"RCON Error: ({e.Message})";
            }
        }
    }
}
