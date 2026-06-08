using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class ServerLogParserSettings
    {
        public uint PerkParserCacheDuration = 10; // minute
    }

    public class ServerScheduleSettings
    {
        private List<uint> RestartTimers = new List<uint> {
            Convert.ToUInt32(TimeSpan.FromMinutes(10).TotalMilliseconds),
            Convert.ToUInt32(TimeSpan.FromMinutes(5).TotalMilliseconds),
            Convert.ToUInt32(TimeSpan.FromMinutes(1).TotalMilliseconds)
        };
        private uint ServerRestartSchedule = Convert.ToUInt32(TimeSpan.FromHours(6).TotalMilliseconds);
        public uint WorkshopItemUpdateSchedule = Convert.ToUInt32(TimeSpan.FromMinutes(30).TotalMilliseconds);
        public uint WorkshopItemUpdateRestartTimer = Convert.ToUInt32(TimeSpan.FromMinutes(15).TotalMilliseconds);
        public string ServerRestartScheduleType = "Interval";
        public List<string> ServerRestartTimes = new List<string> { "03:00" };

        public uint GetServerRestartSchedule()
        {
            return this.ServerRestartSchedule;
            //return this.ServerRestartScheduleType.ToLower() == "interval" ? this.ServerRestartSchedule : Scheduler.GetIntervalFromTimes(this.ServerRestartTimes);
        }

        public List<uint> GetRestartTimers()
        {
            return this.RestartTimers;
        }

    }

    public class BotFeatureSettings
    {
        public bool AutoServerStart = false;
        public bool NonPublicModLogging = false;
    }

    public class RCONSettings
    {
        public string IP = "127.0.0.1";
        public ushort Port = 27015;
        public string Password = "";
    }

    public class BotConfig
    {
        [JsonIgnore]
        public static readonly string SettingsFile = Path.Combine(AppContext.BaseDirectory, "pzdiscordbot.conf");

        public ulong GuildId;
        public ulong CommandChannelId;
        public ulong LogChannelId;
        public ulong PublicChannelId;

        public ServerLogParserSettings ServerLogParserSettings = new ServerLogParserSettings();
        public ServerScheduleSettings ServerScheduleSettings = new ServerScheduleSettings();
        public BotFeatureSettings BotFeatureSettings = new BotFeatureSettings();

        public RCONSettings RCONSettings = new RCONSettings();

        public string WindowsServerPath = Path.Combine(AppContext.BaseDirectory, "server.bat");
        public string LinuxServerPath = Path.Combine(AppContext.BaseDirectory, "server.sh");
        public string UnixServerPath = Path.Combine(AppContext.BaseDirectory, "server.sh");

        public string ServerName = "servertest";

        public void Save()
        {
            File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(this, Formatting.Indented));
        }


    }
}
