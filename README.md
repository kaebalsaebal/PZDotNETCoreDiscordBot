# Project Zomboid DotNETCoreDiscordBot

This project is cross-platform Project Zomboid discord bot inspired by egebilecen's PZServerdiscordbot(https://github.com/egebilecen/PZServerDiscordBot)

I made it for running any platform including Windows, Linux, Docker, etc.

I focused on the original bot's workshop update scheduler, a feature that automatically restarts the server when workshop mod update is detected,  
so many functions on original bot has been not implemented.

I'll try my best to implement the remaining features as well...

# Bot Commands

| Commands | Params | Feature | Available Channel |
| --- | :-: | --- | :-: |
| **/set_public_channel** | target channel | Set bot's public message channel | |
| **/set_command_channel** | target channel | Set bot's command message channel | |
| **/set_log_channel** | target channel | Set bot's log message channel | |
| **/set_configs** | | Configure your bot setting(see below) | command |
| **/save_server** | | Save current server state | command |
| **/restart_server** | minutes | Restart server after param minutes | command |
| **/restart_cancel** | | Cancel scheduled restart job | command |
| **/grant_auth_** | user | Grant user a permission to execute commands in command channel | command |
| **/revoke_auth** | user | Revoke user a permission to execute commands in command channel | command |
| **/show_auth** | user | Show users who can execute commands in command channel | command |
| **/get_cpu_ram** | | Show server's CPU and memory usage | command |
| **/players** | | Show players logged in | public |
| **/check_workshop_mods** | item | Show last (item) update mods in your server | public |

# How to use

first, create a discord bot. The guide is explained well in original bot page.  
(https://github.com/egebilecen/PZServerDiscordBot#creating-the-discord-bot)

make sure to create ```bot_token.txt``` file on zomboid dedicated server location and write your bot token to it.

## create a discord channel

<img width="433" height="210" alt="image" src="https://github.com/user-attachments/assets/5c895171-5787-4dc7-a90a-4a5f7f33f889" />

Then, Create **three** Discord channels as below:

* command channel - Used to send bot commands and interact with the bot.
* log channel - Used for server logs, update notifications, and administrative messages.
* public channel - Used for public announcements and messages visible to all users.

## Start server

**Copy binary file** at zomboid dedicated server location.

Make sure that token, conf, and binary file are **located at dedicated server directory**.

### On Windows

copy ```StartServer64.bat ``` and rename it corresponding to `WindowsServerFile` value in `pzdotnetdiscordbot.conf`.

Execute .exe file. Servername can be configured at ```pzdotnetdiscordbot.conf``` file.

### On Linux

copy ```start-server.sh``` and rename it corresponding to `LinuxServerFile` value in `pzdotnetdiscordbot.conf`.

The vanilla server script has case sensitivity issue on workshop, so please download the script below and run it first.
https://gist.github.com/okaMi0ka/cfd532993e80ad3f808558f1aafd5ea9

execute as below:

```bash
./DotNETCoreDiscordBot_Linux -servername (your server name)
```

### If server runs for the fist time

<img width="365" height="133" alt="image" src="https://github.com/user-attachments/assets/027277e9-20bd-4898-8f11-818d9e5c48a3" />

When you run server first, the bot sends you direct message like above.

Follow the instruction, and when these channels set, the bot automatically starts.

Commands in command channel can be used by granted user.

At first, server owner is granted permission. Owner can manage granted user by ```/grant_auth```, ```/revoke_auth```, and ```/show_auth``` commands._

### RCON setting

This project uses RCON for sending messages to server.

When the bot starts, it automatically finds your own RCON settings in ```(home)/Zomboid/Server/(Your Server name).ini``` and uses that.

So **make sure set your RCON password value NOT EMPTY** on that ```.ini``` file. Unless some command(restart, save, etc) will not run.

```ini
...
RCONPort=27015
...
RCONPassword=1234
...
```

## Miscellaneous

If you set 'true' on /set_configs menu, the ```PZBot_Logs``` directory will be created and logs will be stacked on there.

<img width="143" height="69" alt="image" src="https://github.com/user-attachments/assets/0ad64fd5-a156-4880-9517-33aae193e9ba" />

<img width="148" height="53" alt="image" src="https://github.com/user-attachments/assets/3dcd503d-1a91-4f3d-9de9-3c87efd0bd77" />

```/set_config``` modal example:

<img width="480" height="490" alt="image" src="https://github.com/user-attachments/assets/81cec294-b38d-4e89-8627-86cd4ed65c60" />


# Thanks to

* [PZServerDiscordBot](https://github.com/egebilecen/PZServerDiscordBot) by egebilecen
* [Discord.NET](https://github.com/discord-net/Discord.net) by discord-net 
* [CoreRCON](https://github.com/Challengermode/CoreRcon) by ChallengerMode
