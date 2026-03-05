import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { FilteredEventReport } from '@/types/api';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { BarChart3 } from 'lucide-react';

interface EventReportsChartsProps {
  reports: FilteredEventReport[];
}

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8', '#82ca9d'];

export function EventReportsCharts({ reports }: EventReportsChartsProps) {
  const pieData = reports.map(report => ({
    name: report.eventType,
    value: report.totalCount,
  }));

  const barData = reports.map(report => {
    const data: any = { eventType: report.eventType };
    if (report.countByDevice) {
      Object.entries(report.countByDevice).forEach(([device, count]) => {
        data[device] = count;
      });
    }
    return data;
  });

  const deviceKeys = reports.length > 0 && reports[0].countByDevice 
    ? Object.keys(reports[0].countByDevice) 
    : [];

  return (
    <div className="grid gap-4 md:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BarChart3 className="h-5 w-5" />
            Distribuição de Eventos
          </CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={pieData}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={(entry: any) => `${entry.name}: ${((entry.percent || 0) * 100).toFixed(0)}%`}
                outerRadius={80}
                fill="#8884d8"
                dataKey="value"
              >
                {pieData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BarChart3 className="h-5 w-5" />
            Eventos por Dispositivo
          </CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={barData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="eventType" fontSize={12} />
              <YAxis fontSize={12} />
              <Tooltip />
              <Legend />
              {deviceKeys.map((device, idx) => (
                <Bar 
                  key={device} 
                  dataKey={device} 
                  fill={COLORS[idx % COLORS.length]} 
                  name={device}
                />
              ))}
            </BarChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>
    </div>
  );
}
