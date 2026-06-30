using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot.Scheduler
{
    public interface ISchedulerService
    {
        Task StartAll();
        Task StopAll();
    }

    public class SchedulerService: ISchedulerService
    {
        private readonly IEnumerable<IScheduledJob> _jobs;
        private readonly CancellationTokenSource _token = new CancellationTokenSource();

        public SchedulerService(IEnumerable<IScheduledJob> jobs)
        {
            _jobs = jobs;
        }

        public async Task StartAll()
        {
            LogFile.WriteLine(Messages.Get("init_scheduler"));

            foreach (var job in _jobs)
            {
                _ = Task.Run(() => job.ExecuteAsync(_token.Token));
            }
        }

        public async Task StopAll()
        {
            _token.Cancel();
            LogFile.WriteLine(Messages.Get("stop_scheduler"));
        }
    }
}
