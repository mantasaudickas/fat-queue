﻿using System;
using JetBrains.Annotations;

namespace FatQueue.Messenger.MsSql.Data
{
    internal class Message
    {
        public int QueueId { [UsedImplicitly] get; set; }
        public string ContentType { [UsedImplicitly] get; set; }
        public string Content { [UsedImplicitly] get; set; }
        public string Context { [UsedImplicitly] get; set; }
    }
}
