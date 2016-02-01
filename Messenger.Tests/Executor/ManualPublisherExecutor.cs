using System;
using FatQueue.Messenger.MsSql;
using FatQueue.Messenger.Tests.Events;
using FatQueue.Messenger.Tests.Handlers;

namespace FatQueue.Messenger.Tests.Executor
{
    public class ManualPublisherExecutor
    {
        private static readonly string[] Queues = { "Q0", "Q1", "Q2", "Q3", "Q4", "Q5", "Q6", "Q7", "Q8", "Q9" };

        public void Execute(MsSqlSettings clientSettings)
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
                    int queueIndex;
                    if (key.KeyChar >= '0' && key.KeyChar <= '9')
                    {
                        queueIndex = (key.KeyChar-'0');
                    }
                    else
                    {
                        queueIndex = random.Next(0, Queues.Length);
                    }

                    var queueName = Queues[queueIndex];

                    var messengerClient = new MsSqlMessenger(clientSettings);
                    messengerClient.PublishAsFirst<FatQueuePrintMessageEventHandler>(handler => handler.Handle(
                        new FatQueuePrintMessageEvent
                        {
                            Message = new CustomMessage {Message = "Published from main manually"}
                        }), queueName);

                    Console.WriteLine("Message published to queue {0}", queueName);
                }
            }
        }
    }
}
