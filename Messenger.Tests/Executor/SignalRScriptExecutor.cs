using FatQueue.Messenger.MsSql;
using FatQueue.Messenger.Tests.Events;
using FatQueue.Messenger.Tests.Handlers;

namespace FatQueue.Messenger.Tests.Executor
{
    public class SignalRScriptExecutor
    {
        public void Execute(string connectionString)
        {
            var clientSettings = new MsSqlSettings { ConnectionString = connectionString };
            var messengerClient = new MsSqlMessenger(clientSettings);
            messengerClient.Publish<FatQueueSignalREventHandler>(
                handler => handler.Handle(new FatQueueSignalREvent()));
        }
    }
}
