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
        Task LoadRCONConfig();
        Task<string> SendCommandAsync(string command);
    }

    public class RconManager : IRconManager
    {
        private readonly BotConfig _config;

        public RconManager(BotConfig config)
        {
            _config = config;
        }

        public async Task LoadRCONConfig()
        {
            string serverName = _config.ServerName;
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string iniFile = Path.Combine(homePath, "Zomboid", "Server", $"{serverName}.ini");

            if (!File.Exists(iniFile))
                return;

            string tempPort = Tools.GetValueFromIni(iniFile, "RCONPort");
            string tempPassword = Tools.GetValueFromIni(iniFile, "RCONPassword");

            if (ushort.TryParse(tempPort, out ushort port))
                _config.RCONSettings.Port = port;

            _config.RCONSettings.Password = tempPassword ?? "";

            await _config.Save();
        }

        public async Task<string> SendCommandAsync(string command)
        {
            try
            {
                IPAddress tempIP = IPAddress.Parse(_config.RCONSettings.IP);
                ushort tempPort = _config.RCONSettings.Port;
                string tempPwd = _config.RCONSettings.Password;

                using (var rcon = new RCON(tempIP, tempPort, tempPwd))
                {
                    await rcon.ConnectAsync();
                    string response = await rcon.SendCommandAsync(command);
                    return response;
                }
            }
            catch (Exception e)
            {
                await LogFile.WriteLine($"[RconManager] Error: {e.Message}");
                throw;
            }
        }
    }
}
