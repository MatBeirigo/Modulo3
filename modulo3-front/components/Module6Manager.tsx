'use client';

import { useState } from 'react';
import { useModule6 } from '@/hooks/useModule6';
import { RelayState } from '@/types/api';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { 
  AlertCircle, 
  CheckCircle, 
  Power, 
  PowerOff, 
  Search, 
  Settings, 
  Loader2,
  Radio,
  ToggleLeft,
  ToggleRight,
  HelpCircle
} from 'lucide-react';

interface Module6ManagerProps {
  isPaused?: boolean;
}

export function Module6Manager({ isPaused = false }: Module6ManagerProps) {
  const {
    unconfiguredModules,
    moduleStates,
    loading,
    error,
    configureModule,
    closeRelay,
    openRelay,
    checkRelayState,
    refetch,
  } = useModule6(3000, isPaused);

  const [configuring, setConfiguring] = useState<string | null>(null);
  const [newId, setNewId] = useState<number>(1);
  const [controllingRelay, setControllingRelay] = useState<number | null>(null);
  const [feedbackMessage, setFeedbackMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const getRelayStateDisplay = (state: RelayState) => {
    switch (state) {
      case 'CLOSED':
        return {
          icon: <ToggleRight className="h-4 w-4" />,
          label: 'Fechado',
          variant: 'default' as const,
          color: 'text-green-600'
        };
      case 'OPEN':
        return {
          icon: <ToggleLeft className="h-4 w-4" />,
          label: 'Aberto',
          variant: 'destructive' as const,
          color: 'text-red-600'
        };
      default:
        return {
          icon: <HelpCircle className="h-4 w-4" />,
          label: 'Desconhecido',
          variant: 'secondary' as const,
          color: 'text-gray-600'
        };
    }
  };

  const handleConfigure = async (uniqueId: string) => {
    if (newId < 1 || newId > 99) {
      setFeedbackMessage({ type: 'error', text: 'ID deve estar entre 1 e 99' });
      return;
    }

    setConfiguring(uniqueId);
    setFeedbackMessage(null);

    const result = await configureModule(newId, uniqueId);
    
    setFeedbackMessage({
      type: result.success ? 'success' : 'error',
      text: result.message,
    });

    setConfiguring(null);
  };

  const handleRelayControl = async (moduleId: number, action: 'close' | 'open' | 'check') => {
    setControllingRelay(moduleId);
    setFeedbackMessage(null);

    let result;
    if (action === 'close') {
      result = await closeRelay(moduleId);
    } else if (action === 'open') {
      result = await openRelay(moduleId);
    } else {
      result = await checkRelayState(moduleId);
    }

    setFeedbackMessage({
      type: result.success ? 'success' : 'error',
      text: result.message,
    });

    setControllingRelay(null);
  };

  return (
    <div className="space-y-6">
      {feedbackMessage && (
        <Alert variant={feedbackMessage.type === 'success' ? 'default' : 'destructive'}>
          {feedbackMessage.type === 'success' ? (
            <CheckCircle className="h-4 w-4" />
          ) : (
            <AlertCircle className="h-4 w-4" />
          )}
          <AlertDescription>{feedbackMessage.text}</AlertDescription>
        </Alert>
      )}

      {/* Unconfigured Modules Card */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Radio className="h-5 w-5" />
                Módulos Não Configurados
              </CardTitle>
              <CardDescription>
                Módulos detectados via broadcast UDP aguardando configuração de ID
              </CardDescription>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={refetch}
              disabled={loading}
            >
              {loading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Search className="h-4 w-4" />
              )}
              <span className="ml-2">Atualizar</span>
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {error && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {!error && unconfiguredModules.length === 0 && (
            <div className="text-center py-8 text-muted-foreground">
              <Radio className="h-12 w-12 mx-auto mb-3 opacity-50" />
              <p>Nenhum módulo não configurado detectado</p>
              <p className="text-sm mt-1">
                Módulos que enviarem broadcast UDP aparecerão aqui
              </p>
            </div>
          )}

          {!error && unconfiguredModules.length > 0 && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Unique ID</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Novo ID</TableHead>
                  <TableHead>Ação</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {unconfiguredModules.map((uniqueId) => (
                  <TableRow key={uniqueId}>
                    <TableCell className="font-mono">{uniqueId}</TableCell>
                    <TableCell>
                      <Badge variant="secondary">Não Configurado</Badge>
                    </TableCell>
                    <TableCell>
                      <input
                        type="number"
                        min="1"
                        max="99"
                        value={newId}
                        onChange={(e) => setNewId(parseInt(e.target.value) || 1)}
                        className="w-20 px-2 py-1 border rounded text-sm"
                        disabled={configuring === uniqueId}
                      />
                    </TableCell>
                    <TableCell>
                      <Button
                        size="sm"
                        onClick={() => handleConfigure(uniqueId)}
                        disabled={configuring === uniqueId}
                      >
                        {configuring === uniqueId ? (
                          <>
                            <Loader2 className="h-4 w-4 animate-spin mr-2" />
                            Configurando...
                          </>
                        ) : (
                          <>
                            <Settings className="h-4 w-4 mr-2" />
                            Configurar
                          </>
                        )}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Configured Modules Status Card */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Power className="h-5 w-5" />
                Módulos Configurados - Estados em Tempo Real
              </CardTitle>
              <CardDescription>
                Monitoramento contínuo via broadcasts UDP
              </CardDescription>
            </div>
            <div className="flex items-center gap-2">
              {!isPaused && (
                <div className="flex items-center gap-2">
                  <div className="h-2 w-2 rounded-full bg-green-500 animate-pulse" />
                  <span className="text-xs text-muted-foreground">Atualizando</span>
                </div>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {moduleStates.length === 0 && (
            <div className="text-center py-8 text-muted-foreground">
              <Power className="h-12 w-12 mx-auto mb-3 opacity-50" />
              <p>Nenhum módulo configurado detectado</p>
              <p className="text-sm mt-1">
                Configure módulos para vê-los aqui
              </p>
            </div>
          )}

          {moduleStates.length > 0 && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>ID</TableHead>
                  <TableHead>Unique ID</TableHead>
                  <TableHead>Estado do Relé</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Última Atualização</TableHead>
                  <TableHead>Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {moduleStates.map((module) => {
                  const stateDisplay = getRelayStateDisplay(module.relayState);
                  return (
                    <TableRow key={module.moduleId}>
                      <TableCell className="font-mono font-bold">
                        #{module.moduleId.toString().padStart(2, '0')}
                      </TableCell>
                      <TableCell className="font-mono text-sm">
                        {module.uniqueId}
                      </TableCell>
                      <TableCell>
                        <Badge variant={stateDisplay.variant} className="flex items-center gap-1 w-fit">
                          {stateDisplay.icon}
                          {stateDisplay.label}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant={module.isOnline ? 'default' : 'secondary'}>
                          {module.isOnline ? 'Online' : 'Offline'}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {new Date(module.lastUpdate).toLocaleString('pt-BR')}
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleRelayControl(module.moduleId, 'close')}
                            disabled={controllingRelay === module.moduleId}
                            title="Fechar relé"
                          >
                            <Power className="h-3 w-3" />
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleRelayControl(module.moduleId, 'open')}
                            disabled={controllingRelay === module.moduleId}
                            title="Abrir relé"
                          >
                            <PowerOff className="h-3 w-3" />
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleRelayControl(module.moduleId, 'check')}
                            disabled={controllingRelay === module.moduleId}
                            title="Consultar estado"
                          >
                            <Search className="h-3 w-3" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

    </div>
  );
}
