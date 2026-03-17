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

// Module 6 - Relay types
export interface ConfigureModuleRequest {
  newId: number;
  uniqueId: string;
}

export interface ConfigureModuleResponse {
  message: string;
}

export interface RelayCommandResponse {
  message: string;
}

export interface RelayStateResponse {
  message: string;
}

export type RelayState = 'OPEN' | 'CLOSED' | 'UNKNOWN';

export interface Module6Status {
  moduleId: number;
  uniqueId: string;
  relayState: RelayState;
  lastUpdate: string;
  isOnline: boolean;
}

export interface Module6Info {
  uniqueId: string;
  moduleId?: number;
  lastSeen?: string;
}
