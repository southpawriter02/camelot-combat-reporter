import type { TimelineEntry } from 'camelot-combat-reporter';
import { MARKER_CATEGORY_COLORS } from '@/lib/colorSchemes';
import clsx from 'clsx';

interface TimelineEntryRowProps {
  entry: TimelineEntry;
}

export function TimelineEntryRow({ entry }: TimelineEntryRowProps) {
  const markerColor = MARKER_CATEGORY_COLORS[entry.markerCategory];

  return (
    <div className="flex items-start gap-2 border-b border-slate-100 py-2 last:border-b-0 dark:border-slate-700">
      <div
        className="mt-1 h-2 w-2 flex-shrink-0 rounded-full"
        style={{ backgroundColor: markerColor }}
        title={entry.markerCategory}
      />
      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between gap-2">
          <span className="text-xs font-mono text-slate-500 dark:text-slate-400">
            {entry.formattedRelativeTime}
          </span>
          {entry.primaryValue !== undefined && (
            <span
              className={clsx(
                'text-xs font-medium',
                entry.markerCategory.includes('DAMAGE')
                  ? 'text-red-600 dark:text-red-400'
                  : entry.markerCategory.includes('HEAL')
                    ? 'text-green-600 dark:text-green-400'
                    : 'text-slate-600 dark:text-slate-300'
              )}
            >
              {entry.primaryValue.toLocaleString()} {entry.primaryValueUnit}
            </span>
          )}
        </div>
        <p className="text-sm text-slate-700 dark:text-slate-200 truncate">
          {entry.description}
        </p>
      </div>
    </div>
  );
}
