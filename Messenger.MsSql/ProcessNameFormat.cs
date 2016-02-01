namespace FatQueue.Messenger.MsSql
{
    public enum ProcessNameFormat
    {
        ByMachineNameAndApplicationDirectory,
        ByMachineNameAndProcessId,
        ByMachineNameAndCommandLine,
        Custom
    }
}
