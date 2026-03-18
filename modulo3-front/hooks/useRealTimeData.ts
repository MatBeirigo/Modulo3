import { useState, useEffect, useCallback, useRef } from 'react';
import { apiService } from '@/services/api.service';
import { MeasurementDataDto, RealTimeDataResponse } from '@/types/api';

const MAX_HISTORY = 100;

export function useRealTimeData(interval: number = 2000, isPaused: boolean = false) {
  const [data, setData] = useState<RealTimeDataResponse | null>(null);
  const [measurementHistory, setMeasurementHistory] = useState<MeasurementDataDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const fetchData = useCallback(async () => {
    if (isPaused) return;
    try {
      const response = await apiService.getRealTimeData();
      setData(response);
      setMeasurementHistory(prev => {
        const existingKeys = new Set(prev.map(m => `${m.deviceId}-${m.sequence}`));
        const newPoints = response.measurements.filter(
          m => !existingKeys.has(`${m.deviceId}-${m.sequence}`)
        );
        if (newPoints.length === 0) return prev;
        const combined = [...prev, ...newPoints];
        return combined.length > MAX_HISTORY ? combined.slice(-MAX_HISTORY) : combined;
      });
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [isPaused]);

  const clear = useCallback(() => {
    setData(null);
    setMeasurementHistory([]);
    setLoading(false);
    setError(null);
  }, []);

  useEffect(() => {
    if (isPaused) {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
      return;
    }

    fetchData();
    intervalRef.current = setInterval(fetchData, interval);
    
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [fetchData, interval, isPaused]);

  return { data, measurementHistory, loading, error, refetch: fetchData, clear };
}
