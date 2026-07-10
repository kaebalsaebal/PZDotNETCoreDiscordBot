using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{

    public interface IServerProcess
    {
        string GetDirectory();
        Task ParseServerScript();
        void SetupProcessStartInfo(ProcessStartInfo startInfo, string serverName);
        Task<double> GetCPUUsage();
        double GetRAMUsage();
    }

    public abstract class ServerProcess: IServerProcess
    {
        protected string _scriptPath;
        protected readonly BotConfig _botConfig;
        protected readonly ILogFile _logFile;

        private string _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Zomboid");

        protected ServerProcess(BotConfig botConfig, string scriptPath, ILogFile logFile)
        {
            _botConfig = botConfig;
            _scriptPath = scriptPath;
            _logFile = logFile;
        }

        public string GetDirectory()
        {
            return Path.GetDirectoryName(_scriptPath) ?? "";
        }

        public async Task ParseServerScript()
        {
            if (!File.Exists(_scriptPath)) return;

            string[] lines = await File.ReadAllLinesAsync(_scriptPath);
            bool needModify = false;
            List<string> newLines = new List<string>();

            foreach (string line in lines)
            {
                if (line.Contains("java") || line.Contains("zomboid.steam"))
                {
                    ExtractCommonParams(line);

                    ExtractOSParams(line);
                }
                newLines.Add(line);

                if (line.Trim().ToLower().Contains("pause") || line.Trim().ToLower().StartsWith("read "))
                {
                    needModify = true;
                }
            }

            if (needModify)
            {
                await File.WriteAllLinesAsync(_scriptPath, newLines);
                _logFile.WriteLine(Messages.Get("parse_script_modify"));
            }
        }

        private void ExtractCommonParams(string param)
        {
            if (param.Contains("user.home"))
            {
                string customHome = param.Split('=').Last().Replace("\"", "");
                _basePath = customHome;

                if (Directory.Exists(Path.Combine(_basePath, "Zomboid")))
                {
                    _basePath = Path.Combine(_basePath, "Zomboid");
                }
                _logFile.WriteLine(Messages.Get("custom_location_found").KeyFormat(("location", _basePath)));
            }
        }

        protected abstract void ExtractOSParams(string line);
        public abstract void SetupProcessStartInfo(ProcessStartInfo startInfo, string serverName);

        public abstract Task<double> GetCPUUsage();
        public abstract double GetRAMUsage();
    }
}
