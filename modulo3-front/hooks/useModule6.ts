'use client';

import { useState, useEffect, useCallback } from 'react';
import { apiService } from '@/services/api.service';
import { Module6Status } from '@/types/api';

export function useModule6(pollingInterval: number = 3000, isPaused: boolean = false) {
  const [unconfiguredModules, setUnconfiguredModules] = useState<string[]>([]);
  const [moduleStates, setModuleStates] = useState<Module6Status[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchUnconfiguredModules = useCallback(async () => {
    try {
      const modules = await apiService.getUnconfiguredModules();
      setUnconfiguredModules(modules);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao buscar módulos');
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchModuleStates = useCallback(async () => {
    try {
      const states = await apiService.getModuleStates();
      setModuleStates(states);
      setError(null);
    } catch (err) {
      // Silently fail for state updates if endpoint doesn't exist yet
      console.warn('Erro ao buscar estados dos módulos:', err);
    }
  }, []);

  const fetchAll = useCallback(async () => {
    await Promise.all([
      fetchUnconfiguredModules(),
      fetchModuleStates(),
    ]);
  }, [fetchUnconfiguredModules, fetchModuleStates]);

  useEffect(() => {
    if (isPaused) return;

    fetchAll();
    const interval = setInterval(fetchAll, pollingInterval);

    return () => clearInterval(interval);
  }, [fetchAll, pollingInterval, isPaused]);

  const configureModule = useCallback(async (newId: number, uniqueId: string) => {
    try {
      const response = await apiService.configureModule({ newId, uniqueId });
      // Refresh the list after configuration
      await fetchAll();
      return { success: true, message: response.message };
    } catch (err) {
      return { 
        success: false, 
        message: err instanceof Error ? err.message : 'Erro ao configurar módulo' 
      };
    }
  }, [fetchAll]);

  const closeRelay = useCallback(async (moduleId: number) => {
    try {
      const response = await apiService.closeRelay(moduleId);
      // Refresh states after command
      setTimeout(() => fetchModuleStates(), 1000);
      return { success: true, message: response.message };
    } catch (err) {
      return { 
        success: false, 
        message: err instanceof Error ? err.message : 'Erro ao fechar relé' 
      };
    }
  }, [fetchModuleStates]);

  const openRelay = useCallback(async (moduleId: number) => {
    try {
      const response = await apiService.openRelay(moduleId);
      // Refresh states after command
      setTimeout(() => fetchModuleStates(), 1000);
      return { success: true, message: response.message };
    } catch (err) {
      return { 
        success: false, 
        message: err instanceof Error ? err.message : 'Erro ao abrir relé' 
      };
    }
  }, [fetchModuleStates]);

  const checkRelayState = useCallback(async (moduleId: number) => {
    try {
      const response = await apiService.getRelayState(moduleId);
      // Refresh states after query (response comes via UDP)
      setTimeout(() => fetchModuleStates(), 1500);
      return { success: true, message: response.message };
    } catch (err) {
      return { 
        success: false, 
        message: err instanceof Error ? err.message : 'Erro ao consultar estado' 
      };
    }
  }, [fetchModuleStates]);

  return {
    unconfiguredModules,
    moduleStates,
    loading,
    error,
    configureModule,
    closeRelay,
    openRelay,
    checkRelayState,
    refetch: fetchAll,
  };
}
