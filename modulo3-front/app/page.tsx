'use client';

import { useState } from 'react';
import { useRealTimeData } from '@/hooks/useRealTimeData';
import { useDevices } from '@/hooks/useDevices';
import { useAlarms } from '@/hooks/useAlarms';
import { useEventReports } from '@/hooks/useEventReports';
import { DevicesOverview, DevicesTable } from '@/components/DeviceStatus';
import { ActiveEvents } from '@/components/ActiveEvents';
import { AlarmsOverview } from '@/components/AlarmsOverview';
import { RealTimeCharts } from '@/components/RealTimeCharts';
import { EventReportsCharts } from '@/components/EventReportsCharts';
import { MeasurementsTable } from '@/components/MeasurementsTable';
import { EventReportsTable } from '@/components/EventReportsTable';
import { CommandPanel } from '@/components/CommandPanel';
import { SystemMetrics } from '@/components/SystemMetrics';
import { DataControls } from '@/components/DataControls';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Activity, AlertCircle, Loader2, BarChart3, Zap, Bell, Settings } from 'lucide-react';

export default function Home() {
  const [isPaused, setIsPaused] = useState(false);

  const { data: realTimeData, loading: rtLoading, error: rtError, refetch: refetchRealTime, clear: clearRealTime } = useRealTimeData(2000, isPaused);
  const { devices, loading: devicesLoading, error: devicesError, refetch: refetchDevices, clear: clearDevices } = useDevices(3000, isPaused);
  const { alarms, loading: alarmsLoading, error: alarmsError, refetch: refetchAlarms, clear: clearAlarms } = useAlarms(5000, isPaused);
  const { reports, loading: reportsLoading, error: reportsError, refetch: refetchReports, clear: clearReports } = useEventReports(5000, isPaused);

  const isLoading = rtLoading && devicesLoading && alarmsLoading && reportsLoading;
  const hasError = rtError || devicesError || alarmsError || reportsError;

  const handleReload = () => {
    refetchRealTime();
    refetchDevices();
    refetchAlarms();
    refetchReports();
  };

  const handleClear = () => {
    clearRealTime();
    clearDevices();
    clearAlarms();
    clearReports();
  };

  const handleTogglePause = () => {
    setIsPaused(!isPaused);
  };

  return (
    <div className="min-h-screen bg-background">
      <header className="border-b">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Activity className="h-8 w-8 text-primary" />
              <div>
                <h1 className="text-2xl font-bold">Sistema de Monitoramento de Subestações</h1>
                <p className="text-sm text-muted-foreground">Monitoramento em tempo real via broadcast</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              {isLoading ? (
                <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
              ) : isPaused ? (
                <div className="flex items-center gap-2">
                  <div className="h-2 w-2 rounded-full bg-orange-500" />
                  <span className="text-sm text-muted-foreground">Pausado</span>
                </div>
              ) : (
                <div className="flex items-center gap-2">
                  <div className="h-2 w-2 rounded-full bg-green-500 animate-pulse" />
                  <span className="text-sm text-muted-foreground">Conectado</span>
                </div>
              )}
            </div>
          </div>
        </div>
      </header>

      <main className="container mx-auto px-4 py-6">
        {hasError && (
          <Alert variant="destructive" className="mb-6">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              Erro ao carregar dados: {rtError || devicesError || alarmsError || reportsError}
            </AlertDescription>
          </Alert>
        )}

        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <div className="text-center space-y-3">
              <Loader2 className="h-8 w-8 animate-spin mx-auto text-primary" />
              <p className="text-sm text-muted-foreground">Carregando dados da subestação...</p>
            </div>
          </div>
        ) : (
          <Tabs defaultValue="visao-geral" className="space-y-6">
            <TabsList className="grid w-full grid-cols-5 lg:w-auto">
              <TabsTrigger value="visao-geral" className="flex items-center gap-2">
                <BarChart3 className="h-4 w-4" />
                Visão Geral
              </TabsTrigger>
              <TabsTrigger value="tempo-real" className="flex items-center gap-2">
                <Zap className="h-4 w-4" />
                Tempo Real
              </TabsTrigger>
              <TabsTrigger value="eventos" className="flex items-center gap-2">
                <Activity className="h-4 w-4" />
                Eventos
              </TabsTrigger>
              <TabsTrigger value="alarmes" className="flex items-center gap-2">
                <Bell className="h-4 w-4" />
                Alarmes
              </TabsTrigger>
              <TabsTrigger value="controle" className="flex items-center gap-2">
                <Settings className="h-4 w-4" />
                Controle
              </TabsTrigger>
            </TabsList>

            <TabsContent value="visao-geral" className="space-y-6">
              <section>
                <h2 className="text-xl font-semibold mb-4">Métricas do Sistema</h2>
                <SystemMetrics measurements={realTimeData?.measurements || []} />
              </section>

              <section>
                <h2 className="text-xl font-semibold mb-4">Status dos Dispositivos</h2>
                <DevicesOverview devices={devices} />
              </section>

              <section>
                <h2 className="text-xl font-semibold mb-4">Dispositivos Conectados</h2>
                <DevicesTable devices={devices} />
              </section>
            </TabsContent>

            <TabsContent value="tempo-real" className="space-y-6">
              <section>
                <h2 className="text-xl font-semibold mb-4">Gráficos em Tempo Real</h2>
                <RealTimeCharts measurements={realTimeData?.measurements || []} />
              </section>

              <section>
                <h2 className="text-xl font-semibold mb-4">Eventos Ativos</h2>
                <ActiveEvents events={realTimeData?.activeEvents || []} />
              </section>

              <section>
                <h2 className="text-xl font-semibold mb-4">Tabela de Medições</h2>
                <MeasurementsTable measurements={realTimeData?.measurements || []} />
              </section>
            </TabsContent>

            <TabsContent value="eventos" className="space-y-6">
              <section>
                <h2 className="text-xl font-semibold mb-4">Análise de Eventos</h2>
                <EventReportsCharts reports={reports} />
              </section>

              <section>
                <h2 className="text-xl font-semibold mb-4">Relatórios de Eventos</h2>
                <EventReportsTable reports={reports} />
              </section>
            </TabsContent>

            <TabsContent value="alarmes" className="space-y-6">
              <section>
                <h2 className="text-xl font-semibold mb-4">Visão Geral de Alarmes</h2>
                <AlarmsOverview alarms={alarms} />
              </section>
            </TabsContent>

            <TabsContent value="controle" className="space-y-6">
              <section>
                <h2 className="text-xl font-semibold mb-4">Controles de Dados de Teste</h2>
                <DataControls
                  onReload={handleReload}
                  onClear={handleClear}
                  isPaused={isPaused}
                  onTogglePause={handleTogglePause}
                />
              </section>

              <section>
                <h2 className="text-xl font-semibold mb-4">Painel de Controle</h2>
                <CommandPanel deviceIds={devices.map(d => d.deviceId)} />
              </section>
            </TabsContent>
          </Tabs>
        )}
      </main>

      <footer className="border-t mt-12">
        <div className="container mx-auto px-4 py-4 text-center text-sm text-muted-foreground">
          Sistema de Monitoramento de Subestações - Módulo 3 - v1.0
        </div>
      </footer>
    </div>
  );
}
