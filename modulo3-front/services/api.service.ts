import {
  RealTimeDataResponse,
  DeviceStatusDto,
  ProtectionEventDto,
  AggregatedAlarm,
  FilteredEventReport,
  HistoricalDataResponse,
  CommandRequest,
  CommandResponse,
  SwitchCommand,
} from '@/types/api';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5151';

class ApiService {
  private async fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
    });

    if (!response.ok) {
      throw new Error(`API Error: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }

  async getRealTimeData(): Promise<RealTimeDataResponse> {
    return this.fetchApi<RealTimeDataResponse>('/api/Monitoring/realtime');
  }

  async getDevices(): Promise<DeviceStatusDto[]> {
    return this.fetchApi<DeviceStatusDto[]>('/api/Monitoring/devices');
  }

  async getActiveEvents(): Promise<ProtectionEventDto[]> {
    return this.fetchApi<ProtectionEventDto[]>('/api/Monitoring/events/active');
  }

  async getAlarms(): Promise<AggregatedAlarm[]> {
    return this.fetchApi<AggregatedAlarm[]>('/api/Monitoring/alarms');
  }

  async getEventReports(): Promise<FilteredEventReport[]> {
    return this.fetchApi<FilteredEventReport[]>('/api/Monitoring/events/reports');
  }

  async getHistoricalData(
    deviceId: string,
    startTime?: string,
    endTime?: string
  ): Promise<HistoricalDataResponse> {
    const params = new URLSearchParams();
    if (startTime) params.append('startTime', startTime);
    if (endTime) params.append('endTime', endTime);
    
    const query = params.toString() ? `?${params.toString()}` : '';
    return this.fetchApi<HistoricalDataResponse>(
      `/api/Monitoring/historical/${deviceId}${query}`
    );
  }

  async sendCommand(command: CommandRequest): Promise<CommandResponse> {
    return this.fetchApi<CommandResponse>('/api/Command/send', {
      method: 'POST',
      body: JSON.stringify(command),
    });
  }

  async getCommandStatus(deviceId: string): Promise<SwitchCommand> {
    return this.fetchApi<SwitchCommand>(`/api/Command/status/${deviceId}`);
  }

  async checkHealth(): Promise<boolean> {
    try {
      const response = await fetch(`${API_BASE_URL}/health`);
      return response.ok;
    } catch {
      return false;
    }
  }
}

export const apiService = new ApiService();
