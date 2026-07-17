# Project Zomboid DotNETCoreDiscordBot

This project is cross-platform Project Zomboid discord bot inspired by egebilecen's PZServerdiscordbot(https://github.com/egebilecen/PZServerDiscordBot)

Since I was using egebilecen's bot and had a plan to change my host from Windows to Linux,
but the bot doesn't support Linux(based in .NET Framework) so I made it.

When making this, I focused on the original bot's **workshop update scheduler**, 
a feature that *automatically* restarts the server when workshop mod update is detected.

Many functions on original bot has been not implemented yet. 
I'll try my best to implement remaining features as well...

# Bot Commands

| Commands | Params | Feature | Available Channel |
| --- | :-: | --- | :-: |
| **/set_public_channel** | target channel | Set bot's public message channel | command |
| **/set_command_channel** | target channel | Set bot's command message channel | command |
| **/set_log_channel** | target channel | Set bot's log message channel | command |
| **/set_configs** | | Configure your bot setting(see `Miscellaneous` below) | command |
| **/save_server** | | Save current server state | command |
| **/restart_server** | minutes | Restart server after minutes | command |
| **/restart_cancel** | | Cancel scheduled restart job | command |
| **/shutdown_server** | | Save and shut down server immediately | command |
| **/grant_auth** | user | Grant user a permission to execute commands in command channel | command |
| **/revoke_auth** | user | Revoke user a permission to execute commands in command channel | command |
| **/show_auth** | user | Show users who can execute commands in command channel | command |
| **/server_msg** | message | Send announcement message to PZ server | command |
| **/set_language** | language | Set bot language(default is English(US)) | command |
| **/get_cpu_ram** | | Show server's CPU and memory usage | public |
| **/players** | | Show players logged in | public |
| **/check_workshop_mods** | item | Show last (item) update mods in your server | public |
| **/help** | | Show all commands | public |

Commands in ```command``` channel can be used **only for granted user**.

At first, the server owner is granted permission. Owner can manage granted user by ```/grant_auth```, ```/revoke_auth```, and ```/show_auth``` commands.

# How to use

first, create a discord bot. The guide is explained well in original bot page.  
(https://github.com/egebilecen/PZServerDiscordBot#creating-the-discord-bot)

make sure to create ```bot_token.txt``` file on zomboid dedicated server location and write your bot token to it.

## Create a discord channel

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

<img width="358" height="448" alt="image" src="https://github.com/user-attachments/assets/68e73940-4030-468d-8292-f4a1cf9f7fff" />

## Translation Guide

<img width="1280" height="569" alt="image" src="https://github.com/user-attachments/assets/71e256d8-57cd-4f00-b407-ac13ace31c81" />

After Alpha v0.1.2, the bot supports multilanguage, including

* English(UK/US)
* Korean(ROK/DPRK)
* Burmese(machine translated)
* Kirundi(machine translated)

Even though this is just a small bot, for any kind-hearted volunteers reading this
who might want to help with the translation, 
let me explain how to create a translation file.

First, please name the file **(file name).json.**
The file name **should be formatted** as `lowercase country code_uppercase language code`.

Also, please include the **language_name** and **date_format** two keys at the top.

For `language_name`, use the name of the language in that language,
and for `date_format`, enter the date format used in that country, separated by slashes (`/`).
Please note that it’s **MM**, not mm since `mm` stands for minutes.

For the rest, just refer to the existing JSON files.
Keep the keys as they are, translate *only* the values!

# Thanks to

* [PZServerDiscordBot](https://github.com/egebilecen/PZServerDiscordBot) by egebilecen
* [Discord.NET](https://github.com/discord-net/Discord.net) by discord-net 
* [CoreRCON](https://github.com/Challengermode/CoreRcon) by ChallengerMode
