import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { MeasurementDataDto } from '@/types/api';
import { format } from 'date-fns';
import { Database } from 'lucide-react';

interface MeasurementsTableProps {
  measurements: MeasurementDataDto[];
}

export function MeasurementsTable({ measurements }: MeasurementsTableProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Database className="h-5 w-5" />
          Histórico de Medições
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Dispositivo</TableHead>
                <TableHead>Timestamp</TableHead>
                <TableHead className="text-right">Tensão (V)</TableHead>
                <TableHead className="text-right">Corrente (A)</TableHead>
                <TableHead className="text-right">Frequência (Hz)</TableHead>
                <TableHead className="text-right">Fator de Potência</TableHead>
                <TableHead className="text-right">Sequência</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {measurements.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground">
                    Nenhuma medição disponível
                  </TableCell>
                </TableRow>
              ) : (
                measurements.slice(0, 10).map((measurement, idx) => (
                  <TableRow key={`${measurement.deviceId}-${idx}`}>
                    <TableCell className="font-medium">{measurement.deviceId}</TableCell>
                    <TableCell>{format(new Date(measurement.timestamp), 'dd/MM/yyyy HH:mm:ss')}</TableCell>
                    <TableCell className="text-right">{measurement.voltage.toFixed(2)}</TableCell>
                    <TableCell className="text-right">{measurement.current.toFixed(2)}</TableCell>
                    <TableCell className="text-right">{measurement.frequency.toFixed(2)}</TableCell>
                    <TableCell className="text-right">{measurement.powerFactor.toFixed(3)}</TableCell>
                    <TableCell className="text-right">{measurement.sequence}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      </CardContent>
    </Card>
  );
}
