using System;

namespace FatQueue.Messenger.Core
{
    public interface ISerializer
    {
        string Serialize<T>(T data);
        object Deserialize(string value, Type instanceType);
    }
}
