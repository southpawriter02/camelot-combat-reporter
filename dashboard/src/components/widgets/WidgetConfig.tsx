import { Eye, EyeOff } from 'lucide-react';
import { useConfig } from '@/context/ConfigContext';
import clsx from 'clsx';

const WIDGET_LABELS: Record<string, string> = {
  'damage-meter': 'Damage Meter',
  'healing-meter': 'Healing Meter',
  'dps-timeline': 'DPS Timeline',
  'damage-breakdown': 'Damage Breakdown',
  'healing-breakdown': 'Healing Breakdown',
  'event-timeline': 'Event Timeline',
  'deaths-cc': 'Deaths & CC',
  'player-stats': 'Player Stats',
};

export function WidgetConfig() {
  const { widgets, toggleWidget } = useConfig();

  return (
    <div className="card">
      <h2 className="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">
        Widgets
      </h2>
      <div className="space-y-1">
        {widgets.map((widget) => (
          <button
            key={widget.id}
            onClick={() => toggleWidget(widget.id)}
            className={clsx(
              'flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left text-sm transition-colors',
              widget.visible
                ? 'text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-700'
                : 'text-slate-400 hover:bg-slate-100 dark:text-slate-500 dark:hover:bg-slate-700'
            )}
          >
            <span>{WIDGET_LABELS[widget.id] ?? widget.id}</span>
            {widget.visible ? (
              <Eye className="h-4 w-4 text-blue-500" />
            ) : (
              <EyeOff className="h-4 w-4" />
            )}
          </button>
        ))}
      </div>
    </div>
  );
}
