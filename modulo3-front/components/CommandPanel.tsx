import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { CommandRequest } from '@/types/api';
import { apiService } from '@/services/api.service';
import { useState } from 'react';
import { Zap, Power, Loader2 } from 'lucide-react';

interface CommandPanelProps {
  deviceIds: string[];
}

export function CommandPanel({ deviceIds }: CommandPanelProps) {
  const [loading, setLoading] = useState<string | null>(null);
  const [lastResponse, setLastResponse] = useState<string | null>(null);

  const sendCommand = async (deviceId: string, commandType: string, targetState: string) => {
    setLoading(deviceId);
    setLastResponse(null);
    try {
      const command: CommandRequest = {
        deviceId,
        commandType,
        targetState,
      };
      const response = await apiService.sendCommand(command);
      setLastResponse(`${response.status}: ${response.message}`);
    } catch (error) {
      setLastResponse(`Erro: ${error instanceof Error ? error.message : 'Erro desconhecido'}`);
    } finally {
      setLoading(null);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Zap className="h-5 w-5" />
          Painel de Comandos
        </CardTitle>
      </CardHeader>
      <CardContent>
        {lastResponse && (
          <div className="mb-4 p-3 bg-muted rounded-md text-sm">
            {lastResponse}
          </div>
        )}
        <div className="space-y-3">
          {deviceIds.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              Nenhum dispositivo disponível
            </p>
          ) : (
            deviceIds.slice(0, 5).map((deviceId) => (
              <div
                key={deviceId}
                className="flex items-center justify-between border rounded-lg p-3"
              >
                <div>
                  <p className="font-medium">{deviceId}</p>
                  <p className="text-xs text-muted-foreground">Dispositivo de controle</p>
                </div>
                <div className="flex gap-2">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => sendCommand(deviceId, 'switch', 'open')}
                    disabled={loading === deviceId}
                  >
                    {loading === deviceId ? (
                      <Loader2 className="h-4 w-4 animate-spin" />
                    ) : (
                      <>
                        <Power className="h-4 w-4 mr-1" />
                        Abrir
                      </>
                    )}
                  </Button>
                  <Button
                    size="sm"
                    variant="default"
                    onClick={() => sendCommand(deviceId, 'switch', 'close')}
                    disabled={loading === deviceId}
                  >
                    {loading === deviceId ? (
                      <Loader2 className="h-4 w-4 animate-spin" />
                    ) : (
                      <>
                        <Power className="h-4 w-4 mr-1" />
                        Fechar
                      </>
                    )}
                  </Button>
                </div>
              </div>
            ))
          )}
        </div>
      </CardContent>
    </Card>
  );
}
