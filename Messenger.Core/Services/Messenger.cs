﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using FatQueue.Messenger.Core.Components;
using FatQueue.Messenger.Core.Data;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Expressions;

namespace FatQueue.Messenger.Core.Services
{
    public class Messenger : CommonComponent, IMessengerClient
    {
        private static readonly ConcurrentDictionary<string, int> QueueNames = new ConcurrentDictionary<string, int>(); 
        
        private readonly IRepository _repository;

        public Messenger(SqlSettings settings, RepositoryFactory factory) : base(settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.ConnectionString)) throw new NullReferenceException("ConnectionString is not specified!");

            _repository = factory.Create();
        }

        public void Publish<T>(Expression<Action<T>> methodCall, QueueName queueName = null, PublishSettings publishSettings = null)
        {
            Publish<T, FatQueueContextFactory, FatQueueContext>(methodCall, null, queueName, publishSettings);
        }

        public void Publish<T, TContextFactory, TContext>(
            Expression<Action<T>> action, 
            Expression<Func<TContextFactory, TContext>> contextFactory, 
            QueueName queueName = null,
            PublishSettings publishSettings = null) 
            where TContext : ExecutionContext
        {
            var timer = Stopwatch.StartNew();
            bool success = true;

            try
            {
                var requestType = typeof(T);

                if (publishSettings == null)
                {
                    publishSettings = new PublishSettings();
                }

                var messageContext = new MessageContext
                {
                    QueueName = ResolveQueueName(queueName, requestType),
                    Settings = publishSettings,
                    CreateDate = DateTime.UtcNow
                };

                var expressionSerializer = new ExpressionSerializer(Serializer);
                var content = expressionSerializer.Serialize(action);
                var contextCreator = contextFactory != null ? expressionSerializer.SerializeFactory(contextFactory) : null;
                var contentType = requestType.GetContentType();
                var context = Serializer.Serialize(messageContext);

                Persist(
                    messageContext.QueueName,
                    contentType, content,
                    contextCreator,
                    context,
                    publishSettings.Identity,
                    publishSettings.DelayExecutionInSeconds,
                    publishSettings.HighestPriority);
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

        public bool Cancel(Guid identity)
        {
            return _repository.CancelMessages(identity) > 0;
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

        private void Persist(string queueName, string contentType, string content, string contextFactory, string context, Guid? identity, int delayInSeconds, bool insert)
        {
            var queueId = GetQueueId(queueName, _repository, Logger);

            var taskIdentity = identity.GetValueOrDefault();
            if (taskIdentity == Guid.Empty)
                taskIdentity = Guid.NewGuid();

            var timer = Stopwatch.StartNew();
            try
            {
                if (insert)
                {
                    _repository.InsertMessage(queueId, contentType, content, contextFactory, context, taskIdentity);
                }
                else
                {
                    if (identity.HasValue)
                    {
                        _repository.CreateMessageIfNotExists(queueId, contentType, content, contextFactory, context, delayInSeconds, taskIdentity);
                    }
                    else
                    {
                        _repository.CreateMessage(queueId, contentType, content, contextFactory, context, delayInSeconds, taskIdentity);
                    }
                }
            }
            finally
            {
                timer.Stop();
                Logger.Trace("Created new message in the queue {0} in {1}", queueName, timer.Elapsed);
            }
        }

        internal static int GetQueueId(string queueName, IRepository repository, ILogger logger)
        {
            int queueId = QueueNames.GetOrAdd(queueName, s => FetchQueueId(queueName, repository, logger));
            return queueId;
        }

        private static int FetchQueueId(string queueName, IRepository repository, ILogger logger)
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
