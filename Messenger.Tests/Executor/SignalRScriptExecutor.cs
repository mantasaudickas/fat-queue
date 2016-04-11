using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.MsSql;
using FatQueue.Messenger.Tests.Events;
using FatQueue.Messenger.Tests.Handlers;

namespace FatQueue.Messenger.Tests.Executor
{
    public class SignalRScriptExecutor
    {
        public void Execute(string connectionString)
        {
            var clientSettings = new SqlSettings { ConnectionString = connectionString };
            var messengerClient = new Core.Services.Messenger(clientSettings, new MsSqlRepositoryFactory(clientSettings));
            messengerClient.Publish<FatQueueSignalREventHandler>(handler => handler.Handle(new FatQueueSignalREvent()));
        }
    }
}
