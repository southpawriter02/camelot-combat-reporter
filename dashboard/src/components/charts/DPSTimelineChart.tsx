import { useMemo } from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts';
import {
  DPSCalculator,
  EventType,
  type DamageEvent,
} from 'camelot-combat-reporter';
import { useSession } from '@/context/SessionContext';
import { WidgetContainer } from '../widgets/WidgetContainer';
import { getEntityColor } from '@/lib/colorSchemes';

export function DPSTimelineChart() {
  const { selectedSession, sessionSummary } = useSession();

  const chartData = useMemo(() => {
    if (!selectedSession || !sessionSummary) return [];

    const dpsCalculator = new DPSCalculator(5000); // 5s window
    const { startTime, events } = selectedSession;

    // Get top 5 damage dealers
    const topDealers = sessionSummary.damageMeter
      .slice(0, 5)
      .map((e) => e.entity.name);

    // Calculate DPS timeline for each entity
    const entityTimelines: Record<string, Map<number, number>> = {};

    for (const entityName of topDealers) {
      const entityEvents = events.filter(
        (e): e is DamageEvent =>
          e.eventType === EventType.DAMAGE_DEALT && e.source.name === entityName
      );

      if (entityEvents.length > 0) {
        const timeline = dpsCalculator.calculateDPSTimeline(entityEvents, 1000);
        entityTimelines[entityName] = new Map(
          timeline.map((p) => [
            Math.floor((p.timestamp.getTime() - startTime.getTime()) / 1000),
            p.value,
          ])
        );
      }
    }

    // Build combined data points
    const durationSecs = Math.ceil(selectedSession.durationMs / 1000);
    const data: Array<{ time: number; [key: string]: number }> = [];

    for (let sec = 0; sec <= durationSecs; sec++) {
      const point: { time: number; [key: string]: number } = { time: sec };
      for (const entityName of topDealers) {
        point[entityName] = entityTimelines[entityName]?.get(sec) ?? 0;
      }
      data.push(point);
    }

    return data;
  }, [selectedSession, sessionSummary]);

  const entityNames = useMemo(() => {
    if (!sessionSummary) return [];
    return sessionSummary.damageMeter.slice(0, 5).map((e) => e.entity.name);
  }, [sessionSummary]);

  if (chartData.length === 0) {
    return (
      <WidgetContainer title="DPS Over Time" size="large">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          No damage data available
        </p>
      </WidgetContainer>
    );
  }

  return (
    <WidgetContainer title="DPS Over Time" size="large">
      <div className="h-64">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
            <XAxis
              dataKey="time"
              tickFormatter={(v) => formatTime(v)}
              stroke="#94a3b8"
              fontSize={11}
            />
            <YAxis stroke="#94a3b8" fontSize={11} />
            <Tooltip
              labelFormatter={(v) => `Time: ${formatTime(v as number)}`}
              contentStyle={{
                backgroundColor: 'rgba(255,255,255,0.95)',
                border: '1px solid #e2e8f0',
                borderRadius: '6px',
                fontSize: '12px',
              }}
            />
            <Legend />
            {entityNames.map((name, index) => (
              <Line
                key={name}
                type="monotone"
                dataKey={name}
                stroke={getEntityColor(index)}
                strokeWidth={2}
                dot={false}
                activeDot={{ r: 4 }}
              />
            ))}
          </LineChart>
        </ResponsiveContainer>
      </div>
    </WidgetContainer>
  );
}

function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}
