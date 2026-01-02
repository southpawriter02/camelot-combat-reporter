import { useLog } from '@/context/LogContext';
import { useSession } from '@/context/SessionContext';
import { useConfig } from '@/context/ConfigContext';
import { SessionSummaryCard } from '../session/SessionSummaryCard';
import { DamageMeter } from '../meters/DamageMeter';
import { HealingMeter } from '../meters/HealingMeter';
import { DPSTimelineChart } from '../charts/DPSTimelineChart';
import { HPSTimelineChart } from '../charts/HPSTimelineChart';
import { DamageBreakdownPie } from '../charts/DamageBreakdownPie';
import { HealingBreakdownPie } from '../charts/HealingBreakdownPie';
import { EventTimeline } from '../timeline/EventTimeline';
import { PlayerStatsCard } from '../player-stats/PlayerStatsCard';
import { Upload } from 'lucide-react';

export function Dashboard() {
  const { parsedLog } = useLog();
  const { selectedSession } = useSession();
  const { widgets } = useConfig();

  const isWidgetVisible = (id: string) => widgets.find((w) => w.id === id)?.visible ?? false;

  if (!parsedLog) {
    return (
      <div className="flex h-full flex-col items-center justify-center text-center">
        <Upload className="mb-4 h-16 w-16 text-slate-300 dark:text-slate-600" />
        <h2 className="mb-2 text-xl font-semibold text-slate-700 dark:text-slate-200">
          No Log File Loaded
        </h2>
        <p className="max-w-md text-sm text-slate-500 dark:text-slate-400">
          Drag and drop a DAoC combat log file onto the sidebar, or click the upload area to select a file.
        </p>
      </div>
    );
  }

  if (!selectedSession) {
    return (
      <div className="flex h-full flex-col items-center justify-center text-center">
        <h2 className="mb-2 text-xl font-semibold text-slate-700 dark:text-slate-200">
          No Combat Sessions Found
        </h2>
        <p className="max-w-md text-sm text-slate-500 dark:text-slate-400">
          The log file was parsed but no combat sessions were detected. Make sure the file contains combat events.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-4">
      {/* Session Summary - Always visible */}
      <div className="lg:col-span-4">
        <SessionSummaryCard />
      </div>

      {/* Meters */}
      {isWidgetVisible('damage-meter') && (
        <div className="lg:col-span-2">
          <DamageMeter />
        </div>
      )}
      {isWidgetVisible('healing-meter') && (
        <div className="lg:col-span-2">
          <HealingMeter />
        </div>
      )}

      {/* Timeline Charts */}
      {isWidgetVisible('dps-timeline') && (
        <div className="lg:col-span-4">
          <DPSTimelineChart />
        </div>
      )}

      {/* Breakdown Charts */}
      {isWidgetVisible('damage-breakdown') && (
        <div className="lg:col-span-2">
          <DamageBreakdownPie />
        </div>
      )}
      {isWidgetVisible('healing-breakdown') && (
        <div className="lg:col-span-2">
          <HealingBreakdownPie />
        </div>
      )}

      {/* Event Timeline */}
      {isWidgetVisible('event-timeline') && (
        <div className="lg:col-span-4">
          <EventTimeline />
        </div>
      )}

      {/* Player Stats */}
      {isWidgetVisible('player-stats') && (
        <div className="lg:col-span-4">
          <PlayerStatsCard />
        </div>
      )}
    </div>
  );
}
