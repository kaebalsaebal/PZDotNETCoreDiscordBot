using CoreRCON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public interface IRconManager
    {
        Task<string> SendCommandAsync(string command);
    }

    public class RconManager : IRconManager
    {
        private readonly BotConfig _config;

        public RconManager(BotConfig config)
        {
            _config = config;
        }

        public async Task<string> SendCommandAsync(string command)
        {
            string ip = _config.RCONSettings.IP;
            ushort port = _config.RCONSettings.Port;
            string password = _config.RCONSettings.Password;

            string serverName = _config.ServerName;
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string iniFile = Path.Combine(homePath, "Zomboid", "Server", $"{serverName}.ini");

            if (File.Exists(iniFile))
            {
                string tempPort = GetValueFromIni(iniFile, "RCONPort");
                string tempPassword = GetValueFromIni(iniFile, "RCONPassword");

                if (!string.IsNullOrEmpty(tempPort) && ushort.TryParse(tempPort, out ushort parsedPort))
                {
                    port = parsedPort;
                }

                if (tempPassword != null)
                {
                    password = tempPassword;
                }

                await _config.Save();
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
            catch (Exception e)
            {
                await LogFile.WriteLine($"[RconManager] Error: {e.Message}");
                return $"RCON Error: ({e.Message})";
            }
        }

        private string GetValueFromIni(string iniPath, string key)
        {
            if (!File.Exists(iniPath)) return "";
            var lines = File.ReadAllLines(iniPath);
            foreach (var line in lines)
            {
                if (line.StartsWith($"{key}="))
                {
                    return line.Substring($"{key}=".Length);
                }
            }
            return "";
        }
    }
}
