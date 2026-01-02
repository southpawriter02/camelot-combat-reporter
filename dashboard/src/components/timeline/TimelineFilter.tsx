import { EventType, type TimelineFilterConfig } from 'camelot-combat-reporter';

interface TimelineFilterProps {
  filter: Partial<TimelineFilterConfig>;
  onChange: (filter: Partial<TimelineFilterConfig>) => void;
}

const EVENT_TYPE_OPTIONS = [
  { value: '', label: 'All Events' },
  { value: EventType.DAMAGE_DEALT, label: 'Damage Dealt' },
  { value: EventType.DAMAGE_RECEIVED, label: 'Damage Received' },
  { value: EventType.HEALING_DONE, label: 'Healing Done' },
  { value: EventType.HEALING_RECEIVED, label: 'Healing Received' },
  { value: EventType.DEATH, label: 'Deaths' },
  { value: EventType.CROWD_CONTROL, label: 'Crowd Control' },
];

export function TimelineFilterBar({ filter, onChange }: TimelineFilterProps) {
  return (
    <div className="mb-3 flex flex-wrap gap-2">
      <select
        value={filter.eventTypes?.[0] ?? ''}
        onChange={(e) =>
          onChange({
            ...filter,
            eventTypes: e.target.value ? [e.target.value as EventType] : [],
          })
        }
        className="input w-auto text-xs"
      >
        {EVENT_TYPE_OPTIONS.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>

      <input
        type="text"
        placeholder="Filter by entity..."
        value={filter.entityName ?? ''}
        onChange={(e) =>
          onChange({
            ...filter,
            entityName: e.target.value || undefined,
          })
        }
        className="input w-32 text-xs"
      />

      <input
        type="number"
        placeholder="Min value"
        value={filter.minValue ?? ''}
        onChange={(e) =>
          onChange({
            ...filter,
            minValue: e.target.value ? parseInt(e.target.value, 10) : undefined,
          })
        }
        className="input w-24 text-xs"
      />

      <label className="flex items-center gap-1 text-xs text-slate-600 dark:text-slate-300">
        <input
          type="checkbox"
          checked={filter.criticalOnly ?? false}
          onChange={(e) =>
            onChange({
              ...filter,
              criticalOnly: e.target.checked,
            })
          }
          className="rounded border-slate-300"
        />
        Crits only
      </label>
    </div>
  );
}
