import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { RefreshCw, Trash2, Pause, Play } from 'lucide-react';

interface DataControlsProps {
  onReload: () => void;
  onClear: () => void;
  isPaused: boolean;
  onTogglePause: () => void;
}

export function DataControls({ onReload, onClear, isPaused, onTogglePause }: DataControlsProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">Controles de Dados</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="grid grid-cols-1 gap-3">
          <Button 
            onClick={onReload} 
            variant="default" 
            className="w-full"
            disabled={isPaused}
          >
            <RefreshCw className="mr-2 h-4 w-4" />
            Recarregar Todos os Dados
          </Button>
          
          <Button 
            onClick={onClear} 
            variant="destructive" 
            className="w-full"
          >
            <Trash2 className="mr-2 h-4 w-4" />
            Limpar Interface
          </Button>
          
          <Button 
            onClick={onTogglePause} 
            variant={isPaused ? "default" : "secondary"}
            className="w-full"
          >
            {isPaused ? (
              <>
                <Play className="mr-2 h-4 w-4" />
                Retomar Atualizações
              </>
            ) : (
              <>
                <Pause className="mr-2 h-4 w-4" />
                Pausar Atualizações
              </>
            )}
          </Button>
        </div>
        
        <div className="text-xs text-muted-foreground pt-2 border-t">
          <p><strong>Recarregar:</strong> Busca novos dados do backend</p>
          <p><strong>Limpar:</strong> Remove todos os dados da interface</p>
          <p><strong>Pausar:</strong> Interrompe as atualizações automáticas</p>
        </div>
      </CardContent>
    </Card>
  );
}
