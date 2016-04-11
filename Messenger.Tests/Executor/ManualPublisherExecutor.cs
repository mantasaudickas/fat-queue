using System;
using System.Collections.Generic;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.MsSql;
using FatQueue.Messenger.PostgreSql;
using FatQueue.Messenger.Tests.Events;
using FatQueue.Messenger.Tests.Handlers;

namespace FatQueue.Messenger.Tests.Executor
{
    public class ManualPublisherExecutor
    {
        private static readonly string[] Queues = { "Q0", "Q1", "Q2", "Q3", "Q4", "Q5", "Q6", "Q7", "Q8", "Q9" };
        private static readonly Queue<Guid> Identities = new Queue<Guid>();

        public void Execute(SqlSettings clientSettings)
        {
            var random = new Random();
            var finished = false;
            while (!finished)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    finished = true;
                }
                else
                {
                    var messengerClient = new Core.Services.Messenger(clientSettings, new MsSqlRepositoryFactory(clientSettings));
                    //var messengerClient = new Core.Services.Messenger(clientSettings, new PostgreSqlRepositoryFactory(clientSettings));

                    int delayExecution = 0;
                    Guid? identity = null;
                    int queueIndex;
                    if (key.KeyChar >= '0' && key.KeyChar <= '9')
                    {
                        queueIndex = (key.KeyChar-'0');
                        delayExecution = 30;
                        identity = Guid.NewGuid();
                        Identities.Enqueue(identity.Value);
                    }
                    else if (key.Key == ConsoleKey.Delete)
                    {
                        var identityToCancel = Identities.Dequeue();
                        var canceled = messengerClient.Cancel(identityToCancel);
                        if (canceled)
                        {
                            Console.WriteLine("Canceled: {0}", identityToCancel);
                        }
                        else
                        {
                            Console.WriteLine("Unable to cancel anymore: {0}", identityToCancel);
                        }
                        continue;
                    }
                    else
                    {
                        queueIndex = random.Next(0, Queues.Length);
                    }

                    var queueName = Queues[queueIndex];

                    var publishSettings = new PublishSettings
                    {
                        DelayExecutionInSeconds = delayExecution,
                        Identity = identity
                    };

                    var request = new FatQueuePrintMessageEvent
                    {
                        Message = new CustomMessage { Message = "Published from main manually. Delay: " + delayExecution }
                    };

                    messengerClient.Publish<FatQueuePrintMessageEventHandler>(handler => handler.Handle(request), new QueueName(queueName), publishSettings);

                    Console.WriteLine("Message published to queue {0}", queueName);
                }
            }
        }
    }
}
