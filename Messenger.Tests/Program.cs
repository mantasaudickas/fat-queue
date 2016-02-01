using System;
using System.Threading;
using FatQueue.Messenger.Core.Tools;
using FatQueue.Messenger.MsSql;
using FatQueue.Messenger.Tests.Executor;

namespace FatQueue.Messenger.Tests
{
    static class Program
    {
        const string ConnectionString = "Server=.\\SQLEXPRESS;Database=Messenger;Integrated Security=SSPI";

        private static MsSqlMessengerServer _server;

        static void Main(string[] args)
        {
            var source = new CancellationTokenSource();

            try
            {
                StartServer(source, ConnectionString);

                var clientSettings = new MsSqlSettings
                {
                    ConnectionString = ConnectionString,
                    Logger = new ConsoleLogger(true)
                };

                var executor = new LongRunningTestExecutor();
                //executor.Execute(clientSettings);

                var failingExecutor = new FailingMessagesTestExecutor();
                failingExecutor.Execute(ConnectionString);

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
            var settings = new MsSqlServerSettings
            {
                ConnectionString = connectionString,
                CheckInterval = TimeSpan.FromSeconds(10),
                MessageBatchSize = 10,
                MaxProcessCount = 10,
                Logger = new ConsoleLogger(true),
                CompletedMessages = new CompletedMessages { Archive = true, Cleanup = true, CleanOlderThanUtc = () => DateTime.UtcNow.AddMinutes(-5) },
            };

            _server = new MsSqlMessengerServer(settings);
            _server.Start(cancelationSource.Token);
        }
    }
}
