using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class LinuxProcess : ServerProcess
    {
        public LinuxProcess(BotConfig botConfig, string scriptPath, ILogFile logFile) : base(botConfig, scriptPath, logFile)
        {
        }

        protected override void ExtractOSParams(string line)
        {
            string[] args = _botConfig.Linuxparams;

            if (args == null || args.Length == 0) return;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-servername" && i + 1 < args.Length)
                {
                    string servername = args[i + 1].Replace("\"", "").Trim();
                    if (!string.IsNullOrEmpty(servername))
                    {
                        _botConfig.ServerName = servername;
                    }
                    break;
                }
            }
        }

        public override void SetupProcessStartInfo(ProcessStartInfo startInfo, string serverName)
        {
            Process.Start("chmod", $"+x \"{_scriptPath}\"")?.WaitForExit();

            startInfo.FileName = "/bin/bash";
            startInfo.Arguments = $"\"{_scriptPath}\" -servername \"{serverName}\"";
        }

        public override async Task<double> GetCPUUsage()
        {
            try
            {
                var stat1 = File.ReadAllLines("/proc/stat")[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                long idle1 = long.Parse(stat1[4]);
                long total1 = stat1.Skip(1).Take(7).Select(long.Parse).Sum();

                await Task.Delay(500);

                var stat2 = File.ReadAllLines("/proc/stat")[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                long idle2 = long.Parse(stat2[4]);
                long total2 = stat2.Skip(1).Take(7).Select(long.Parse).Sum();

                long totalDelta = total2 - total1;
                if (totalDelta == 0) return 0.0;

                return (1.0 - ((double)(idle2 - idle1) / totalDelta)) * 100.0;
            }
            catch { return 0.0; }
        }

        public override double GetRAMUsage()
        {
            try
            {
                var lines = File.ReadAllLines("/proc/meminfo");
                double total = 0, available = 0;

                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:"))
                        double.TryParse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1], out total);
                    else if (line.StartsWith("MemAvailable:"))
                        double.TryParse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1], out available);
                }

                return total > 0 ? ((total - available) / total) * 100.0 : 0.0;
            }
            catch { return 0.0; }
        }
    }
}
