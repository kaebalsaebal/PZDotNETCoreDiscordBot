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
        public static string Location = Path.Combine(AppContext.BaseDirectory, "PZBot.log");

        private static readonly object _fileLock = new object();

        public static string GetDateTime()
        {
            return DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        public static void WriteLine(string log)
        {
            var msg = "(" + GetDateTime() + ") " + log;

            lock (_fileLock)
            {
                var file = File.AppendText(Location);
                file.WriteLine(msg);
                file.Close();
            }

            Console.WriteLine(msg);
        }
    }
}
