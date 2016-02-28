using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Services;

namespace FatQueue.Messenger.PostgreSql
{
    public class PostgreSqlMessengerService : MessengerService
    {
        public PostgreSqlMessengerService(SqlSettings settings, ILogger logger) : base(logger, new PostgreSqlRepositoryFactory(settings))
        {
        }
    }
}
