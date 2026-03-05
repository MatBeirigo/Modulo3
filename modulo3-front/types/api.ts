export interface AggregatedAlarm {
  eventId: string;
  eventType: string;
  location: string;
  latitude: number;
  longitude: number;
  firstOccurrence: string;
  lastOccurrence: string;
  eventCount: number;
  severity: string;
  affectedDevices: string[];
}

export interface CommandRequest {
  deviceId: string;
  commandType: string;
  targetState: string;
}

export interface CommandResponse {
  commandId: string;
  status: string;
  issuedAt: string;
  message: string;
}

export interface DeviceStatusDto {
  deviceId: string;
  lastUpdate: string;
  isActive: boolean;
  isStale: boolean;
  secondsSinceLastUpdate: number;
}

export interface FilteredEventReport {
  eventType: string;
  totalCount: number;
  countByDevice: Record<string, number>;
  reportTimestamp: string;
  windowDuration: string;
}

export interface MeasurementDataDto {
  deviceId: string;
  timestamp: string;
  voltage: number;
  current: number;
  frequency: number;
  powerFactor: number;
  sequence: number;
}

export interface ProtectionEventDto {
  deviceId: string;
  startTime: string;
  endTime?: string;
  eventType: string;
  severity: string;
  isActive: boolean;
}

export interface RealTimeDataResponse {
  measurements: MeasurementDataDto[];
  deviceStatuses: DeviceStatusDto[];
  activeEvents: ProtectionEventDto[];
  timestamp: string;
}

export interface SwitchCommand {
  deviceId: string;
  commandType: string;
  targetState: string;
  requestedAt: string;
  confirmedAt?: string;
  currentState: string;
  isPending: boolean;
}

export interface HistoricalDataResponse {
  deviceId: string;
  dataPoints: MeasurementDataDto[];
  startTime: string;
  endTime: string;
  totalPoints: number;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
}
