using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Services;

namespace FatQueue.Messenger.PostgreSql
{
    public class PostgreSqlRepositoryFactory : RepositoryFactory
    {
        public PostgreSqlRepositoryFactory(SqlSettings settings) : base(() => new PostgreSqlRepository(settings))
        {
        }
    }
}
