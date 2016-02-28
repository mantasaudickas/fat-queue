using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FatQueue.Messenger.Core.Components;

namespace FatQueue.Messenger.Core.Services
{
    public class TaskMonitoringService
    {
        private readonly IRepository _repository;
        private readonly IMessengerService _messengerService;

        public TaskMonitoringService(ILogger logger, IDictionary<string, Task> tasks, RepositoryFactory factory)
        {
            _repository = factory.Create();
            _messengerService = new MessengerService(logger, factory);

            Logger = logger;
            Tasks = tasks;
        }

        private ILogger Logger { get; }
        private IDictionary<string, Task> Tasks { get; }

        public void Start(CancellationToken cancellationToken, Func<string, Task> createNewTask)
        {
            var finished = false;
            while (!finished && !cancellationToken.IsCancellationRequested)
            {
                finished = true;
                try
                {
                    var keys = new List<string>(Tasks.Keys);
                    var keysToRecover = new HashSet<string>();
                    foreach (var processName in keys)
                    {
                        var task = Tasks[processName];

                        if (!task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
                        {
                            finished = false;
                            _repository.Heartbeat(processName);
                        }
                        else
                        {
                            keysToRecover.Add(processName);
                        }
                    }

                    RecoverTask(createNewTask, keysToRecover, Tasks);
                    TryRecoverCrashedProcesses();
                    TryRecoverFailedMessages(cancellationToken);
                }
                catch (Exception e)
                {
                    Logger.Error(e.GetFormattedError("Heartbeat failed!"));
                    finished = false;
                }

                // wait a minute before next heart beat
                TimeSpan.FromMinutes(1).Sleep(Logger, cancellationToken);
            }
        }

        private void RecoverTask(Func<string, Task> createNewTask, HashSet<string> processes,
            IDictionary<string, Task> tasks)
        {
            foreach (var processName in processes)
            {
                Task task;
                if (tasks.TryGetValue(processName, out task))
                {
                    tasks.Remove(processName);
                    try
                    {
                        task.Dispose();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                _messengerService.ReleaseProcessLock(processName);

                // recover task
                var newTask = createNewTask(processName);
                Tasks[processName] = newTask;
            }
        }

        private void TryRecoverCrashedProcesses()
        {
            try
            {
                // if heartbeat does not happen in 5 minutes, clear process
                _repository.ClearStaleProcesses(DateTime.UtcNow.AddMinutes(-5));
            }
            catch (Exception e)
            {
                Logger.Error(e.GetFormattedError("Unable to clear stale processes"));
            }
        }

        private void TryRecoverFailedMessages(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() => _messengerService.RecoverFailedMessages(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
