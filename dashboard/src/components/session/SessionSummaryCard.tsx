import { Swords, Heart, Skull, Zap } from 'lucide-react';
import { useSession } from '@/context/SessionContext';
import { formatDuration } from 'camelot-combat-reporter';

export function SessionSummaryCard() {
  const { selectedSession, sessionSummary } = useSession();

  if (!selectedSession || !sessionSummary) {
    return null;
  }

  const { summary } = selectedSession;

  return (
    <div className="card">
      <div className="mb-3 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-200">
          Session Summary
        </h3>
        <span className="text-xs text-slate-500 dark:text-slate-400">
          {formatDuration(selectedSession.durationMs)}
        </span>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="rounded-md bg-red-50 p-2 dark:bg-red-900/20">
          <div className="mb-1 flex items-center gap-1 text-red-600 dark:text-red-400">
            <Swords className="h-4 w-4" />
            <span className="text-xs font-medium">Damage</span>
          </div>
          <p className="text-lg font-bold text-slate-700 dark:text-slate-200">
            {summary.totalDamageDealt.toLocaleString()}
          </p>
        </div>

        <div className="rounded-md bg-green-50 p-2 dark:bg-green-900/20">
          <div className="mb-1 flex items-center gap-1 text-green-600 dark:text-green-400">
            <Heart className="h-4 w-4" />
            <span className="text-xs font-medium">Healing</span>
          </div>
          <p className="text-lg font-bold text-slate-700 dark:text-slate-200">
            {summary.totalHealingDone.toLocaleString()}
          </p>
        </div>

        <div className="rounded-md bg-slate-100 p-2 dark:bg-slate-700/50">
          <div className="mb-1 flex items-center gap-1 text-slate-600 dark:text-slate-400">
            <Skull className="h-4 w-4" />
            <span className="text-xs font-medium">Deaths</span>
          </div>
          <p className="text-lg font-bold text-slate-700 dark:text-slate-200">
            {summary.deathCount}
          </p>
        </div>

        <div className="rounded-md bg-purple-50 p-2 dark:bg-purple-900/20">
          <div className="mb-1 flex items-center gap-1 text-purple-600 dark:text-purple-400">
            <Zap className="h-4 w-4" />
            <span className="text-xs font-medium">CC Events</span>
          </div>
          <p className="text-lg font-bold text-slate-700 dark:text-slate-200">
            {summary.ccEventCount}
          </p>
        </div>
      </div>
    </div>
  );
}
