import { Menu, Moon, Sun, X } from 'lucide-react';
import { useConfig } from '@/context/ConfigContext';
import { useLog } from '@/context/LogContext';

export function Header() {
  const { theme, toggleTheme, sidebarOpen, toggleSidebar } = useConfig();
  const { parsedLog, clearLog } = useLog();

  return (
    <header className="flex h-14 items-center justify-between border-b border-slate-200 bg-white px-4 dark:border-slate-700 dark:bg-slate-800">
      <div className="flex items-center gap-3">
        <button
          onClick={toggleSidebar}
          className="rounded-md p-2 hover:bg-slate-100 dark:hover:bg-slate-700"
          aria-label={sidebarOpen ? 'Close sidebar' : 'Open sidebar'}
        >
          <Menu className="h-5 w-5" />
        </button>
        <h1 className="text-lg font-semibold">Camelot Combat Reporter</h1>
        {parsedLog && (
          <span className="rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-700 dark:bg-blue-900 dark:text-blue-200">
            {parsedLog.filename}
          </span>
        )}
      </div>

      <div className="flex items-center gap-2">
        {parsedLog && (
          <button
            onClick={clearLog}
            className="flex items-center gap-1 rounded-md px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-700"
          >
            <X className="h-4 w-4" />
            Clear
          </button>
        )}
        <button
          onClick={toggleTheme}
          className="rounded-md p-2 hover:bg-slate-100 dark:hover:bg-slate-700"
          aria-label={theme === 'light' ? 'Switch to dark mode' : 'Switch to light mode'}
        >
          {theme === 'light' ? (
            <Moon className="h-5 w-5" />
          ) : (
            <Sun className="h-5 w-5" />
          )}
        </button>
      </div>
    </header>
  );
}
