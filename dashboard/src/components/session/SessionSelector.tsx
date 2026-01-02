import { Calendar, Clock, Users } from 'lucide-react';
import { useSession } from '@/context/SessionContext';
import { formatDuration } from 'camelot-combat-reporter';
import clsx from 'clsx';

export function SessionSelector() {
  const { sessions, selectedSession, selectSession } = useSession();

  if (sessions.length === 0) {
    return null;
  }

  return (
    <div className="card">
      <h2 className="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">
        Combat Sessions ({sessions.length})
      </h2>
      <div className="space-y-2">
        {sessions.map((session, index) => {
          const isSelected = selectedSession?.id === session.id;
          return (
            <button
              key={session.id}
              onClick={() => selectSession(session.id)}
              className={clsx(
                'w-full rounded-md border p-2 text-left transition-colors',
                isSelected
                  ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/30'
                  : 'border-slate-200 hover:border-slate-300 hover:bg-slate-50 dark:border-slate-600 dark:hover:border-slate-500 dark:hover:bg-slate-700/50'
              )}
            >
              <div className="mb-1 flex items-center justify-between">
                <span className="text-xs font-medium text-slate-700 dark:text-slate-200">
                  Session {index + 1}
                </span>
                {isSelected && (
                  <span className="rounded bg-blue-500 px-1.5 py-0.5 text-[10px] font-medium text-white">
                    Active
                  </span>
                )}
              </div>
              <div className="flex flex-wrap gap-x-3 gap-y-1 text-[11px] text-slate-500 dark:text-slate-400">
                <span className="flex items-center gap-1">
                  <Calendar className="h-3 w-3" />
                  {session.startTime.toLocaleTimeString()}
                </span>
                <span className="flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  {formatDuration(session.durationMs)}
                </span>
                <span className="flex items-center gap-1">
                  <Users className="h-3 w-3" />
                  {session.participants.length}
                </span>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
}
