using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DotNETCoreDiscordBot
{
    public static class LogFile
    {
        public static string logsFile = Path.Combine(AppContext.BaseDirectory, "PZBot.log");

        private static readonly object fileLock = new object();

        public static string GetDateTime()
        {
            return DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        public static void WriteLine(string log)
        {
            var msg = "(" + GetDateTime() + ") " + log;

            lock (fileLock)
            {
                var file = File.AppendText(logsFile);
                file.WriteLine(msg);
                file.Close();
            }

            Console.WriteLine(msg);
        }
    }
}
