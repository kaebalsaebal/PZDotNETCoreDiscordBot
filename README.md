# Project Zomboid DotNETCoreDiscordBot

This project is cross-platform Project Zomboid discord bot inspired by egebilecen's PZServerdiscordbot(https://github.com/egebilecen/PZServerDiscordBot)

I made it for running any platform including Windows, Linux, Docker, etc.

I focused on the original bot's workshop update scheduler, a feature that automatically restarts the server when workshop mod update is detected,  
so many functions on original bot has been not implemented.

I'll try my best to implement the remaining features as well...

# How to use

first, create a discord bot. The guide is explained well in original bot page.  
(https://github.com/egebilecen/PZServerDiscordBot#creating-the-discord-bot)

make sure to create ```bot_token.txt``` file on zomboid dedicated server location and write your bot token to it.

## create a discord channel

<img width="433" height="210" alt="image" src="https://github.com/user-attachments/assets/5c895171-5787-4dc7-a90a-4a5f7f33f889" />

Then, Create three Discord channels:

* command - Used to send bot commands and interact with the bot.
* log - Used for server logs, update notifications, and administrative messages.
* public - Used for public announcements and messages visible to all users.

## configure a config file

Make a ```pzdotnetdiscordbot.conf``` file on zomboid dedicated server location(same as ```bot_token.txt```).

You can find template file at the releases page.

and configure your guild and channel ids on below:

```conf
  "GuildId": ,
  "CommandChannelId": ,
  "LogChannelId": ,
  "PublicChannelId": ,
```

(If you don't know ids, read below pzdotnetdiscordbot.conf guide.)

## start server

Mopy binary file at zomboid dedicated server location.

Make sure that token, conf, and binary file are located at same location.

### On Windows

copy ```StartServer64.bat ``` and rename it to match the `WindowsServerPath` configured in `pzdotnetdiscordbot.conf`.


Execute .exe file. Servername can be configured at ```pzdotnetdiscordbot.conf``` file.

### On Linux

copy ```start-server.sh``` and rename it to match the `LinuxServerPath` configured in `pzdotnetdiscordbot.conf`.

ProjectZomboid64.json

```bash
./DotNETCoreDiscordBot_Linux -servername (your server name)
```

If servername parameter is not configured, it will start default server written at ```pzdotnetdiscordbot.conf```.

## RCON Warning

This project uses RCON for sending message to server.

By Default, the RCON settings(IP,port,password) are configured in ```pzdotnetdiscordbot.conf``` on default value.

But if you have configured your own RCON settings in (home)/Zomboid/Server/(Your Server name).ini,  
The bot automatically finds and uses it.

# pzdotnetdiscordbot.conf guide

```conf
{
  "GuildId": Your Discord Server ID,
  "CommandChannelId": Your Discord Server Command Channel ID,
  "LogChannelId": Your Discord Server Log Channel ID,
  "PublicChannelId": Your Discord Server Command Channel ID,
  "ServerLogParserSettings": {
    "PerkParserCacheDuration": 10
  },
  "ServerScheduleSettings": {
    "RestartTimes": [600000,300000,60000],
    "ServerRestartSchedule": 86400000,
    "WorkshopItemUpdateSchedule": 1800000,
    "WorkshopItemUpdateRestartTimer": 600000,
    "ServerRestartScheduleType": "Interval",
    "ServerRestartTimes": [
      "03:00"
    ]
  },
  "BotFeatureSettings": {
    "AutoServerStart": false,
    "NonPublicModLogging": false
  },
  "RCONSettings": {
    "IP": "127.0.0.1",
    "Port": 27015,
    "Password": "1234"
  },
  "WindowsServerPath": "./server.bat",
  "LinuxServerPath": "./server.sh",
  "UnixServerPath": "./server.sh",
  "ServerName": "servertest"
}
```

## GuildId, CommandChannelId, LogchannelId, PublicChannelId

Open your disbord in web browser and go to channel.

<img width="921" height="803" alt="image" src="https://github.com/user-attachments/assets/31c9a710-d46c-4e62-84b4-92ad7d0c3cf3" />

Note the url.


### Example

if your current url is 
```
https://discord.com/channels/aaaa/bbbb
```
, **aaaa** is **GuildId** and **bbbb** is **ChannelId**.

## RestartTimes

The `RestartTimes` option accepts a list of miliseconds values in descending order.

When the remaining time reaches the first value, a notification is sent immediately.

Additional notifications are then sent based on the difference between consecutive values.

### Example

```json
{
  RestartTimes: [600000, 300000, 60000]
}
```

For the example above:

| Schedule Value | Notification Time                                  |
| -------------- | -------------------------------------------------- |
| 600000(10 min) | Immediately when 10 minutes remain                 |
| 300000(5 min)  | 5 minutes after the previous notification (10 - 5) |
| 60000(1 min)   | 4 minutes after the previous notification (5 - 1)  |

As a result, notifications are sent when there are **10 minutes**, **5 minutes**, and **1 minute** remaining.

Duplicate values are automatically removed, and the schedule is sorted in descending order before processing.

## WorkshopItemUpdateSchedule

The interval, in milliseconds, at which the server checks for Workshop item updates.

Example:

```json
"WorkshopItemUpdateSchedule": 1800000
```

The value above causes the workshop update checker to run every **30 minutes**.

---

## WorkshopItemUpdateRestartTimer

The delay, in milliseconds, before the server restarts after a Workshop item update has been detected.

Example:

```json
"WorkshopItemUpdateRestartTimer": 600000
```

The value above causes the server to restart **10 minutes** after an update is found.

Automatically send restart notifications on **[Value, Half Value, 1 min]**

---

### Example

```json
{
  "WorkshopItemUpdateSchedule": 1800000,
  "WorkshopItemUpdateRestartTimer": 600000
}
```

Behavior:

1. The server checks for Workshop updates every 30 minutes.
2. If an update is detected, a restart is scheduled.
3. The server automatically restarts 10 minutes later to apply the update.
4. automatically send notifications on 10 min, 5 min, 1 min.

## RCONSettings, ServerPath, ServerName

```json
{
  "RCONSettings": {
    "IP": "127.0.0.1",
    "Port": 27015,
    "Password": "1234"
  },
  "WindowsServerPath": "./server.bat",
  "LinuxServerPath": "./server.sh",
  "UnixServerPath": "./server.sh",
  "ServerName": "servertest"
}
```

| Setting                 | Description                                                                           |
| ----------------------- | ------------------------------------------------------------------------------------- |
| `RCONSettings.IP`       | The IP address used to connect to the game server's RCON interface.                   |
| `RCONSettings.Port`     | The RCON port exposed by the game server.                                             |
| `RCONSettings.Password` | The password required to authenticate with the RCON server.                           |
| `WindowsServerPath`     | Path to the server startup script or executable on Windows systems.                   |
| `LinuxServerPath`       | Path to the server startup script or executable on Linux systems.                     |
| `UnixServerPath`        | Path to the server startup script or executable on Unix-based systems.                |
| `ServerName`            | A friendly name used to identify the server in logs, notifications, and bot messages. |

### Example

```json
{
  "RCONSettings": {
    "IP": "192.168.1.100",
    "Port": 27015,
    "Password": "my-rcon-password"
  },
  "WindowsServerPath": "C:/Servers/MyServer/start.bat",
  "LinuxServerPath": "/home/steam/server/start.sh",
  "UnixServerPath": "/home/steam/server/start.sh",
  "ServerName": "Production Server"
}
```

In this example, the bot connects to the server through RCON at `192.168.1.100:27015`, uses the specified password for authentication, and starts the server using the appropriate startup script depending on the operating system.


# Thanks to

* [egebilecen](https://github.com/egebilecen): original discord bot author
* [Discord.NET(https://docs.discordnet.dev/): framework to make this
