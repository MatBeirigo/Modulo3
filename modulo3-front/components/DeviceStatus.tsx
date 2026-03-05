import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { DeviceStatusDto } from '@/types/api';
import { Activity, AlertCircle, CheckCircle, Clock } from 'lucide-react';

interface DevicesOverviewProps {
  devices: DeviceStatusDto[];
}

export function DevicesOverview({ devices }: DevicesOverviewProps) {
  const activeDevices = devices.filter(d => d.isActive).length;
  const staleDevices = devices.filter(d => d.isStale).length;
  const totalDevices = devices.length;

  return (
    <div className="grid gap-4 md:grid-cols-3">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Total de Dispositivos</CardTitle>
          <Activity className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">{totalDevices}</div>
          <p className="text-xs text-muted-foreground">Dispositivos conectados</p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Dispositivos Ativos</CardTitle>
          <CheckCircle className="h-4 w-4 text-green-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">{activeDevices}</div>
          <p className="text-xs text-muted-foreground">
            {totalDevices > 0 ? Math.round((activeDevices / totalDevices) * 100) : 0}% do total
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Dispositivos Inativos</CardTitle>
          <AlertCircle className="h-4 w-4 text-red-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">{staleDevices}</div>
          <p className="text-xs text-muted-foreground">
            {totalDevices > 0 ? Math.round((staleDevices / totalDevices) * 100) : 0}% do total
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

interface DevicesTableProps {
  devices: DeviceStatusDto[];
}

export function DevicesTable({ devices }: DevicesTableProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Status dos Dispositivos</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          {devices.map((device) => (
            <div
              key={device.deviceId}
              className="flex items-center justify-between border-b pb-3 last:border-0"
            >
              <div className="flex items-center gap-3">
                <div
                  className={`h-2 w-2 rounded-full ${
                    device.isActive ? 'bg-green-500' : 'bg-red-500'
                  }`}
                />
                <div>
                  <p className="font-medium">{device.deviceId}</p>
                  <p className="text-xs text-muted-foreground flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    {device.secondsSinceLastUpdate}s atrás
                  </p>
                </div>
              </div>
              <div className="flex gap-2">
                <Badge variant={device.isActive ? 'default' : 'destructive'}>
                  {device.isActive ? 'Ativo' : 'Inativo'}
                </Badge>
                {device.isStale && (
                  <Badge variant="secondary">Obsoleto</Badge>
                )}
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
