import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { FilteredEventReport } from '@/types/api';
import { FileText } from 'lucide-react';
import { format } from 'date-fns';

interface EventReportsTableProps {
  reports: FilteredEventReport[];
}

export function EventReportsTable({ reports }: EventReportsTableProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <FileText className="h-5 w-5" />
          Relatórios de Eventos
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tipo de Evento</TableHead>
                <TableHead className="text-right">Total</TableHead>
                <TableHead>Dispositivos Afetados</TableHead>
                <TableHead>Timestamp</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {reports.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="text-center text-muted-foreground">
                    Nenhum relatório disponível
                  </TableCell>
                </TableRow>
              ) : (
                reports.map((report, idx) => (
                  <TableRow key={`${report.eventType}-${idx}`}>
                    <TableCell className="font-medium">{report.eventType}</TableCell>
                    <TableCell className="text-right">
                      <Badge>{report.totalCount}</Badge>
                    </TableCell>
                    <TableCell>
                      {report.countByDevice && (
                        <div className="flex flex-wrap gap-1">
                          {Object.entries(report.countByDevice).map(([device, count]) => (
                            <Badge key={device} variant="outline" className="text-xs">
                              {device}: {count}
                            </Badge>
                          ))}
                        </div>
                      )}
                    </TableCell>
                    <TableCell>{format(new Date(report.reportTimestamp), 'dd/MM/yyyy HH:mm:ss')}</TableCell>
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
