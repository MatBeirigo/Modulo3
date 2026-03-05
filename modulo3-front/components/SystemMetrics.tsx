import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { MeasurementDataDto } from '@/types/api';
import { TrendingUp, TrendingDown, Minus, Zap, Activity, Radio } from 'lucide-react';

interface SystemMetricsProps {
  measurements: MeasurementDataDto[];
}

export function SystemMetrics({ measurements }: SystemMetricsProps) {
  if (measurements.length === 0) {
    return null;
  }

  const latestMeasurements = measurements.slice(0, Math.min(10, measurements.length));
  
  const avgVoltage = latestMeasurements.reduce((sum, m) => sum + m.voltage, 0) / latestMeasurements.length;
  const avgCurrent = latestMeasurements.reduce((sum, m) => sum + m.current, 0) / latestMeasurements.length;
  const avgFrequency = latestMeasurements.reduce((sum, m) => sum + m.frequency, 0) / latestMeasurements.length;
  const avgPowerFactor = latestMeasurements.reduce((sum, m) => sum + m.powerFactor, 0) / latestMeasurements.length;

  const prevVoltage = measurements.length > 10 ? measurements[10].voltage : avgVoltage;
  const prevCurrent = measurements.length > 10 ? measurements[10].current : avgCurrent;
  const prevFrequency = measurements.length > 10 ? measurements[10].frequency : avgFrequency;
  const prevPowerFactor = measurements.length > 10 ? measurements[10].powerFactor : avgPowerFactor;

  const getTrendIcon = (current: number, previous: number, threshold = 0.01) => {
    const diff = current - previous;
    if (Math.abs(diff) < threshold) return <Minus className="h-4 w-4 text-gray-500" />;
    return diff > 0 ? <TrendingUp className="h-4 w-4 text-green-500" /> : <TrendingDown className="h-4 w-4 text-red-500" />;
  };

  const getTrendPercentage = (current: number, previous: number) => {
    if (previous === 0) return '0';
    const diff = ((current - previous) / previous) * 100;
    return diff.toFixed(2);
  };

  return (
    <div className="grid gap-4 md:grid-cols-4">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Tensão Média</CardTitle>
          <Zap className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="flex items-baseline justify-between">
            <div>
              <div className="text-2xl font-bold">{avgVoltage.toFixed(2)}</div>
              <p className="text-xs text-muted-foreground">Volts (V)</p>
            </div>
            <div className="flex items-center gap-1">
              {getTrendIcon(avgVoltage, prevVoltage, 0.5)}
              <span className="text-xs text-muted-foreground">
                {getTrendPercentage(avgVoltage, prevVoltage)}%
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Corrente Média</CardTitle>
          <Activity className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="flex items-baseline justify-between">
            <div>
              <div className="text-2xl font-bold">{avgCurrent.toFixed(2)}</div>
              <p className="text-xs text-muted-foreground">Amperes (A)</p>
            </div>
            <div className="flex items-center gap-1">
              {getTrendIcon(avgCurrent, prevCurrent, 0.5)}
              <span className="text-xs text-muted-foreground">
                {getTrendPercentage(avgCurrent, prevCurrent)}%
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Frequência Média</CardTitle>
          <Radio className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="flex items-baseline justify-between">
            <div>
              <div className="text-2xl font-bold">{avgFrequency.toFixed(2)}</div>
              <p className="text-xs text-muted-foreground">Hertz (Hz)</p>
            </div>
            <div className="flex items-center gap-1">
              {getTrendIcon(avgFrequency, prevFrequency, 0.05)}
              <span className="text-xs text-muted-foreground">
                {getTrendPercentage(avgFrequency, prevFrequency)}%
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Fator de Potência</CardTitle>
          <Zap className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="flex items-baseline justify-between">
            <div>
              <div className="text-2xl font-bold">{avgPowerFactor.toFixed(3)}</div>
              <p className="text-xs text-muted-foreground">Adimensional</p>
            </div>
            <div className="flex items-center gap-1">
              {getTrendIcon(avgPowerFactor, prevPowerFactor, 0.01)}
              <span className="text-xs text-muted-foreground">
                {getTrendPercentage(avgPowerFactor, prevPowerFactor)}%
              </span>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
