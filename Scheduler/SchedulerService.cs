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
		private readonly ILogFile _logFile;
		private readonly CancellationTokenSource _token = new CancellationTokenSource();

        public SchedulerService(IEnumerable<IScheduledJob> jobs, ILogFile logFile)
        {
            _jobs = jobs;
            _logFile = logFile;
        }

        public async Task StartAll()
        {
            _logFile.WriteLine(Messages.Get("init_scheduler"));

            foreach (var job in _jobs)
            {
                _ = Task.Run(() => job.ExecuteAsync(_token.Token));
            }
        }

        public async Task StopAll()
        {
            _token.Cancel();
            _logFile.WriteLine(Messages.Get("stop_scheduler"));
        }
    }
}
