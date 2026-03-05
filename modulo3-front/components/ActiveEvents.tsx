import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ProtectionEventDto } from '@/types/api';
import { AlertTriangle, Shield, XCircle } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';
import { ptBR } from 'date-fns/locale';

interface ActiveEventsProps {
  events: ProtectionEventDto[];
}

export function ActiveEvents({ events }: ActiveEventsProps) {
  const getSeverityColor = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical':
      case 'critico':
        return 'bg-red-500';
      case 'high':
      case 'alto':
        return 'bg-orange-500';
      case 'medium':
      case 'medio':
        return 'bg-yellow-500';
      default:
        return 'bg-blue-500';
    }
  };

  const getSeverityIcon = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical':
      case 'critico':
        return <XCircle className="h-4 w-4" />;
      case 'high':
      case 'alto':
        return <AlertTriangle className="h-4 w-4" />;
      default:
        return <Shield className="h-4 w-4" />;
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <AlertTriangle className="h-5 w-5" />
          Eventos de Proteção Ativos
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          {events.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              Nenhum evento ativo no momento
            </p>
          ) : (
            events.map((event, idx) => (
              <div
                key={`${event.deviceId}-${idx}`}
                className="flex items-start justify-between border-b pb-3 last:border-0"
              >
                <div className="flex items-start gap-3">
                  <div className={`mt-1 h-2 w-2 rounded-full ${getSeverityColor(event.severity)}`} />
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="font-medium">{event.deviceId}</p>
                      {event.isActive && (
                        <Badge variant="destructive" className="text-xs">Em Andamento</Badge>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground">{event.eventType}</p>
                    <p className="text-xs text-muted-foreground mt-1">
                      Iniciado {formatDistanceToNow(new Date(event.startTime), { 
                        addSuffix: true,
                        locale: ptBR
                      })}
                    </p>
                  </div>
                </div>
                <Badge variant="outline" className="flex items-center gap-1">
                  {getSeverityIcon(event.severity)}
                  {event.severity}
                </Badge>
              </div>
            ))
          )}
        </div>
      </CardContent>
    </Card>
  );
}
