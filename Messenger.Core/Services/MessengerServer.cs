using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FatQueue.Messenger.Core.Components;
using FatQueue.Messenger.Core.Expressions;

namespace FatQueue.Messenger.Core.Services
{
    public class MessengerServer : CommonComponent, IMessengerServer
    {
        private readonly RepositoryFactory _repositoryFactory;
        private readonly IJobActivator _jobActivator;
        private readonly TimeSpan _checkInterval;

        private readonly int _maxProcessCount;
        private readonly int _messageBatchSize;

        private readonly ProcessNameFormat _processNameFormat;
        private readonly string _customProcessName;

        private readonly CompletedMessages _completedMessages;

        public MessengerServer(SqlServerSettings settings, RepositoryFactory repositoryFactory)
            : base(settings)
        {
            _repositoryFactory = repositoryFactory;
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.ConnectionString)) throw new NullReferenceException("ConnectionString is not specified!");

            _jobActivator = settings.JobActivator ?? new JobActivator();
            _checkInterval = settings.CheckInterval.HasValue && settings.CheckInterval > TimeSpan.Zero
                ? settings.CheckInterval.Value
                : TimeSpan.FromSeconds(10);

            _maxProcessCount = settings.MaxProcessCount > 0 ? settings.MaxProcessCount : Environment.ProcessorCount*2;
            _messageBatchSize = settings.MessageBatchSize > 0 ? settings.MessageBatchSize : 10;

            _processNameFormat = settings.ProcessNameFormat;
            _customProcessName = settings.CustomProcessName;

            _completedMessages = settings.CompletedMessages ?? CompletedMessages.Default;

            if (_completedMessages.Archive && _completedMessages.Cleanup && _completedMessages.CleanOlderThanUtc == null)
            {
                _completedMessages.CleanOlderThanUtc = () => DateTime.Today;
            }
        }

        public void Start(CancellationToken? cancellationToken)
        {
            var startLogMessage = new StringBuilder();
            startLogMessage.AppendFormat("Starting MessengerServer with {0} processes.", _maxProcessCount).AppendLine();
            startLogMessage.AppendFormat("  Batch size: {0}", _messageBatchSize).AppendLine();
            startLogMessage.AppendFormat("  Check interval: {0}", _checkInterval).AppendLine();
            startLogMessage.AppendFormat("  Archive messages: {0}", _completedMessages.Archive).AppendLine();
            startLogMessage.AppendFormat("  Cleanup old messages: {0}", _completedMessages.Cleanup).AppendLine();
            Logger.Info(startLogMessage.ToString());

            var token = cancellationToken.GetValueOrDefault(CancellationToken.None);
            Task.Factory.StartNew(() => StartMonitor(token), TaskCreationOptions.LongRunning);

            if (_completedMessages.Cleanup)
            {
                CleanupService cleanupService = new CleanupService(Logger, _repositoryFactory.Create());
                cleanupService.Start(_completedMessages.CleanOlderThanUtc, token);
            }
        }

        private void StartMonitor(CancellationToken cancellationToken)
        {
            var tasks = new Dictionary<string, Task>();
            for (int i = 0; i < _maxProcessCount; i++)
            {
                int id = i+1;

                var processName = GetProcessName(id);
                var task = StartTask(processName, cancellationToken);
                tasks.Add(processName, task);
            }

            TaskMonitoringService monitoringService = new TaskMonitoringService(Logger, tasks, _repositoryFactory);
            monitoringService.Start(cancellationToken, processName => StartTask(processName, cancellationToken));
        }

        private string GetProcessName(int id)
        {
            string processKey;
            switch (_processNameFormat)
            {
                case ProcessNameFormat.ByMachineNameAndApplicationDirectory:
                    var directory = Path.GetDirectoryName(GetType().Assembly.Location);
                    processKey = $"{Environment.MachineName}:{id} at {directory}";
                    break;
                case ProcessNameFormat.ByMachineNameAndCommandLine:
                    var workingDirectory = Environment.CommandLine;
                    processKey = $"{Environment.MachineName}:{id} at {workingDirectory}";
                    break;
                case ProcessNameFormat.ByMachineNameAndProcessId:
                    var processId = Process.GetCurrentProcess().Id;
                    processKey = $"{Environment.MachineName}:{processId}:{id}";
                    break;
                case ProcessNameFormat.Custom:
                    processKey = $"{Environment.MachineName}:{id} [{_customProcessName}]";
                    break;

                default:
                    throw new NotSupportedException();
            }
            return processKey;
        }

        private Task StartTask(string processName, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => ProcessMessages(cancellationToken, processName), TaskCreationOptions.LongRunning);
        }

        private void ProcessMessages(CancellationToken cancelationToken, string processName)
        {
            Logger.Debug("Starting process with name {0}", processName);

            var processor = new MessageProcessor(_jobActivator, Serializer, Logger, _repositoryFactory);
            processor.Process(cancelationToken, processName, _messageBatchSize, _checkInterval, _completedMessages.Archive);
        }
    }
}
