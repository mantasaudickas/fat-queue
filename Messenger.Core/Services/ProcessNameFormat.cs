namespace FatQueue.Messenger.Core.Services
{
    public enum ProcessNameFormat
    {
        ByMachineNameAndApplicationDirectory,
        ByMachineNameAndProcessId,
        ByMachineNameAndCommandLine,
        Custom
    }
}
