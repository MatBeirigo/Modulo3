import { useState, useCallback } from 'react';
import { apiService } from '@/services/api.service';
import { HistoricalDataResponse } from '@/types/api';

export function useHistoricalData() {
  const [data, setData] = useState<HistoricalDataResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchHistoricalData = useCallback(async (
    deviceId: string,
    startTime?: string,
    endTime?: string
  ) => {
    setLoading(true);
    setError(null);
    try {
      const response = await apiService.getHistoricalData(deviceId, startTime, endTime);
      setData(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, []);

  return { data, loading, error, fetchHistoricalData };
}
