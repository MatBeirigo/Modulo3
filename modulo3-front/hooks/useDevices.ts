import { useState, useEffect, useCallback, useRef } from 'react';
import { apiService } from '@/services/api.service';
import { DeviceStatusDto } from '@/types/api';

export function useDevices(interval: number = 3000, isPaused: boolean = false) {
  const [devices, setDevices] = useState<DeviceStatusDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const fetchDevices = useCallback(async () => {
    if (isPaused) return;
    try {
      const response = await apiService.getDevices();
      setDevices(response);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [isPaused]);

  const clear = useCallback(() => {
    setDevices([]);
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

    fetchDevices();
    intervalRef.current = setInterval(fetchDevices, interval);
    
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [fetchDevices, interval, isPaused]);

  return { devices, loading, error, refetch: fetchDevices, clear };
}
