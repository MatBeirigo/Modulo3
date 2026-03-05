import { useState, useEffect, useCallback, useRef } from 'react';
import { apiService } from '@/services/api.service';
import { RealTimeDataResponse } from '@/types/api';

export function useRealTimeData(interval: number = 2000, isPaused: boolean = false) {
  const [data, setData] = useState<RealTimeDataResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const fetchData = useCallback(async () => {
    if (isPaused) return;
    try {
      const response = await apiService.getRealTimeData();
      setData(response);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [isPaused]);

  const clear = useCallback(() => {
    setData(null);
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

  return { data, loading, error, refetch: fetchData, clear };
}
