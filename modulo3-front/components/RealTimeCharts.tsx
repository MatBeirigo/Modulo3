import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { MeasurementDataDto } from '@/types/api';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { Activity } from 'lucide-react';
import { format } from 'date-fns';

interface RealTimeChartsProps {
  measurements: MeasurementDataDto[];
}

export function RealTimeCharts({ measurements }: RealTimeChartsProps) {
  const chartData = measurements.slice(-20).map(m => ({
    time: format(new Date(m.timestamp), 'HH:mm:ss'),
    voltage: m.voltage,
    current: m.current,
    frequency: m.frequency,
    powerFactor: m.powerFactor,
    deviceId: m.deviceId,
  }));

  return (
    <div className="grid gap-4 md:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="h-5 w-5" />
            Tensão e Corrente
          </CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="time" fontSize={12} />
              <YAxis fontSize={12} />
              <Tooltip />
              <Legend />
              <Line 
                type="monotone" 
                dataKey="voltage" 
                stroke="#8884d8" 
                name="Tensão (V)"
                strokeWidth={2}
              />
              <Line 
                type="monotone" 
                dataKey="current" 
                stroke="#82ca9d" 
                name="Corrente (A)"
                strokeWidth={2}
              />
            </LineChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="h-5 w-5" />
            Frequência
          </CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="time" fontSize={12} />
              <YAxis fontSize={12} domain={['dataMin - 1', 'dataMax + 1']} />
              <Tooltip />
              <Legend />
              <Line 
                type="monotone" 
                dataKey="frequency" 
                stroke="#ffc658" 
                name="Frequência (Hz)"
                strokeWidth={2}
              />
            </LineChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="h-5 w-5" />
            Fator de Potência
          </CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="time" fontSize={12} />
              <YAxis fontSize={12} domain={[0, 1]} />
              <Tooltip />
              <Legend />
              <Line 
                type="monotone" 
                dataKey="powerFactor" 
                stroke="#ff7300" 
                name="Fator de Potência"
                strokeWidth={2}
              />
            </LineChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Medições Atuais</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {measurements.slice(0, 3).map((m, idx) => (
              <div key={`${m.deviceId}-${idx}`} className="border rounded-lg p-3">
                <p className="font-medium mb-2">{m.deviceId}</p>
                <div className="grid grid-cols-2 gap-2 text-sm">
                  <div>
                    <span className="text-muted-foreground">Tensão:</span>
                    <span className="ml-2 font-semibold">{m.voltage.toFixed(2)} V</span>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Corrente:</span>
                    <span className="ml-2 font-semibold">{m.current.toFixed(2)} A</span>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Frequência:</span>
                    <span className="ml-2 font-semibold">{m.frequency.toFixed(2)} Hz</span>
                  </div>
                  <div>
                    <span className="text-muted-foreground">FP:</span>
                    <span className="ml-2 font-semibold">{m.powerFactor.toFixed(3)}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
