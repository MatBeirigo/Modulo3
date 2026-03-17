'use client';

import { useState, useCallback } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DeviceStatusDto, HistoricalDataResponse, ProtectionEventDto } from '@/types/api';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { History, Search, Loader2, AlertCircle, Activity } from 'lucide-react';
import { format } from 'date-fns';
import { apiService } from '@/services/api.service';

interface HistoricalChartsProps {
  devices: DeviceStatusDto[];
}

function toLocalDatetimeValue(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

const CHART_SERIES = [
  { key: 'voltage',     label: 'Tensão (V)',        color: '#8884d8', domain: undefined as undefined },
  { key: 'current',     label: 'Corrente (A)',      color: '#82ca9d', domain: undefined as undefined },
  { key: 'frequency',   label: 'Frequência (Hz)',   color: '#ffc658', domain: ['dataMin - 1', 'dataMax + 1'] as [string, string] },
  { key: 'powerFactor', label: 'Fator de Potência', color: '#ff7300', domain: [0, 1] as [number, number] },
];

function MeasurementCharts({ result }: { result: HistoricalDataResponse }) {
  const chartData = result.dataPoints.map(m => ({
    time: format(new Date(m.timestamp), 'HH:mm:ss'),
    voltage: m.voltage,
    current: m.current,
    frequency: m.frequency,
    powerFactor: m.powerFactor,
  }));

  if (chartData.length === 0) {
    return (
      <p className="text-sm text-center py-4 text-muted-foreground">
        Nenhum ponto de dados no período.
      </p>
    );
  }

  return (
    <div className="space-y-2">
      <p className="text-xs text-muted-foreground">
        <strong>{result.totalPoints}</strong> ponto(s) · {format(new Date(result.startTime), 'dd/MM/yyyy HH:mm')} → {format(new Date(result.endTime), 'dd/MM/yyyy HH:mm')}
      </p>
      <div className="grid grid-cols-2 gap-3">
        {CHART_SERIES.map(({ key, label, color, domain }) => (
          <Card key={key} className="shadow-none border">
            <CardHeader className="pb-1 pt-3 px-4">
              <CardTitle className="text-xs font-medium text-muted-foreground uppercase tracking-wide">{label}</CardTitle>
            </CardHeader>
            <CardContent className="px-2 pb-3">
              <ResponsiveContainer width="100%" height={160}>
                <LineChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                  <XAxis dataKey="time" fontSize={10} tick={{ fill: '#888' }} />
                  <YAxis fontSize={10} tick={{ fill: '#888' }} domain={domain} width={40} />
                  <Tooltip contentStyle={{ fontSize: 12 }} />
                  <Line type="monotone" dataKey={key} stroke={color} name={label} dot={false} strokeWidth={2} />
                </LineChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

// ─── Module 1: Measurements ──────────────────────────────────────────────────

function MeasurementsHistory({ devices }: { devices: DeviceStatusDto[] }) {
  const now = new Date();
  const tenMinAgo = new Date(now.getTime() - 10 * 60 * 1000);

  const [selectedDevice, setSelectedDevice] = useState('');
  const [startTime, setStartTime] = useState(toLocalDatetimeValue(tenMinAgo));
  const [endTime, setEndTime] = useState(toLocalDatetimeValue(now));
  const [results, setResults] = useState<HistoricalDataResponse[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFetch = useCallback(async () => {
    setLoading(true);
    setError(null);
    setResults(null);
    const start = new Date(startTime).toISOString();
    const end = new Date(endTime).toISOString();
    try {
      const targets = selectedDevice ? [selectedDevice] : devices.map(d => d.deviceId);
      const fetched = await Promise.all(
        targets.map(id => apiService.getHistoricalData(id, start, end))
      );
      setResults(fetched);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro desconhecido');
    } finally {
      setLoading(false);
    }
  }, [selectedDevice, startTime, endTime, devices]);

  const canFetch = selectedDevice !== '' || devices.length > 0;

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <History className="h-4 w-4 text-primary" />
            Medições Históricas
            <Badge variant="outline" className="ml-auto text-xs font-normal">Module 1</Badge>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 gap-3">
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Dispositivo</label>
              <select
                className="border rounded-md px-3 py-2 text-sm bg-background w-full"
                value={selectedDevice}
                onChange={e => setSelectedDevice(e.target.value)}
              >
                <option value="">Todos os dispositivos</option>
                {devices.map(d => (
                  <option key={d.deviceId} value={d.deviceId}>{d.deviceId}</option>
                ))}
              </select>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="flex flex-col gap-1">
                <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Início</label>
                <input
                  type="datetime-local"
                  className="border rounded-md px-3 py-2 text-sm bg-background w-full"
                  value={startTime}
                  onChange={e => setStartTime(e.target.value)}
                />
              </div>
              <div className="flex flex-col gap-1">
                <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Fim</label>
                <input
                  type="datetime-local"
                  className="border rounded-md px-3 py-2 text-sm bg-background w-full"
                  value={endTime}
                  onChange={e => setEndTime(e.target.value)}
                />
              </div>
            </div>
          </div>
          <Button onClick={handleFetch} disabled={!canFetch || loading} className="w-full">
            {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Search className="h-4 w-4 mr-2" />}
            Buscar
          </Button>
        </CardContent>
      </Card>

      {error && (
        <div className="flex items-center gap-2 text-destructive text-sm p-3 bg-destructive/5 border border-destructive/30 rounded-lg">
          <AlertCircle className="h-4 w-4 shrink-0" />{error}
        </div>
      )}

      {results !== null && results.map(result => (
        <Card key={result.deviceId}>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm flex items-center gap-2">
              <span className="font-mono text-primary">{result.deviceId}</span>
              <Badge variant="secondary" className="text-xs">{result.totalPoints} pontos</Badge>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <MeasurementCharts result={result} />
          </CardContent>
        </Card>
      ))}

      {!loading && !error && results === null && (
        <div className="text-center py-10 text-muted-foreground text-sm">
          Selecione o período e clique em Buscar para visualizar as medições.
        </div>
      )}
    </div>
  );
}

// ─── Module 2: Event history ──────────────────────────────────────────────────

const SEVERITY_COLORS: Record<string, 'destructive' | 'default' | 'secondary' | 'outline'> = {
  Critical: 'destructive',
  High: 'destructive',
  Medium: 'default',
  Low: 'secondary',
};

function EventHistory({ devices }: { devices: DeviceStatusDto[] }) {
  const [selectedDevice, setSelectedDevice] = useState('');
  const [limit, setLimit] = useState(100);
  const [events, setEvents] = useState<ProtectionEventDto[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFetch = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await apiService.getEventHistory(selectedDevice || undefined, limit);
      setEvents(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro desconhecido');
    } finally {
      setLoading(false);
    }
  }, [selectedDevice, limit]);

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <Activity className="h-4 w-4 text-primary" />
            Histórico de Eventos
            <Badge variant="outline" className="ml-auto text-xs font-normal">Module 2</Badge>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 gap-3">
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Dispositivo</label>
              <select
                className="border rounded-md px-3 py-2 text-sm bg-background w-full"
                value={selectedDevice}
                onChange={e => setSelectedDevice(e.target.value)}
              >
                <option value="">Todos os dispositivos</option>
                {devices.map(d => (
                  <option key={d.deviceId} value={d.deviceId}>{d.deviceId}</option>
                ))}
              </select>
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Limite de resultados</label>
              <input
                type="number"
                min={1}
                max={500}
                className="border rounded-md px-3 py-2 text-sm bg-background w-full"
                value={limit}
                onChange={e => setLimit(Math.min(500, Math.max(1, Number(e.target.value))))}
              />
            </div>
          </div>
          <Button onClick={handleFetch} disabled={loading} className="w-full">
            {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Search className="h-4 w-4 mr-2" />}
            Buscar
          </Button>
        </CardContent>
      </Card>

      {error && (
        <div className="flex items-center gap-2 text-destructive text-sm p-3 bg-destructive/5 border border-destructive/30 rounded-lg">
          <AlertCircle className="h-4 w-4 shrink-0" />{error}
        </div>
      )}

      {events !== null && (
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm flex items-center gap-2">
              Resultados
              <Badge variant="secondary" className="text-xs">{events.length} evento(s)</Badge>
            </CardTitle>
          </CardHeader>
          <CardContent>
            {events.length === 0 ? (
              <p className="text-center py-8 text-muted-foreground text-sm">Nenhum evento encontrado.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-2 pr-3 text-xs font-medium text-muted-foreground uppercase">Dispositivo</th>
                      <th className="text-left py-2 pr-3 text-xs font-medium text-muted-foreground uppercase">Tipo</th>
                      <th className="text-left py-2 pr-3 text-xs font-medium text-muted-foreground uppercase">Severidade</th>
                      <th className="text-left py-2 pr-3 text-xs font-medium text-muted-foreground uppercase">Início</th>
                      <th className="text-left py-2 pr-3 text-xs font-medium text-muted-foreground uppercase">Fim</th>
                      <th className="text-left py-2 text-xs font-medium text-muted-foreground uppercase">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {events.map((e, i) => (
                      <tr key={i} className="border-b last:border-0 hover:bg-muted/40 transition-colors">
                        <td className="py-2 pr-3 font-mono text-xs">{e.deviceId}</td>
                        <td className="py-2 pr-3">{e.eventType}</td>
                        <td className="py-2 pr-3">
                          <Badge variant={SEVERITY_COLORS[e.severity] ?? 'secondary'}>
                            {e.severity}
                          </Badge>
                        </td>
                        <td className="py-2 pr-3 tabular-nums text-xs">
                          {format(new Date(e.startTime), 'dd/MM/yyyy HH:mm:ss')}
                        </td>
                        <td className="py-2 pr-3 tabular-nums text-xs">
                          {e.endTime ? format(new Date(e.endTime), 'dd/MM/yyyy HH:mm:ss') : '—'}
                        </td>
                        <td className="py-2">
                          <Badge variant={e.isActive ? 'destructive' : 'secondary'}>
                            {e.isActive ? 'Ativo' : 'Finalizado'}
                          </Badge>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {!loading && events === null && !error && (
        <div className="text-center py-10 text-muted-foreground text-sm">
          Clique em Buscar para carregar o histórico de eventos.
        </div>
      )}
    </div>
  );
}

// ─── Main component ───────────────────────────────────────────────────────────

export function HistoricalCharts({ devices }: HistoricalChartsProps) {
  return (
    <div className="grid grid-cols-1 xl:grid-cols-2 gap-8 items-start">
      <MeasurementsHistory devices={devices} />
      <EventHistory devices={devices} />
    </div>
  );
}
