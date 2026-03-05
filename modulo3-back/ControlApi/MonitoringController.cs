using Microsoft.AspNetCore.Mvc;
using Services;
using Core.DTO;
using Core.Models;

namespace ControlApi;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Monitoramento")]
public class MonitoringController : ControllerBase
{
    private readonly DataAggregationService _aggregationService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(DataAggregationService aggregationService, ILogger<MonitoringController> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    [HttpGet("realtime")]
    [ProducesResponseType(typeof(RealTimeDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<RealTimeDataResponse> GetRealTimeData()
    {
        var deviceStatuses = _aggregationService.GetAllDeviceStatuses();
        var measurements = deviceStatuses
            .Where(d => d.IsActive)
            .Select(d => _aggregationService.GetLatestMeasurement(d.DeviceId))
            .Where(m => m != null)
            .Select(m => new MeasurementDataDto
            {
                DeviceId = m.DeviceId,
                Timestamp = m.Timestamp,
                Voltage = m.Voltage,
                Current = m.Current,
                Frequency = m.Frequency,
                PowerFactor = m.PowerFactor,
                Sequence = m.Sequence
            })
            .ToList();

        var activeEvents = _aggregationService.GetActiveEvents()
            .Select(e => new ProtectionEventDto
            {
                DeviceId = e.DeviceId,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                EventType = e.EventType,
                Severity = e.Severity,
                IsActive = e.IsActive
            })
            .ToList();

        var statuses = deviceStatuses.Select(s => new DeviceStatusDto
        {
            DeviceId = s.DeviceId,
            LastUpdate = s.LastUpdate,
            IsActive = s.IsActive,
            IsStale = s.IsStale,
            SecondsSinceLastUpdate = (int)s.TimeSinceLastUpdate.TotalSeconds
        }).ToList();

        return Ok(new RealTimeDataResponse
        {
            Measurements = measurements,
            DeviceStatuses = statuses,
            ActiveEvents = activeEvents,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("historical/{deviceId}")]
    [ProducesResponseType(typeof(HistoricalDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<HistoricalDataResponse> GetHistoricalData(
        string deviceId,
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime)
    {
        if (string.IsNullOrEmpty(deviceId))
            return BadRequest(new { Message = "DeviceId é obrigatório" });

        var start = startTime ?? DateTime.UtcNow.AddMinutes(-10);
        var end = endTime ?? DateTime.UtcNow;

        if (start > end)
            return BadRequest(new { Message = "StartTime não pode ser maior que EndTime" });

        var data = _aggregationService.GetHistoricalData(deviceId, start, end);

        var response = new HistoricalDataResponse
        {
            DeviceId = deviceId,
            DataPoints = data.Select(m => new MeasurementDataDto
            {
                DeviceId = m.DeviceId,
                Timestamp = m.Timestamp,
                Voltage = m.Voltage,
                Current = m.Current,
                Frequency = m.Frequency,
                PowerFactor = m.PowerFactor,
                Sequence = m.Sequence
            }).ToList(),
            StartTime = start,
            EndTime = end,
            TotalPoints = data.Count
        };

        return Ok(response);
    }

    [HttpGet("devices")]
    [ProducesResponseType(typeof(List<DeviceStatusDto>), StatusCodes.Status200OK)]
    public ActionResult<List<DeviceStatusDto>> GetDeviceStatuses()
    {
        var statuses = _aggregationService.GetAllDeviceStatuses()
            .Select(s => new DeviceStatusDto
            {
                DeviceId = s.DeviceId,
                LastUpdate = s.LastUpdate,
                IsActive = s.IsActive,
                IsStale = s.IsStale,
                SecondsSinceLastUpdate = (int)s.TimeSinceLastUpdate.TotalSeconds
            })
            .ToList();

        return Ok(statuses);
    }

    [HttpGet("events/active")]
    [ProducesResponseType(typeof(List<ProtectionEventDto>), StatusCodes.Status200OK)]
    public ActionResult<List<ProtectionEventDto>> GetActiveEvents()
    {
        var events = _aggregationService.GetActiveEvents()
            .Select(e => new ProtectionEventDto
            {
                DeviceId = e.DeviceId,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                EventType = e.EventType,
                Severity = e.Severity,
                IsActive = e.IsActive
            })
            .ToList();

        return Ok(events);
    }

    [HttpGet("events/reports")]
    [ProducesResponseType(typeof(List<FilteredEventReport>), StatusCodes.Status200OK)]
    public ActionResult<List<FilteredEventReport>> GetEventReports()
    {
        var reports = _aggregationService.GetEventReports();
        return Ok(reports);
    }

    [HttpGet("alarms")]
    [ProducesResponseType(typeof(List<AggregatedAlarmDto>), StatusCodes.Status200OK)]
    public ActionResult<List<AggregatedAlarmDto>> GetAggregatedAlarms()
    {
        var alarms = _aggregationService.GetAggregatedAlarms()
            .Select(a => new AggregatedAlarmDto
            {
                EventId = a.CriticalEventId,
                EventType = a.CriticalEventType,
                Location = a.LocationString,
                Latitude = a.Local?[0] ?? 0,
                Longitude = a.Local?[1] ?? 0,
                FirstOccurrence = a.FirstOccurrence,
                LastOccurrence = a.LastOccurrence,
                EventCount = a.ClusterSize,
                Severity = a.Severity,
                AffectedDevices = a.AffectedDevices ?? new List<string>()
            })
            .ToList();

        return Ok(alarms);
    }
}