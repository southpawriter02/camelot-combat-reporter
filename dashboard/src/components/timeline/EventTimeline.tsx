import { useState, useMemo } from 'react';
import type { TimelineFilterConfig } from 'camelot-combat-reporter';
import { useSession } from '@/context/SessionContext';
import { WidgetContainer } from '../widgets/WidgetContainer';
import { TimelineEntryRow } from './TimelineEntryRow';
import { TimelineFilterBar } from './TimelineFilter';

const MAX_VISIBLE_ENTRIES = 100;

export function EventTimeline() {
  const { getTimeline } = useSession();
  const [filter, setFilter] = useState<Partial<TimelineFilterConfig>>({});
  const [showAll, setShowAll] = useState(false);

  const timeline = useMemo(() => getTimeline(filter), [getTimeline, filter]);

  if (!timeline) {
    return (
      <WidgetContainer title="Event Timeline" size="full">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          No session selected
        </p>
      </WidgetContainer>
    );
  }

  const entries = timeline.entries;
  const displayedEntries = showAll ? entries : entries.slice(0, MAX_VISIBLE_ENTRIES);
  const hasMore = entries.length > MAX_VISIBLE_ENTRIES && !showAll;

  return (
    <WidgetContainer title={`Event Timeline (${entries.length} events)`} size="full">
      <TimelineFilterBar filter={filter} onChange={setFilter} />

      {entries.length === 0 ? (
        <p className="text-sm text-slate-500 dark:text-slate-400">
          No events match the current filter
        </p>
      ) : (
        <>
          <div className="max-h-96 overflow-y-auto">
            {displayedEntries.map((entry) => (
              <TimelineEntryRow key={entry.id} entry={entry} />
            ))}
          </div>
          {hasMore && (
            <button
              onClick={() => setShowAll(true)}
              className="mt-2 w-full rounded-md bg-slate-100 py-2 text-xs text-slate-600 hover:bg-slate-200 dark:bg-slate-700 dark:text-slate-300 dark:hover:bg-slate-600"
            >
              Show all {entries.length} events
            </button>
          )}
        </>
      )}

      <div className="mt-3 flex flex-wrap gap-4 text-xs text-slate-500 dark:text-slate-400">
        <span>Total Damage: {timeline.stats.totalDamage.toLocaleString()}</span>
        <span>Total Healing: {timeline.stats.totalHealing.toLocaleString()}</span>
        <span>Deaths: {timeline.stats.deathCount}</span>
        <span>CC Events: {timeline.stats.ccCount}</span>
      </div>
    </WidgetContainer>
  );
}
