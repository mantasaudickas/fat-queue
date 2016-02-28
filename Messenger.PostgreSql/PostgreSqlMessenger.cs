using FatQueue.Messenger.Core.Database;

namespace FatQueue.Messenger.PostgreSql
{
    public class PostgreSqlMessenger : Core.Services.Messenger
    {
        public PostgreSqlMessenger(SqlSettings settings) : base(settings, new PostgreSqlRepositoryFactory(settings))
        {
        }
    }
}
