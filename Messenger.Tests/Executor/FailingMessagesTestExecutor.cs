using FatQueue.Messenger.MsSql;
using FatQueue.Messenger.Tests.Events;
using FatQueue.Messenger.Tests.Handlers;

namespace FatQueue.Messenger.Tests.Executor
{
    public class FailingMessagesTestExecutor
    {
        public void Execute(string connectionString)
        {
            ProcessTasks(connectionString);
        }

        private void ProcessTasks(string connectionString)
        {
            var clientSettings = new MsSqlSettings { ConnectionString = connectionString };
            var messengerClient = new MsSqlMessenger(clientSettings);

            for (int i = 0; i < 1; ++i)
            {
                int index = i+1;
/*
                messengerClient.Publish<FatQueueFailingEventHandler>(
                    handler => handler.Handle(new FatQueueFailingEvent {Fail = (index%9 == 0)}));
*/
                messengerClient.Publish<FatQueueFailingEventHandler>(
                    handler => handler.Handle(new FatQueueFailingEvent {Fail = true}));
            }
        }
    }
}
