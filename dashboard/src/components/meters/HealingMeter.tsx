import { useMemo } from 'react';
import type { HealingMeterEntry } from 'camelot-combat-reporter';
import { useSession } from '@/context/SessionContext';
import { WidgetContainer } from '../widgets/WidgetContainer';
import { MeterBar } from './MeterBar';
import { getEntityColor } from '@/lib/colorSchemes';

export function HealingMeter() {
  const { sessionSummary } = useSession();

  const entries = useMemo(() => {
    if (!sessionSummary) return [];
    return sessionSummary.healingMeter.slice(0, 10); // Top 10
  }, [sessionSummary]);

  if (entries.length === 0) {
    return (
      <WidgetContainer title="Healing Meter" size="medium">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          No healing data available
        </p>
      </WidgetContainer>
    );
  }

  const maxHealing = entries[0]?.totalHealing ?? 1;

  return (
    <WidgetContainer title="Healing Meter" size="medium">
      <div className="space-y-2">
        {entries.map((entry, index) => (
          <HealingMeterRow
            key={entry.entity.name}
            entry={entry}
            maxHealing={maxHealing}
            color={getEntityColor(index)}
          />
        ))}
      </div>
    </WidgetContainer>
  );
}

interface HealingMeterRowProps {
  entry: HealingMeterEntry;
  maxHealing: number;
  color: string;
}

function HealingMeterRow({ entry, maxHealing, color }: HealingMeterRowProps) {
  const barPercentage = (entry.totalHealing / maxHealing) * 100;

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
          <span>{entry.totalHealing.toLocaleString()}</span>
          <span className="w-14 text-right">{entry.hps.toFixed(1)} HPS</span>
          <span
            className="w-12 text-right"
            title={`${(entry.overhealRate * 100).toFixed(1)}% overheal`}
          >
            {(entry.overhealRate * 100).toFixed(0)}% OH
          </span>
        </div>
      </div>
      <MeterBar percentage={barPercentage} color={color} />
    </div>
  );
}
