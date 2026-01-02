import { useMemo } from 'react';
import type { DamageMeterEntry } from 'camelot-combat-reporter';
import { useSession } from '@/context/SessionContext';
import { WidgetContainer } from '../widgets/WidgetContainer';
import { MeterBar } from './MeterBar';
import { getEntityColor } from '@/lib/colorSchemes';

export function DamageMeter() {
  const { sessionSummary } = useSession();

  const entries = useMemo(() => {
    if (!sessionSummary) return [];
    return sessionSummary.damageMeter.slice(0, 10); // Top 10
  }, [sessionSummary]);

  if (entries.length === 0) {
    return (
      <WidgetContainer title="Damage Meter" size="medium">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          No damage data available
        </p>
      </WidgetContainer>
    );
  }

  const maxDamage = entries[0]?.totalDamage ?? 1;

  return (
    <WidgetContainer title="Damage Meter" size="medium">
      <div className="space-y-2">
        {entries.map((entry, index) => (
          <DamageMeterRow
            key={entry.entity.name}
            entry={entry}
            maxDamage={maxDamage}
            color={getEntityColor(index)}
          />
        ))}
      </div>
    </WidgetContainer>
  );
}

interface DamageMeterRowProps {
  entry: DamageMeterEntry;
  maxDamage: number;
  color: string;
}

function DamageMeterRow({ entry, maxDamage, color }: DamageMeterRowProps) {
  const barPercentage = (entry.totalDamage / maxDamage) * 100;

  return (
    <div className="group">
      <div className="mb-1 flex items-center justify-between text-xs">
        <div className="flex items-center gap-2">
          <span
            className="flex h-5 w-5 items-center justify-center rounded text-[10px] font-bold text-white"
            style={{ backgroundColor: color }}
          >
            {entry.rank}
          </span>
          <span className="font-medium text-slate-700 dark:text-slate-200">
            {entry.entity.name}
          </span>
        </div>
        <div className="flex items-center gap-3 text-slate-500 dark:text-slate-400">
          <span>{entry.totalDamage.toLocaleString()}</span>
          <span className="w-14 text-right">{entry.dps.toFixed(1)} DPS</span>
          <span className="w-10 text-right">{(entry.percentage * 100).toFixed(1)}%</span>
        </div>
      </div>
      <MeterBar percentage={barPercentage} color={color} />
    </div>
  );
}
