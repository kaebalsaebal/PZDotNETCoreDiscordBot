using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public interface IScheduledJob
    {
        Task ExecuteAsync(CancellationToken token);
    }
}