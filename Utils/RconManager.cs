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
        private readonly BotConfig _botConfig;
        private readonly ILogFile _logFile;

		public RconManager(BotConfig config, ILogFile logFile)
        {
            _botConfig = config;
            _logFile = logFile;
        }

        public async Task LoadRCONConfig()
        {
            string serverName = _botConfig.ServerName;
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string iniFile = Path.Combine(homePath, "Zomboid", "Server", $"{serverName}.ini");

            if (!File.Exists(iniFile))
                return;

            var tools = new Tools();
            string tempPort = tools.GetValueFromIni(iniFile, "RCONPort");
            string tempPassword = tools.GetValueFromIni(iniFile, "RCONPassword");

            if (ushort.TryParse(tempPort, out ushort port))
                _botConfig.RCONSettings.Port = port;

            _botConfig.RCONSettings.Password = tempPassword ?? "";

            await _botConfig.Save(_logFile);
        }

        public async Task<string> SendCommandAsync(string command)
        {
            try
            {
                IPAddress tempIP = IPAddress.Parse(_botConfig.RCONSettings.IP);
                ushort tempPort = _botConfig.RCONSettings.Port;
                string tempPwd = _botConfig.RCONSettings.Password;

                using (var rcon = new RCON(tempIP, tempPort, tempPwd))
                {
                    await rcon.ConnectAsync();
                    string response = await rcon.SendCommandAsync(command);
                    return response;
                }
            }
            catch (Exception e)
            {
                _logFile.WriteLine(Messages.Get("rcon_error").KeyFormat(("error", e.Message)));
                throw new Exception(e.Message);
            }
        }
    }
}
