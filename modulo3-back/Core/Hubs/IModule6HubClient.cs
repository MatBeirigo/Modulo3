namespace Core.Hubs;

public interface IModule6HubClient
{
    Task RelayStateUpdated(object payload);
    Task ModuleConfigured(object payload);
    Task UnconfiguredModule(object payload);
}