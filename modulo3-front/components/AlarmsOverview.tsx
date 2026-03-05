import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { AggregatedAlarm } from '@/types/api';
import { Bell, TrendingUp } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';
import { ptBR } from 'date-fns/locale';

interface AlarmsOverviewProps {
  alarms: AggregatedAlarm[];
}

export function AlarmsOverview({ alarms }: AlarmsOverviewProps) {
  const totalOccurrences = alarms.reduce((sum, alarm) => sum + alarm.eventCount, 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Bell className="h-5 w-5" />
          Alarmes Agregados
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="mb-4">
          <div className="text-3xl font-bold">{totalOccurrences}</div>
          <p className="text-sm text-muted-foreground">Ocorrências totais</p>
        </div>
        <div className="space-y-3">
          {alarms.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              Nenhum alarme registrado
            </p>
          ) : (
            alarms.map((alarm, idx) => (
              <div
                key={`${alarm.eventType}-${idx}`}
                className="border rounded-lg p-3 space-y-2"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <TrendingUp className="h-4 w-4 text-muted-foreground" />
                    <span className="font-medium">{alarm.eventType}</span>
                  </div>
                  <Badge>{alarm.eventCount} ocorrências</Badge>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <Badge variant="outline">{alarm.severity}</Badge>
                  <span className="text-muted-foreground">
                    Último: {formatDistanceToNow(new Date(alarm.lastOccurrence), {
                      addSuffix: true,
                      locale: ptBR
                    })}
                  </span>
                </div>
                {alarm.affectedDevices && alarm.affectedDevices.length > 0 && (
                  <div className="text-xs text-muted-foreground">
                    Dispositivos: {alarm.affectedDevices.join(', ')}
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </CardContent>
    </Card>
  );
}
