namespace Core.Hubs;

public interface IModule6Notifier
{
    Task NotifyRelayStateUpdated(int moduleId, string state);
    Task NotifyModuleConfigured(int moduleId, string uniqueId, string ip);
    Task NotifyUnconfiguredModule(string uniqueId, string? ip);
}