using JetBrains.Annotations;

namespace FatQueue.Messenger.MsSql.Data
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class QueueInfo
    {
        public string Name { get; set; }
        public int QueueId { get; set; }
        public int? Retries { get; set; }
    }
}
