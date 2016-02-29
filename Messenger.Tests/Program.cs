using System;
using System.Threading;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Services;
using FatQueue.Messenger.Core.Tools;
using FatQueue.Messenger.MsSql;
using FatQueue.Messenger.PostgreSql;
using FatQueue.Messenger.Tests.Executor;

namespace FatQueue.Messenger.Tests
{
    static class Program
    {
        const string ConnectionString = "Server=.\\SQLEXPRESS;Database=Messenger;Integrated Security=SSPI";
        //const string ConnectionString = "Server=127.0.0.1;Database=Messenger;User Id=admin;Password=admin;Enlist=true";

        private static MessengerServer _server;

        static void Main(string[] args)
        {
            var source = new CancellationTokenSource();

            try
            {
                StartServer(source, ConnectionString);

                var clientSettings = new SqlSettings
                {
                    ConnectionString = ConnectionString,
                    Logger = new ConsoleLogger(true)
                };

                //var executor = new LongRunningTestExecutor();
                //executor.Execute(clientSettings);

                //var failingExecutor = new FailingMessagesTestExecutor();
                //failingExecutor.Execute(ConnectionString);

                //var signalRExecutor = new SignalRScriptExecutor();
                //signalRExecutor.Execute(ConnectionString);

                var manualPublisherExecutor = new ManualPublisherExecutor();
                manualPublisherExecutor.Execute(clientSettings);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
            }

            Console.WriteLine("Done. Press enter to exit..");
            Console.ReadLine();

            source.Cancel();

            Console.WriteLine("Shut down?");
            Console.ReadLine();
        }

        private static void StartServer(CancellationTokenSource cancelationSource, string connectionString)
        {
            var settings = new SqlServerSettings
            {
                ConnectionString = connectionString,
                CheckInterval = TimeSpan.FromSeconds(10),
                MessageBatchSize = 1,
                MaxProcessCount = 10,
                Logger = new ConsoleLogger(true),
                CompletedMessages = new CompletedMessages { Archive = true, Cleanup = true, CleanOlderThanUtc = () => DateTime.UtcNow.AddMinutes(-5) },
            };

            //_server = new MessengerServer(settings, new PostgreSqlRepositoryFactory(settings));
            _server = new MessengerServer(settings, new MsSqlRepositoryFactory(settings));
            _server.Start(cancelationSource.Token);
        }
    }
}
