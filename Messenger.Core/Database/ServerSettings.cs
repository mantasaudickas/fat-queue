namespace FatQueue.Messenger.Core.Database
{
    public static class ServerSettings
    {
        public const int NextTryAfterFailInSeconds = 30;
        public const int MaxRetriesBeforeFail = 10;
        public const int JobTimeoutInSeconds = 30 * 60; // 30 minutes

        public const int FailedMessageBatchSize = 10;
        public const int FailedMessageRecoveryFromMinutes = 5 * 24 * 60;    // lets try for 5 days
        public const int FailedMessageRecoveryToMinutes = 60;    // lets try first recover only after one hour
    }
}
