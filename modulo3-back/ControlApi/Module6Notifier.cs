using Core.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ControlApi;

public class Module6Notifier : IModule6Notifier
{
    private readonly IHubContext<Module6Hub, IModule6HubClient> _hub;

    public Module6Notifier(IHubContext<Module6Hub, IModule6HubClient> hub)
    {
        _hub = hub;
    }

    public Task NotifyRelayStateUpdated(int moduleId, string state) =>
        _hub.Clients.All.RelayStateUpdated(new { moduleId, state, timestamp = DateTime.UtcNow });

    public Task NotifyModuleConfigured(int moduleId, string uniqueId, string ip) =>
        _hub.Clients.All.ModuleConfigured(new { moduleId, uniqueId, ip, timestamp = DateTime.UtcNow });

    public Task NotifyUnconfiguredModule(string uniqueId, string? ip) =>
        _hub.Clients.All.UnconfiguredModule(new { uniqueId, ip, timestamp = DateTime.UtcNow });
}