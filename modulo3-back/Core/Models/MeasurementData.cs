using System.Text.Json.Serialization;

namespace Core.Models;

public class MeasurementData
{
    public string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Voltage { get; set; }
    public double Current { get; set; }
    public double Frequency { get; set; }
    public double PowerFactor { get; set; }
    public long Sequence { get; set; }
    public string Status { get; set; }
}

public class ProtectionEvent
{
    public string DeviceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string EventType { get; set; }
    public string Severity { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public class FilteredEventReport
{
    public string EventType { get; set; }
    public int TotalCount { get; set; }
    public Dictionary<string, int> CountByDevice { get; set; }
    public DateTime ReportTimestamp { get; set; }
    public TimeSpan WindowDuration { get; set; }
}

public class AggregatedAlarm
{
    [JsonPropertyName("critical_event_id")]
    public string CriticalEventId { get; set; }

    [JsonPropertyName("critical_event_type")]
    public string CriticalEventType { get; set; }

    [JsonPropertyName("local")]
    public double[] Local { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("cluster_size")]
    public int ClusterSize { get; set; }

    [JsonIgnore]
    public DateTime FirstOccurrence { get; set; }

    [JsonIgnore]
    public DateTime LastOccurrence { get; set; }

    [JsonIgnore]
    public string Severity { get; set; }

    [JsonIgnore]
    public List<string> AffectedDevices { get; set; }

    [JsonIgnore]
    public string AlarmType => CriticalEventType;

    [JsonIgnore]
    public int OccurrenceCount => ClusterSize;

    [JsonIgnore]
    public string LocationString => Local != null && Local.Length == 2
        ? $"Lat: {Local[0]:F4}, Long: {Local[1]:F4}"
        : "Desconhecido";
}

public class SwitchCommand
{
    public string DeviceId { get; set; }
    public string CommandType { get; set; }
    public string TargetState { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string CurrentState { get; set; }
    public bool IsPending { get; set; }
}

public class DeviceStatus
{
    public string DeviceId { get; set; }
    public DateTime LastUpdate { get; set; }
    public bool IsActive { get; set; }
    public bool IsStale { get; set; }
    public TimeSpan TimeSinceLastUpdate { get; set; }
    public long LastSequence { get; set; }
}