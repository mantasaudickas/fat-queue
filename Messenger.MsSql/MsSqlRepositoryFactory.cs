using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Services;

namespace FatQueue.Messenger.MsSql
{
    public class MsSqlRepositoryFactory : RepositoryFactory
    {
        public MsSqlRepositoryFactory(SqlSettings settings) : base(() => new MsSqlRepository(settings))
        {
        }
    }
}
