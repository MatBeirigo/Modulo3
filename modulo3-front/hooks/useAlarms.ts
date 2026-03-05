import { useState, useEffect, useCallback, useRef } from 'react';
import { apiService } from '@/services/api.service';
import { AggregatedAlarm } from '@/types/api';

export function useAlarms(interval: number = 5000, isPaused: boolean = false) {
  const [alarms, setAlarms] = useState<AggregatedAlarm[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const fetchAlarms = useCallback(async () => {
    if (isPaused) return;
    try {
      const response = await apiService.getAlarms();
      setAlarms(response);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [isPaused]);

  const clear = useCallback(() => {
    setAlarms([]);
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

    fetchAlarms();
    intervalRef.current = setInterval(fetchAlarms, interval);
    
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [fetchAlarms, interval, isPaused]);

  return { alarms, loading, error, refetch: fetchAlarms, clear };
}
