using Discord.Interactions;
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
        public uint RestartTimer = Convert.ToUInt32(TimeSpan.FromMinutes(10).TotalMilliseconds);
        public uint ServerRestartSchedule = Convert.ToUInt32(TimeSpan.FromHours(6).TotalMilliseconds);
        public uint WorkshopItemUpdateSchedule = Convert.ToUInt32(TimeSpan.FromMinutes(30).TotalMilliseconds);
        //public uint WorkshopItemUpdateRestartTimer = Convert.ToUInt32(TimeSpan.FromMinutes(15).TotalMilliseconds);
        public string ServerRestartScheduleType = "Interval";
        public List<string> ServerRestartTimes = new List<string> { "03:00" };

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

    public class ServerProcessSettings
    {
        public string WindowsServerFile = "server.bat";
        public string LinuxServerFile = "server.sh";
        public string UnixServerFile = "server.sh";
    }

    public class BotConfig
    {
        [JsonIgnore]
        public string ServerName { get; set; } = "servertest";

        [JsonIgnore]
        public string[] Linuxparams { get; set; } = Array.Empty<string>();

        [JsonIgnore]
        public bool SaveAsFile = true;

        [JsonIgnore]
        private static readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);

        [JsonIgnore]
        private static readonly string _location = Path.Combine(AppContext.BaseDirectory, "pzdotnetdiscordbot.conf");

        public string GetConfLocation()
        {
            return _location;
        }

        public ulong GuildId;
        public ulong CommandChannelId;
        public ulong LogChannelId;
        public ulong PublicChannelId;

        public ServerLogParserSettings ServerLogParserSettings = new ServerLogParserSettings();
        public ServerScheduleSettings ServerScheduleSettings = new ServerScheduleSettings();
        public BotFeatureSettings BotFeatureSettings = new BotFeatureSettings();

        public RCONSettings RCONSettings = new RCONSettings();

        public ServerProcessSettings ServerProcessSettings = new ServerProcessSettings();

        public async Task Save()
        {
            await _saveLock.WaitAsync();

            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                await File.WriteAllTextAsync(_location, json);
            }
            finally
            {
                await LogFile.WriteLine($"[BotConfig] Config File has been saved...");
                _saveLock.Release();
            }
        }
    }
}
