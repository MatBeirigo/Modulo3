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
import { Module6Manager } from '@/components/Module6Manager';
import { HistoricalCharts } from '@/components/HistoricalCharts';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Activity, AlertCircle, Loader2, BarChart3, Zap, Bell, Radio, History } from 'lucide-react';

export default function Home() {
  const [isPaused, setIsPaused] = useState(false);

  const { data: realTimeData, measurementHistory, loading: rtLoading, error: rtError } = useRealTimeData(2000, isPaused);
  const { devices, loading: devicesLoading, error: devicesError } = useDevices(3000, isPaused);
  const { alarms, loading: alarmsLoading, error: alarmsError } = useAlarms(5000, isPaused);
  const { reports, loading: reportsLoading, error: reportsError } = useEventReports(5000, isPaused);

  const isLoading = rtLoading && devicesLoading && alarmsLoading && reportsLoading;
  const hasError = rtError || devicesError || alarmsError || reportsError;

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
                Tempo Real (M1)
              </TabsTrigger>
              {/* <TabsTrigger value="eventos" className="flex items-center gap-2">
                <Activity className="h-4 w-4" />
                Eventos (M2)
              </TabsTrigger> */}
              <TabsTrigger value="alarmes" className="flex items-center gap-2">
                <Bell className="h-4 w-4" />
                Alarmes (M5)
              </TabsTrigger>
              <TabsTrigger value="modulo6" className="flex items-center gap-2">
                <Radio className="h-4 w-4" />
                Relés (M6)
              </TabsTrigger>
              <TabsTrigger value="historico" className="flex items-center gap-2">
                <History className="h-4 w-4" />
                Histórico
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
                <RealTimeCharts measurements={measurementHistory} />
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

            <TabsContent value="modulo6" className="space-y-6">
              <section>
                <h2 className="text-xl font-semibold mb-4">Módulo 6 - Gerenciamento de Relés</h2>
                <Module6Manager isPaused={isPaused} />
              </section>
            </TabsContent>

            <TabsContent value="historico" className="space-y-6">
              <HistoricalCharts devices={devices} />
            </TabsContent>


          </Tabs>
        )}
      </main>

      <footer className="border-t mt-12">
        <div className="container mx-auto px-4 py-4 text-center text-sm text-muted-foreground">
          Sistema de Monitoramento de Subestações - Módulo 3 - v2.0
        </div>
      </footer>
    </div>
  );
}