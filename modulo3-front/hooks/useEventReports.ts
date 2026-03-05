import { useState, useEffect, useCallback, useRef } from 'react';
import { apiService } from '@/services/api.service';
import { FilteredEventReport } from '@/types/api';

export function useEventReports(interval: number = 5000, isPaused: boolean = false) {
  const [reports, setReports] = useState<FilteredEventReport[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const fetchReports = useCallback(async () => {
    if (isPaused) return;
    try {
      const response = await apiService.getEventReports();
      setReports(response);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [isPaused]);

  const clear = useCallback(() => {
    setReports([]);
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

    fetchReports();
    intervalRef.current = setInterval(fetchReports, interval);
    
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [fetchReports, interval, isPaused]);

  return { reports, loading, error, refetch: fetchReports, clear };
}
