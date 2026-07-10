using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class Tools
    {
        public string GetServerIniPath(string servername)
        {
            string serverName = servername;

            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string targetDirectory = Path.Combine(homePath, "Zomboid", "Server");

            return Path.Combine(targetDirectory, $"{serverName}.ini");
        }

        public string GetValueFromIni(string iniPath, string key)
        {
            if (!File.Exists(iniPath)) return "";

            var lines = File.ReadAllLines(iniPath);
            foreach (var line in lines)
            {
                if (line.StartsWith($"{key}="))
                {
                    string value = line.Substring($"{key}=".Length);
                    return value;
                }
            }
            return "";
        }

        // Split long message to List because of 2000 limit of discord followup
        public List<StringBuilder> SplitSB(StringBuilder sb)
        {
            var splitedSB = new List<StringBuilder>();
            if (sb.Length == 0) return splitedSB;

            int maxLength = 1900;

            var tempSB = new StringBuilder();

            using (var sr = new StringReader(sb.ToString()))
            {
                string line = sr.ReadLine();
                while (line != null)
                {
                    if (tempSB.Length + line.Length + 1 > maxLength)
                    {
                        splitedSB.Add(tempSB);
                        tempSB = new StringBuilder();
                    }

                    tempSB.AppendLine(line);
                    line = sr.ReadLine();
                }
            }

            if (tempSB.Length > 0)
            {
                splitedSB.Add(tempSB);
            }

            return splitedSB;
        }
    }
}
