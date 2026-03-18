using Core.DTO;
using Core.Hubs;
using Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Services;

public class DataAggregationService
{
    private readonly ILogger<DataAggregationService> _logger;
    private readonly IModule6Notifier _module6Notifier;
    private readonly ConcurrentDictionary<string, CircularBuffer<MeasurementData>> _measurementBuffers;
    private readonly ConcurrentDictionary<string, MeasurementData> _latestMeasurements;
    private readonly ConcurrentDictionary<string, ProtectionEvent> _activeEvents;
    private readonly ConcurrentDictionary<string, DeviceStatus> _deviceStatuses;
    private readonly ConcurrentDictionary<string, FilteredEventReport> _eventReports;
    private readonly ConcurrentDictionary<string, AggregatedAlarm> _aggregatedAlarms;
    private readonly ConcurrentDictionary<string, SwitchCommand> _pendingCommands;
    private readonly ConcurrentDictionary<string, DateTime> _unconfiguredModules;
    private readonly ConcurrentDictionary<string, string> _moduleIpAddresses;
    private readonly ConcurrentDictionary<int, string> _moduleIpById;
    private readonly ConcurrentDictionary<int, string> _relayStates;
    private readonly ConcurrentDictionary<string, int> _pendingStateRequest = new();
    private readonly CircularBuffer<ProtectionEvent> _eventHistory;
    private const int EventHistorySize = 500;
    private readonly TimeSpan _staleThreshold = TimeSpan.FromSeconds(10);
    private const int BufferSize = 1000;

    public DataAggregationService(ILogger<DataAggregationService> logger, IModule6Notifier module6Notifier)
    {
        _logger = logger;
        _module6Notifier = module6Notifier;
        _measurementBuffers = new ConcurrentDictionary<string, CircularBuffer<MeasurementData>>();
        _latestMeasurements = new ConcurrentDictionary<string, MeasurementData>();
        _activeEvents = new ConcurrentDictionary<string, ProtectionEvent>();
        _deviceStatuses = new ConcurrentDictionary<string, DeviceStatus>();
        _eventReports = new ConcurrentDictionary<string, FilteredEventReport>();
        _aggregatedAlarms = new ConcurrentDictionary<string, AggregatedAlarm>();
        _pendingCommands = new ConcurrentDictionary<string, SwitchCommand>();
        _unconfiguredModules = new ConcurrentDictionary<string, DateTime>();
        _moduleIpAddresses = new ConcurrentDictionary<string, string>();
        _moduleIpById = new ConcurrentDictionary<int, string>();
        _relayStates = new ConcurrentDictionary<int, string>();
        _eventHistory = new CircularBuffer<ProtectionEvent>(EventHistorySize);
    }

