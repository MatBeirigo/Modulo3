'use client';

import { useMemo } from 'react';
import dynamic from 'next/dynamic';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { AggregatedAlarm } from '@/types/api';
import { Bell, MapPin, TrendingUp } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';
import { ptBR } from 'date-fns/locale';

const AlarmMap = dynamic(() => import('./AlarmMap').then(m => m.AlarmMap), {
  ssr: false,
  loading: () => (
    <div className="flex items-center justify-center h-[420px] text-muted-foreground text-sm">
      Carregando mapa...
    </div>
  ),
});

// ─── Severity config ──────────────────────────────────────────────────────────

const SEVERITY_ORDER = ['CRITICAL', 'HIGH', 'MEDIUM', 'LOW'];

const SEVERITY_CONFIG: Record<string, { color: string; label: string; variant: 'destructive' | 'default' | 'secondary' | 'outline' }> = {
  CRITICAL: { color: '#dc2626', label: 'Crítico', variant: 'destructive' },
  HIGH:     { color: '#ea580c', label: 'Alto',    variant: 'destructive' },
  MEDIUM:   { color: '#d97706', label: 'Médio',   variant: 'default'     },
  LOW:      { color: '#16a34a', label: 'Baixo',   variant: 'secondary'   },
};

const getSeverityConfig = (s: string) =>
  SEVERITY_CONFIG[s] ?? { color: '#6b7280', label: s, variant: 'secondary' as const };

// ─── Types ────────────────────────────────────────────────────────────────────

interface GroupedAlarm {
  eventType: string;
  totalCount: number;
  severity: string;
  firstOccurrence: string;
  lastOccurrence: string;
  locations: number;
  _severityRank: number;
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function groupAlarms(alarms: AggregatedAlarm[]): GroupedAlarm[] {
  const map = new Map<string, GroupedAlarm>();

  for (const alarm of alarms) {
    const existing = map.get(alarm.eventType);
    const rank = SEVERITY_ORDER.indexOf(alarm.severity);
    const effectiveRank = rank === -1 ? 99 : rank;
    if (existing) {
      existing.totalCount += alarm.eventCount;
      existing.locations += 1;
      if (new Date(alarm.firstOccurrence) < new Date(existing.firstOccurrence))
        existing.firstOccurrence = alarm.firstOccurrence;
      if (new Date(alarm.lastOccurrence) > new Date(existing.lastOccurrence))
        existing.lastOccurrence = alarm.lastOccurrence;
      if (effectiveRank < existing._severityRank) {
        existing.severity = alarm.severity;
        existing._severityRank = effectiveRank;
      }
    } else {
      map.set(alarm.eventType, {
        eventType: alarm.eventType,
        totalCount: alarm.eventCount,
        severity: alarm.severity,
        firstOccurrence: alarm.firstOccurrence,
        lastOccurrence: alarm.lastOccurrence,
        locations: 1,
        _severityRank: effectiveRank,
      });
    }
  }

  return Array.from(map.values()).sort((a, b) => b.totalCount - a.totalCount);
}

// ─── Main component ───────────────────────────────────────────────────────────

interface AlarmsOverviewProps {
  alarms: AggregatedAlarm[];
}

export function AlarmsOverview({ alarms }: AlarmsOverviewProps) {
  const grouped = useMemo(() => groupAlarms(alarms), [alarms]);
  const totalOccurrences = alarms.reduce((sum, a) => sum + a.eventCount, 0);

  const severitySummary = useMemo(() => {
    const map = new Map<string, number>();
    for (const a of alarms) map.set(a.severity, (map.get(a.severity) ?? 0) + a.eventCount);
    return SEVERITY_ORDER.filter(s => map.has(s)).map(s => ({ severity: s, count: map.get(s)! }));
  }, [alarms]);

  if (alarms.length === 0) {
    return (
      <Card>
        <CardContent className="text-center py-12 text-muted-foreground">
          <Bell className="h-10 w-10 mx-auto mb-3 opacity-40" />
          <p>Nenhum alarme registrado</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="pt-4 pb-4">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Total de Ocorrências</p>
            <p className="text-3xl font-bold mt-1">{totalOccurrences}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4 pb-4">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Tipos de Alarme</p>
            <p className="text-3xl font-bold mt-1">{grouped.length}</p>
          </CardContent>
        </Card>
        {severitySummary.slice(0, 2).map(({ severity, count }) => {
          const cfg = getSeverityConfig(severity);
          return (
            <Card key={severity} style={{ borderLeft: `4px solid ${cfg.color}` }}>
              <CardContent className="pt-4 pb-4">
                <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">{cfg.label}</p>
                <p className="text-3xl font-bold mt-1" style={{ color: cfg.color }}>{count}</p>
              </CardContent>
            </Card>
          );
        })}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 items-start">
        {/* Grouped alarm list */}
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base">
              <Bell className="h-4 w-4 text-primary" />
              Alarmes por Tipo
              <Badge variant="secondary" className="ml-auto text-xs">{grouped.length} tipos</Badge>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {grouped.map(group => {
                const cfg = getSeverityConfig(group.severity);
                return (
                  <div
                    key={group.eventType}
                    className="flex items-center justify-between p-3 rounded-lg border hover:bg-muted/40 transition-colors"
                    style={{ borderLeftWidth: 3, borderLeftColor: cfg.color }}
                  >
                    <div className="flex items-center gap-3 min-w-0">
                      <TrendingUp className="h-4 w-4 shrink-0" style={{ color: cfg.color }} />
                      <div className="min-w-0">
                        <p className="font-medium text-sm truncate">{group.eventType}</p>
                        <p className="text-xs text-muted-foreground">
                          {group.locations} localização(ões) · último:{' '}
                          {formatDistanceToNow(new Date(group.lastOccurrence), {
                            addSuffix: true,
                            locale: ptBR,
                          })}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2 shrink-0 ml-3">
                      <Badge variant={cfg.variant} className="text-xs">{cfg.label}</Badge>
                      <Badge variant="outline" className="text-xs tabular-nums font-mono">
                        {group.totalCount}x
                      </Badge>
                    </div>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>

        {/* Geographic map */}
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base">
              <MapPin className="h-4 w-4 text-primary" />
              Distribuição Geográfica
            </CardTitle>
            <p className="text-xs text-muted-foreground">
              Clique em um marcador para ver os detalhes do alarme
            </p>
          </CardHeader>
          <CardContent className="p-0 overflow-hidden rounded-b-lg">
            <AlarmMap alarms={alarms} />
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
