import { useMemo, useState } from 'react';
import {
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  Tooltip,
  Legend,
} from 'recharts';
import { useSession } from '@/context/SessionContext';
import { WidgetContainer } from '../widgets/WidgetContainer';
import { CHART_COLORS } from '@/lib/colorSchemes';

type BreakdownType = 'bySpell' | 'byTarget';

export function HealingBreakdownPie() {
  const { sessionSummary } = useSession();
  const [breakdownType, setBreakdownType] = useState<BreakdownType>('bySpell');

  const chartData = useMemo(() => {
    if (!sessionSummary) return [];

    const participantMetrics = sessionSummary.participantMetrics;
    if (participantMetrics.length === 0) return [];

    // Aggregate across all participants
    const aggregated: Record<string, { name: string; value: number; color: string }> = {};

    for (const participant of participantMetrics) {
      const healingMetrics = participant.healing;

      if (breakdownType === 'bySpell') {
        for (const breakdown of healingMetrics.bySpell) {
          const key = breakdown.spellName;
          if (!aggregated[key]) {
            const index = Object.keys(aggregated).length;
            aggregated[key] = {
              name: key,
              value: 0,
              color: CHART_COLORS[index % CHART_COLORS.length]!,
            };
          }
          aggregated[key]!.value += breakdown.totalHealing;
        }
      } else if (breakdownType === 'byTarget') {
        for (const breakdown of healingMetrics.byTarget) {
          const key = breakdown.target.name;
          if (!aggregated[key]) {
            const index = Object.keys(aggregated).length;
            aggregated[key] = {
              name: key,
              value: 0,
              color: CHART_COLORS[index % CHART_COLORS.length]!,
            };
          }
          aggregated[key]!.value += breakdown.totalHealing;
        }
      }
    }

    // Sort by value and take top 8, group rest as "Other"
    const sorted = Object.values(aggregated).sort((a, b) => b.value - a.value);
    if (sorted.length > 8) {
      const top = sorted.slice(0, 7);
      const otherValue = sorted.slice(7).reduce((sum, item) => sum + item.value, 0);
      top.push({ name: 'Other', value: otherValue, color: '#6b7280' });
      return top;
    }

    return sorted;
  }, [sessionSummary, breakdownType]);

  if (chartData.length === 0) {
    return (
      <WidgetContainer title="Healing Breakdown" size="medium">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          No healing data available
        </p>
      </WidgetContainer>
    );
  }

  return (
    <WidgetContainer title="Healing Breakdown" size="medium">
      <div className="mb-2 flex gap-1">
        {(['bySpell', 'byTarget'] as const).map((type) => (
          <button
            key={type}
            onClick={() => setBreakdownType(type)}
            className={`rounded px-2 py-1 text-xs transition-colors ${
              breakdownType === type
                ? 'bg-green-500 text-white'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-700 dark:text-slate-300 dark:hover:bg-slate-600'
            }`}
          >
            {type === 'bySpell' ? 'Spell' : 'Target'}
          </button>
        ))}
      </div>
      <div className="h-56">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={chartData}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              innerRadius={40}
              outerRadius={70}
              paddingAngle={2}
            >
              {chartData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.color} />
              ))}
            </Pie>
            <Tooltip
              formatter={(value: number) => value.toLocaleString()}
              contentStyle={{
                backgroundColor: 'rgba(255,255,255,0.95)',
                border: '1px solid #e2e8f0',
                borderRadius: '6px',
                fontSize: '12px',
              }}
            />
            <Legend
              wrapperStyle={{ fontSize: '11px' }}
              formatter={(value: string) =>
                value.length > 12 ? `${value.slice(0, 12)}...` : value
              }
            />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </WidgetContainer>
  );
}
