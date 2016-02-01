using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Components;
using FatQueue.Messenger.Core.Expressions;
using FatQueue.Messenger.MsSql.Data;
using FatQueue.Messenger.MsSql.Orm;

namespace FatQueue.Messenger.MsSql
{
    public class MsSqlMessenger : CommonComponent, IMessengerClient
    {
        private static readonly ConcurrentDictionary<string, int> QueueNames = new ConcurrentDictionary<string, int>(); 
        
        private readonly MsSqlRepository _repository;

        public MsSqlMessenger(MsSqlSettings settings) : base(settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.ConnectionString)) throw new NullReferenceException("ConnectionString is not specified!");

            _repository = new MsSqlRepository(settings.ConnectionString);
        }

        public void Publish<T>(Expression<Action<T>> methodCall, QueueName queueName, PublishSettings publishSettings = null)
        {
            var timer = Stopwatch.StartNew();
            bool success = true;

            try
            {
                var requestType = typeof (T);

                if (publishSettings == null)
                {
                    publishSettings = PublishSettings.Default;
                }

                var messageContext = new MessageContext
                {
                    QueueName = ResolveQueueName(queueName, requestType),
                    Settings = publishSettings
                };

                var expressionSerializer = new ExpressionSerializer(Serializer);
                var content = expressionSerializer.Serialize(methodCall);
                var contentType = requestType.GetContentType();
                var context = Serializer.Serialize(messageContext);

                Persist(messageContext.QueueName, contentType, content, context, false);
            }
            catch
            {
                success = false;
                throw;
            }
            finally
            {
                timer.Stop();
                Logger.Trace("Publishing {0} in {1}", success ? "completed" : "failed", timer.Elapsed);
            }
        }

        public void Publish<T>(Expression<Action<T>> methodCall, string queueName = null, PublishSettings publishSettings = null)
        {
            Publish(methodCall, new QueueName {Name = queueName}, publishSettings);
        }

        public void PublishAsFirst<T>(Expression<Action<T>> methodCall, QueueName queueName, PublishSettings publishSettings = null)
        {
            var timer = Stopwatch.StartNew();
            bool success = true;

            try
            {
                var requestType = typeof(T);

                if (publishSettings == null)
                {
                    publishSettings = PublishSettings.Default;
                }

                var messageContext = new MessageContext
                {
                    QueueName = ResolveQueueName(queueName, requestType),
                    Settings = publishSettings,
                    PublishedAsFirstInTheQueue = true
                };

                var expressionSerializer = new ExpressionSerializer(Serializer);
                var content = expressionSerializer.Serialize(methodCall);
                var contentType = requestType.GetContentType();
                var context = Serializer.Serialize(messageContext);

                Persist(messageContext.QueueName, contentType, content, context, true);
            }
            catch
            {
                success = false;
                throw;
            }
            finally
            {
                timer.Stop();
                Logger.Trace("Publishing {0} in {1}", success ? "completed" : "failed", timer.Elapsed);
            }
        }

        public void PublishAsFirst<T>(Expression<Action<T>> methodCall, string queueName = null, PublishSettings publishSettings = null)
        {
            PublishAsFirst(methodCall, new QueueName { Name = queueName }, publishSettings);
        }

        private string ResolveQueueName(QueueName queueName, Type requestType)
        {
            var name = string.Empty;
            var canUseScope = true;

            if (queueName != null)
            {
                name = queueName.Name;
                canUseScope = !queueName.IgnoreQueueSettingsScope;
            }

            if (canUseScope)
            {
                var scope = QueueSettingsScope.Current;
                if (scope != null)
                {
                    name = scope.QueueName;
                }
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = GetDefaultQueueName(requestType);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = DefaultQueueName;
            }

            return name;
        }

        private void Persist(string queueName, string contentType, string content, string context, bool insert)
        {
            var queueId = GetQueueId(queueName, _repository, Logger);

            var timer = Stopwatch.StartNew();
            try
            {
                if (insert)
                    _repository.InsertMessage(queueId, contentType, content, context);
                else
                    _repository.CreateMessage(queueId, contentType, content, context);
            }
            finally
            {
                timer.Stop();
                Logger.Trace("Created new message in the queue {0} in {1}", queueName, timer.Elapsed);
            }
        }

        internal static int GetQueueId(string queueName, MsSqlRepository repository, ILogger logger)
        {
            int queueId = QueueNames.GetOrAdd(queueName, s => FetchQueueId(queueName, repository, logger));
            return queueId;
        }

        private static int FetchQueueId(string queueName, MsSqlRepository repository, ILogger logger)
        {
            var queueId = repository.FetchQueueId(queueName);
            if (!queueId.HasValue)
            {
                var timer = Stopwatch.StartNew();
                try
                {
                    queueId = repository.CreateQueue(queueName);
                }
                finally
                {
                    timer.Stop();
                    logger.Trace("Created new queue {0} with id {1} in {2}", queueName, queueId, timer.Elapsed);
                }
            }
            return queueId.Value;
        }
    }
}
