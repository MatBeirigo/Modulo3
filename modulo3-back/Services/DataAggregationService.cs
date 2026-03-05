using Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Services;

public class DataAggregationService
{
    private readonly ILogger<DataAggregationService> _logger;
    private readonly ConcurrentDictionary<string, CircularBuffer<MeasurementData>> _measurementBuffers;
    private readonly ConcurrentDictionary<string, MeasurementData> _latestMeasurements;
    private readonly ConcurrentDictionary<string, ProtectionEvent> _activeEvents;
    private readonly ConcurrentDictionary<string, DeviceStatus> _deviceStatuses;
    private readonly ConcurrentDictionary<string, FilteredEventReport> _eventReports;
    private readonly ConcurrentDictionary<string, AggregatedAlarm> _aggregatedAlarms;
    private readonly ConcurrentDictionary<string, SwitchCommand> _pendingCommands;
    private readonly TimeSpan _staleThreshold = TimeSpan.FromSeconds(10);
    private const int BufferSize = 1000;

    public DataAggregationService(ILogger<DataAggregationService> logger)
    {
        _logger = logger;
        _measurementBuffers = new ConcurrentDictionary<string, CircularBuffer<MeasurementData>>();
        _latestMeasurements = new ConcurrentDictionary<string, MeasurementData>();
        _activeEvents = new ConcurrentDictionary<string, ProtectionEvent>();
        _deviceStatuses = new ConcurrentDictionary<string, DeviceStatus>();
        _eventReports = new ConcurrentDictionary<string, FilteredEventReport>();
        _aggregatedAlarms = new ConcurrentDictionary<string, AggregatedAlarm>();
        _pendingCommands = new ConcurrentDictionary<string, SwitchCommand>();
    }

    public async Task ProcessPacket(BroadcastPacket packet)
    {
        await Task.Run(() =>
        {
            try
            {
                switch (packet.Module)
                {
                    case "MODULE1":
                        ProcessModule1Measurement(packet);
                        break;
                    case "MODULE2":
                        ProcessModule2Event(packet);
                        break;
                    case "MODULE4":
                        ProcessModule4Report(packet);
                        break;
                    case "MODULE5":
                        ProcessModule5Alarm(packet);
                        break;
                    case "MODULE6":
                        ProcessModule6State(packet);
                        break;
                    default:
                        _logger.LogWarning("Módulo desconhecido: {Module}", packet.Module);
                        break;
                }

                UpdateDeviceStatus(packet.Origin, packet.Sequence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar pacote do módulo {Module}", packet.Module);
            }
        });
    }

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

        if (packet.OperationType == "EVENT_START")
        {
            eventData.IsActive = true;
            eventData.StartTime = packet.Timestamp;
            _activeEvents[packet.Origin] = eventData;
            _logger.LogInformation("Evento de proteção iniciado: Device={Device}, Type={Type}", packet.Origin, eventData.EventType);
        }
        else if (packet.OperationType == "EVENT_END")
        {
            if (_activeEvents.TryRemove(packet.Origin, out var activeEvent))
            {
                activeEvent.EndTime = packet.Timestamp;
                activeEvent.IsActive = false;
                _logger.LogInformation("Evento de proteção finalizado: Device={Device}, Duration={Duration}ms",
                    packet.Origin, (activeEvent.EndTime - activeEvent.StartTime)?.TotalMilliseconds);
            }
        }
    }

    private void ProcessModule4Report(BroadcastPacket packet)
    {
        var report = JsonSerializer.Deserialize<FilteredEventReport>(packet.Data);
        if (report == null) return;

        report.ReportTimestamp = packet.Timestamp;
        _eventReports[report.EventType] = report;

        _logger.LogDebug("Relatório de eventos processado: Type={Type}, Count={Count}", report.EventType, report.TotalCount);
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

        _logger.LogInformation("Alarme agregado: ID={Id}, Type={Type}, ClusterSize={Size}, Location=[{Lat}, {Long}]",
            eventId, alarm.CriticalEventType, alarm.ClusterSize,
            alarm.Local?[0] ?? 0, alarm.Local?[1] ?? 0);
    }

    private string DetermineSeverity(int clusterSize)
    {
        return clusterSize switch
        {
            >= 50 => "CRITICAL",
            >= 20 => "HIGH",
            >= 10 => "MEDIUM",
            _ => "LOW"
        };
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
            {
                status.IsActive = false;
            }
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

    public MeasurementData GetLatestMeasurement(string deviceId)
    {
        _latestMeasurements.TryGetValue(deviceId, out var measurement);
        return measurement;
    }

    public List<ProtectionEvent> GetActiveEvents()
    {
        return _activeEvents.Values.ToList();
    }

    public List<DeviceStatus> GetAllDeviceStatuses()
    {
        CheckStaleDevices();
        return _deviceStatuses.Values.ToList();
    }

    public List<FilteredEventReport> GetEventReports()
    {
        return _eventReports.Values.ToList();
    }

    public List<AggregatedAlarm> GetAggregatedAlarms()
    {
        return _aggregatedAlarms.Values.ToList();
    }

    public void RegisterCommand(SwitchCommand command)
    {
        command.RequestedAt = DateTime.UtcNow;
        command.IsPending = true;
        _pendingCommands[command.DeviceId] = command;
    }

    public SwitchCommand GetCommandStatus(string deviceId)
    {
        _pendingCommands.TryGetValue(deviceId, out var command);
        return command;
    }
}

public class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private readonly int _capacity;
    private int _head;
    private int _count;
    private readonly object _lock = new object();

    public CircularBuffer(int capacity)
    {
        _capacity = capacity;
        _buffer = new T[capacity];
        _head = 0;
        _count = 0;
    }

    public void Add(T item)
    {
        lock (_lock)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _capacity;
            if (_count < _capacity)
                _count++;
        }
    }

    public List<T> GetAll()
    {
        lock (_lock)
        {
            var result = new List<T>(_count);
            var start = _count < _capacity ? 0 : _head;
            for (int i = 0; i < _count; i++)
            {
                result.Add(_buffer[(start + i) % _capacity]);
            }
            return result;
        }
    }
}