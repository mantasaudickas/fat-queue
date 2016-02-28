using FatQueue.Messenger.Core.Services;

namespace FatQueue.Messenger.MsSql
{
    public class MsSqlMessengerServer : MessengerServer
    {
        public MsSqlMessengerServer(SqlServerSettings settings) : base(settings, new MsSqlRepositoryFactory(settings))
        {
        }
    }
}
