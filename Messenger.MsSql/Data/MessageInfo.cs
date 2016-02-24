using System;
using JetBrains.Annotations;

namespace FatQueue.Messenger.MsSql.Data
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    internal class MessageInfo
    {
        public int MessageId { get; set; }
        public string Content { get; set; }
        public string Context { get; set; }
        public DateTime StartDate { get; set; }
        public Guid? Identity { get; set; }
    }
}
