using System;
using System.Diagnostics;
using System.Threading;
using System.Transactions;
using FatQueue.Messenger.Core.Components;
using FatQueue.Messenger.Core.Data;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Expressions;

namespace FatQueue.Messenger.Core.Services
{
    public class MessageProcessor
    {
        private readonly ISerializer _serializer;
        private readonly IJobActivator _jobActivator;

        public MessageProcessor(IJobActivator jobActivator, ISerializer serializer, ILogger logger, RepositoryFactory factory)
        {
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (jobActivator == null) throw new ArgumentNullException(nameof(jobActivator));

            _serializer = serializer;
            _jobActivator = jobActivator;

            Repository = factory.Create();
            Service = new MessengerService(logger, factory);
            Logger = logger;
            
            Canceled = false;
        }

        private IRepository Repository { get; }
        private IMessengerService Service { get; }
        private ILogger Logger { get; }
        private bool Canceled { get; set; }

        public void Process(CancellationToken cancellationToken, string processName, int batchSize, TimeSpan checkInterval, bool archiveMessages)
        {
            cancellationToken.Register(() => Canceled = true);

            Service.ReleaseProcessLock(processName);

            while (!Canceled)
            {
                var sleep = false;

                var timer = Stopwatch.StartNew();
                try
                {
                    var queueInfo = Repository.LockQueue(processName);
                    if (queueInfo != null)
                    {
                        using (new QueueSettingsScope(queueInfo.Name))
                        {
                            var state = ProcessQueueMessages(queueInfo.QueueId, batchSize, archiveMessages);
                            ValidateResultState(state, queueInfo);
                        }
                    }
                    else
                    {
                        sleep = true;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.GetFormattedError());
                    sleep = true;
                }
                finally
                {
                    timer.Stop();
                    Logger.Trace("Batch processing for {0} messages completed in {1}", batchSize, timer.Elapsed);
                }

                if (sleep)
                {
                    checkInterval.Sleep(Logger, cancellationToken);
                    Service.ReleaseProcessLock(processName);
                }
            }

            Logger.Trace("Processing thread completed.");
        }

        private ResultState ProcessQueueMessages(int queueId, int batchSize, bool archiveMessages)
        {
            var state = new ResultState
            {
                IsFailed = false
            };

            var messages = Repository.FetchQueueMessages(queueId, batchSize);
            if (messages != null)
            {
                foreach (var message in messages)
                {
                    var timer = Stopwatch.StartNew();
                    var messageId = message.MessageId;

                    state.LastMessageId = messageId;

                    try
                    {
                        var messageContext = GetMessageContext(message.Context);

                        state.MaxRetries = messageContext.Settings.MaxRetriesBeforeFail;
                        state.RecoveryMode = messageContext.Settings.RecoveryMode;

                        PublishSettings settings = messageContext.Settings;
                        using (var transaction = Repository.CreateProcessingTransaction(settings.JobIsolationLevel, settings.JobTimeout))
                        {
                            var executor = new ExpressionExecutor(_serializer, _jobActivator);
                            executor.Execute(message.Content);

                            if (transaction.TransactionStatus.HasValue)
                            {
                                var transactionStatus = transaction.TransactionStatus.Value;
                                if (transactionStatus == TransactionStatus.Aborted ||
                                    transactionStatus == TransactionStatus.InDoubt)
                                {
                                    throw new Exception($"Invalid transaction status: [{transactionStatus}]! Unable to commit!");
                                }
                            }

                            Repository.RemoveMessage(messageId, archiveMessages && !settings.DiscardWhenComplete);

                            transaction.Complete();
                        }

                        state.ProcessedMessages += 1;
                    }
                    catch (Exception e)
                    {
                        var error = e.GetFormattedError(messageId);
                        Logger.Error(error);

                        state.Error = error;
                        state.IsFailed = true;
                    }
                    finally
                    {
                        timer.Stop();
                        Logger.Trace("Message {0} processed in {1}", messageId, timer.Elapsed);
                    }

                    if (Canceled || state.IsFailed)
                    {
                        break;
                    }
                }
            }

            return state;
        }

        private void ValidateResultState(ResultState state, QueueInfo queueInfo)
        {
            if (state.IsFailed)
            {
                // message failed, increment retry count and set error information
                // if there was at least one message successfull reset retry count
                var retries = (state.ProcessedMessages > 0 ? 0 : queueInfo.Retries.GetValueOrDefault()) + 1;

                if (retries >= state.MaxRetries && state.RecoveryMode != RecoveryMode.Block)
                {
                    RecoverQueue(queueInfo.QueueId, state.LastMessageId, state.RecoveryMode);
                }
                else
                {
                    var nextTryTime = DateTime.UtcNow.AddSeconds(ServerSettings.NextTryAfterFailInSeconds);

                    Repository.SetQueueFailure(queueInfo.QueueId, retries, state.Error, nextTryTime);
                }
            }
            else
            {
                Repository.ReleaseQueue(queueInfo.QueueId);
            }
        }

        private void RecoverQueue(int queueId, int messageId, RecoveryMode recoveryMode)
        {
            using (var transaction = Repository.CreateProcessingTransaction(TransactionIsolationLevel.ReadCommitted, TimeSpan.FromMinutes(1)))
            {
                switch (recoveryMode)
                {
                    case RecoveryMode.MakeLast:
                        Repository.MoveMessageToEnd(messageId);
                        break;
                    case RecoveryMode.MarkAsFailed:
                        Repository.CopyMessageToFailed(messageId);
                        Repository.RemoveMessage(messageId);
                        break;
                    default:
                        // should not happen
                        throw new NotImplementedException();
                }

                Repository.ReleaseQueue(queueId);
                transaction.Complete();
            }
        }

        private MessageContext GetMessageContext(string context)
        {
            MessageContext messageContext = null;

            if (!string.IsNullOrWhiteSpace(context))
            {
                messageContext =
                    _serializer.Deserialize(context, typeof (MessageContext)) as MessageContext;
            }

            if (messageContext == null)
            {
                messageContext = new MessageContext();
            }

            if (messageContext.Settings == null)
            {
                messageContext.Settings = new PublishSettings();
            }

            if (messageContext.Settings.MaxRetriesBeforeFail == 0)
            {
                messageContext.Settings.MaxRetriesBeforeFail = ServerSettings.MaxRetriesBeforeFail;
            }

            if (messageContext.Settings.JobTimeout <= TimeSpan.Zero)
            {
                messageContext.Settings.JobTimeout = TimeSpan.FromSeconds(ServerSettings.JobTimeoutInSeconds);
            }

            return messageContext;
        }
    }
}
