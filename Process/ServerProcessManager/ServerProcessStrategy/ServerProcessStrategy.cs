using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public abstract class ServerProcessStrategy
    {
        protected string _scriptPath = "";
        private string _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Zomboid");

        public string GetDirectory()
        {
            return Path.GetDirectoryName(_scriptPath);
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
                await LogFile.WriteLine($"[ServerProcessManager] Removed pause/read in Server Script File");
            }
        }

        private async void ExtractCommonParams(string param)
        {
            if (param.Contains("user.home"))
            {
                string customHome = param.Split('=').Last().Replace("\"", "");
                _basePath = customHome;

                if (Directory.Exists(Path.Combine(_basePath, "Zomboid")))
                {
                    _basePath = Path.Combine(_basePath, "Zomboid");
                }
                await LogFile.WriteLine($"[ServerProcessManager] Custom Location Found: {_basePath}");
            }
        }

        protected abstract void ExtractOSParams(string line);
        public abstract void SetupProcessStartInfo(ProcessStartInfo startInfo, string serverName);
    }
}
