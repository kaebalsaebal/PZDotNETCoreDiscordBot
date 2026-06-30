using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class WindowsProcess : ServerProcess
    {
        public WindowsProcess(BotConfig botConfig, string scriptPath) : base(botConfig, scriptPath)
        {
        }

        protected override void ExtractOSParams(string line)
        {
            if(line.Contains("-servername", StringComparison.OrdinalIgnoreCase))
            {
                string key = "-servername";
                int index = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    string temp = line.Substring(index + key.Length).Trim();
                    string[] values = temp.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (values.Length > 0)
                    {
                        string servername = values[0].Replace("\"", "").Trim();

                        if (!string.IsNullOrEmpty(servername))
                        {
                            _botConfig.ServerName = servername;
                        }
                    }
                }
            }
        }

        public override void SetupProcessStartInfo(ProcessStartInfo startInfo, string serverName)
        {
            startInfo.FileName = _scriptPath;
        }

        public override async Task<double> GetCPUUsage()
        {
            return await Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_PerfOS_Processor");
                var mo = searcher.Get().Cast<ManagementObject>().FirstOrDefault(x => x["Name"].ToString() == "_Total");

                return mo != null ? Convert.ToDouble(mo["PercentProcessorTime"]) : 0.0;
            });
        }

        public override double GetRAMUsage()
        {
            using var wmiObject = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            var memoryValues = wmiObject.Get().Cast<ManagementObject>().FirstOrDefault();

            if (memoryValues != null)
            {
                double free = double.Parse(memoryValues["FreePhysicalMemory"].ToString());
                double total = double.Parse(memoryValues["TotalVisibleMemorySize"].ToString());
                return ((total - free) / total) * 100.0;
            }
            return 0.0;
        }
    }
}
