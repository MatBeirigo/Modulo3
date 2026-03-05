using System.Text.Json.Serialization;

namespace Core.DTO;

public class RealTimeDataResponse
{
    public List<MeasurementDataDto> Measurements { get; set; }
    public List<DeviceStatusDto> DeviceStatuses { get; set; }
    public List<ProtectionEventDto> ActiveEvents { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MeasurementDataDto
{
    public string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Voltage { get; set; }
    public double Current { get; set; }
    public double Frequency { get; set; }
    public double PowerFactor { get; set; }
    public long Sequence { get; set; }
}

public class HistoricalDataResponse
{
    public string DeviceId { get; set; }
    public List<MeasurementDataDto> DataPoints { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalPoints { get; set; }
}

public class ProtectionEventDto
{
    public string DeviceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string EventType { get; set; }
    public string Severity { get; set; }
    public bool IsActive { get; set; }
}

public class DeviceStatusDto
{
    public string DeviceId { get; set; }
    public DateTime LastUpdate { get; set; }
    public bool IsActive { get; set; }
    public bool IsStale { get; set; }
    public int SecondsSinceLastUpdate { get; set; }
}

public class CommandRequest
{
    public string DeviceId { get; set; }
    public string CommandType { get; set; }
    public string TargetState { get; set; }
}

public class CommandResponse
{
    public string CommandId { get; set; }
    public string Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public string Message { get; set; }
}

public class AggregatedAlarmDto
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string Location { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
    public int EventCount { get; set; }
    public string Severity { get; set; }
    public List<string> AffectedDevices { get; set; }
}