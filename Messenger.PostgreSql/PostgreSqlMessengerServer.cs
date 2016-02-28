using FatQueue.Messenger.Core.Services;

namespace FatQueue.Messenger.PostgreSql
{
    public class PostgreSqlMessengerServer : MessengerServer
    {
        public PostgreSqlMessengerServer(SqlServerSettings settings) : base(settings, new PostgreSqlRepositoryFactory(settings))
        {
        }
    }
}