    public async Task ProcessPacket(BroadcastPacket packet)
    {
        await Task.Run(() =>
        {
            try
            {
                switch (packet.Module)
                {
                    case "MODULE1": ProcessModule1Measurement(packet); break;
                    case "MODULE2": ProcessModule2Event(packet); break;
                    case "MODULE4": ProcessModule4Report(packet); break;
                    case "MODULE5": ProcessModule5Alarm(packet); break;
                    case "MODULE6": ProcessModule6State(packet); break;
                    default: _logger.LogWarning("Módulo desconhecido: {Module}", packet.Module); break;
                }
                UpdateDeviceStatus(packet.Origin, packet.Sequence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar pacote do módulo {Module}", packet.Module);
            }
        });
    }

    public async Task ProcessModule6Packet(Module6Packet packet)
    {
        if (packet.Prefix == Module6Packet.VisualizationPrefix && packet.Command == Module6Command.CheckState)
        {
            if (!int.TryParse(packet.State, out var moduleId) || moduleId <= 0)
            {
                _logger.LogWarning(
                    "Resposta de estado com ID de módulo inválido — State='{State}', Raw='{Raw}', IP={Ip}",
                    packet.State, packet.RawPacket, packet.SourceIp);
                return;
            }

            var relayState = packet.IsRelayClosed ? "CLOSED" : "OPEN";

            if (packet.SourceIp != null && !_moduleIpById.ContainsKey(moduleId))
            {
                _moduleIpById[moduleId] = packet.SourceIp;
                _logger.LogInformation(
                    "IP registrado automaticamente via resposta de estado: ModuleID={Id}, IP={Ip}",
                    moduleId, packet.SourceIp);
            }

            _relayStates[moduleId] = relayState;
            _logger.LogInformation(
                "Estado do relé recebido: ModuleID={Id}, Estado={State}, IP={Ip}",
                moduleId, relayState, packet.SourceIp);

            await _module6Notifier.NotifyRelayStateUpdated(moduleId, relayState);
            UpdateDeviceStatus($"MODULE6-{moduleId:D2}", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return;
        }

        var deviceId = $"MODULE6-{packet.RecipientId:D2}";

        switch (packet.Command)
        {
            case Module6Command.Unconfigured:
                _logger.LogInformation("Módulo 6 sem configuração. UniqueID={UniqueId}, IP={Ip}", packet.UniqueId, packet.SourceIp);

                if (packet.UniqueId != null)
                {
                    _unconfiguredModules[packet.UniqueId] = DateTime.UtcNow;
                    if (packet.SourceIp != null)
                        _moduleIpAddresses[packet.UniqueId] = packet.SourceIp;
                }

                await _module6Notifier.NotifyUnconfiguredModule(packet.UniqueId ?? string.Empty, packet.SourceIp);
                break;

            case Module6Command.ConfigureId:
                _logger.LogInformation("Configuração de ID: NovoID={NewId}, UniqueID={UniqueId}", packet.RecipientId, packet.UniqueId);
                if (packet.SourceIp != null)
                    _moduleIpById[packet.RecipientId] = packet.SourceIp;
                break;

            default:
                _logger.LogWarning("Comando Módulo 6 não tratado: {Command}", packet.Command);
                break;
        }

        UpdateDeviceStatus(deviceId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public async Task RegisterModuleId(int moduleId, string uniqueId)
    {
        if (_moduleIpAddresses.TryGetValue(uniqueId, out var ip))
        {
            _moduleIpById[moduleId] = ip;
            _unconfiguredModules.TryRemove(uniqueId, out _);

            _logger.LogInformation("Módulo registrado: ID={ModuleId}, IP={Ip}, UniqueID={UniqueId}", moduleId, ip, uniqueId);

            await _module6Notifier.NotifyModuleConfigured(moduleId, uniqueId, ip);
        }
        else
        {
            _logger.LogWarning("IP não encontrado para UniqueID={UniqueId}", uniqueId);
        }
    }

    public List<Module6StatusDto> GetConfiguredModules()
    {
        var result = new List<Module6StatusDto>();

        foreach (var (moduleId, ip) in _moduleIpById)
        {
            var deviceId = $"MODULE6-{moduleId:D2}";

            var uniqueId = _moduleIpAddresses
                .FirstOrDefault(kv => kv.Value == ip).Key ?? string.Empty;

            var relayState = _relayStates.TryGetValue(moduleId, out var state)
                ? state
                : "UNKNOWN";

            _deviceStatuses.TryGetValue(deviceId, out var status);

            var lastUpdate = status?.LastUpdate ?? DateTime.MinValue;
            var isOnline = status is { IsStale: false, IsActive: true };

            result.Add(new Module6StatusDto(moduleId, uniqueId, relayState, lastUpdate, isOnline));
        }

        return result;
    }

    public string? GetModuleIp(string uniqueId)
    {
        _moduleIpAddresses.TryGetValue(uniqueId, out var ip);
        return ip;
    }

    public string? GetModuleIpById(int moduleId)
    {
        _moduleIpById.TryGetValue(moduleId, out var ip);
        return ip;
    }

    public List<string> GetUnconfiguredModules() => _unconfiguredModules.Keys.ToList();

    public Dictionary<int, string> GetAllRelayStates() => new(_relayStates);

    private void ProcessModule1Measurement(BroadcastPacket packet)
    {
        var measurement = JsonSerializer.Deserialize<MeasurementData>(packet.Data);
        if (measurement == null) return;

        measurement.Timestamp = packet.Timestamp;
        measurement.Sequence = packet.Sequence;
        measurement.DeviceId = packet.Origin;

        var buffer = _measurementBuffers.GetOrAdd(packet.Origin, _ => new CircularBuffer<MeasurementData>(BufferSize));
        buffer.Add(measurement);
        _latestMeasurements[packet.Origin] = measurement;

        _logger.LogDebug("Medição processada: Device={Device}, Seq={Seq}", packet.Origin, packet.Sequence);
    }

    private void ProcessModule2Event(BroadcastPacket packet)
    {
        var eventData = JsonSerializer.Deserialize<ProtectionEvent>(packet.Data);
        if (eventData == null) return;

        eventData.DeviceId = packet.Origin;

        var phase = eventData.Metadata != null && eventData.Metadata.TryGetValue("Phase", out var p)
            ? p.GetString() ?? "?"
            : "?";
        var eventKey = $"{packet.Origin}:{eventData.EventType}:{phase}";

        if (packet.OperationType == "EVENT_START")
        {
            eventData.IsActive = true;
            eventData.StartTime = packet.Timestamp;
            _activeEvents[eventKey] = eventData;
            _eventHistory.Add(eventData);
            _logger.LogInformation(
                "Evento iniciado: Device={Device}, Type={Type}, Phase={Phase}, Severity={Severity}",
                packet.Origin, eventData.EventType, phase, eventData.Severity);
        }
        else if (packet.OperationType == "EVENT_END")
        {
            if (_activeEvents.TryRemove(eventKey, out var activeEvent))
            {
                activeEvent.EndTime = packet.Timestamp;
                activeEvent.IsActive = false;
                _logger.LogInformation(
                    "Evento finalizado: Device={Device}, Type={Type}, Phase={Phase}, Severity={Severity}, Duration={Duration}ms",
                    packet.Origin, activeEvent.EventType, phase, activeEvent.Severity,
                    (activeEvent.EndTime - activeEvent.StartTime)?.TotalMilliseconds);
            }
            else
            {
                eventData.IsActive = false;
                eventData.EndTime = packet.Timestamp;
                _eventHistory.Add(eventData);
                _logger.LogInformation(
                    "Evento finalizado sem START em memória: Device={Device}, Type={Type}, Phase={Phase}",
                    packet.Origin, eventData.EventType, phase);
            }
        }
    }

    public List<ProtectionEvent> GetEventHistory(
        string? deviceId = null,
        int limit = 100,
        DateTime? startTime = null,
        DateTime? endTime = null)
        {
            var all = _eventHistory.GetAll();

            if (!string.IsNullOrEmpty(deviceId))
                all = all.Where(e => e.DeviceId == deviceId).ToList();

            if (startTime.HasValue)
                all = all.Where(e => (e.EndTime ?? e.StartTime) >= startTime.Value).ToList();

            if (endTime.HasValue)
                all = all.Where(e => e.StartTime <= endTime.Value).ToList();

            return all
                .OrderByDescending(e => e.EndTime ?? e.StartTime)
                .Take(limit)
                .ToList();
        }

    private void ProcessModule4Report(BroadcastPacket packet)
    {
        var report = JsonSerializer.Deserialize<FilteredEventReport>(packet.Data);
        if (report == null) return;

        report.ReportTimestamp = packet.Timestamp;
        _eventReports[report.EventType] = report;

        _logger.LogDebug("Relatório processado: Type={Type}, Count={Count}", report.EventType, report.TotalCount);
    }

    private void ProcessModule5Alarm(BroadcastPacket packet)
    {
        var alarm = JsonSerializer.Deserialize<AggregatedAlarm>(packet.Data, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (alarm == null) return;

        var eventId = alarm.CriticalEventId ?? Guid.NewGuid().ToString();

        if (_aggregatedAlarms.TryGetValue(eventId, out var existing))
        {
            existing.ClusterSize = alarm.ClusterSize;
            existing.LastOccurrence = DateTimeOffset.FromUnixTimeMilliseconds(alarm.Timestamp).UtcDateTime;
            existing.Local = alarm.Local;
        }
        else
        {
            alarm.FirstOccurrence = DateTimeOffset.FromUnixTimeMilliseconds(alarm.Timestamp).UtcDateTime;
            alarm.LastOccurrence = alarm.FirstOccurrence;
            alarm.Severity = DetermineSeverity(alarm.ClusterSize);
            alarm.AffectedDevices = new List<string>();
            _aggregatedAlarms[eventId] = alarm;
        }

        _logger.LogInformation("Alarme agregado: ID={Id}, Type={Type}, ClusterSize={Size}",
            eventId, alarm.CriticalEventType, alarm.ClusterSize);
    }

    private void ProcessModule6State(BroadcastPacket packet)
    {
        var stateData = JsonSerializer.Deserialize<Dictionary<string, string>>(packet.Data);
        if (stateData == null) return;

        var deviceId = stateData.GetValueOrDefault("DeviceId", packet.Origin);
        var currentState = stateData.GetValueOrDefault("State", "UNKNOWN");

        if (_pendingCommands.TryGetValue(deviceId, out var command))
        {
            command.CurrentState = currentState;
            command.ConfirmedAt = packet.Timestamp;
            command.IsPending = command.TargetState != currentState;

            if (!command.IsPending)
            {
                _pendingCommands.TryRemove(deviceId, out _);
                _logger.LogInformation("Comando confirmado: Device={Device}, State={State}", deviceId, currentState);
            }
        }
    }

    private string DetermineSeverity(int clusterSize) => clusterSize switch
    {
        >= 50 => "CRITICAL",
        >= 20 => "HIGH",
        >= 10 => "MEDIUM",
        _ => "LOW"
    };

    private void UpdateDeviceStatus(string deviceId, long sequence)
    {
        var status = _deviceStatuses.GetOrAdd(deviceId, _ => new DeviceStatus { DeviceId = deviceId });
        status.LastUpdate = DateTime.UtcNow;
        status.LastSequence = sequence;
        status.IsActive = true;
        status.IsStale = false;
        status.TimeSinceLastUpdate = TimeSpan.Zero;
    }

    public void CheckStaleDevices()
    {
        var now = DateTime.UtcNow;
        foreach (var status in _deviceStatuses.Values)
        {
            status.TimeSinceLastUpdate = now - status.LastUpdate;
            status.IsStale = status.TimeSinceLastUpdate > _staleThreshold;
            if (status.IsStale)
                status.IsActive = false;
        }
    }

    public List<MeasurementData> GetHistoricalData(string deviceId, DateTime startTime, DateTime endTime)
    {
        if (!_measurementBuffers.TryGetValue(deviceId, out var buffer))
            return new List<MeasurementData>();

        return buffer.GetAll()
            .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
            .OrderBy(m => m.Timestamp)
            .ToList();
    }

    public MeasurementData? GetLatestMeasurement(string deviceId)
    {
        _latestMeasurements.TryGetValue(deviceId, out var measurement);
        return measurement;
    }

    public List<ProtectionEvent> GetActiveEvents() => _activeEvents.Values.ToList();

    public List<DeviceStatus> GetAllDeviceStatuses()
    {
        CheckStaleDevices();
        return _deviceStatuses.Values.ToList();
    }

    public List<FilteredEventReport> GetEventReports() => _eventReports.Values.ToList();

    public List<AggregatedAlarm> GetAggregatedAlarms() => _aggregatedAlarms.Values.ToList();

    public void RegisterCommand(SwitchCommand command)
    {
        command.RequestedAt = DateTime.UtcNow;
        command.IsPending = true;
        _pendingCommands[command.DeviceId] = command;
    }

    public SwitchCommand? GetCommandStatus(string deviceId)
    {
        _pendingCommands.TryGetValue(deviceId, out var command);
        return command;
    }
}