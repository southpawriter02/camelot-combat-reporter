import { useConfig } from '@/context/ConfigContext';
import { FileLoader } from '../file-loader/FileLoader';
import { SessionSelector } from '../session/SessionSelector';
import { WidgetConfig } from '../widgets/WidgetConfig';
import { ExportButtons } from '../export/ExportButtons';

export function Sidebar() {
  const { sidebarOpen } = useConfig();

  if (!sidebarOpen) {
    return null;
  }

  return (
    <aside className="flex w-72 flex-col gap-4 overflow-y-auto border-r border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-800/50">
      <FileLoader />
      <SessionSelector />
      <WidgetConfig />
      <ExportButtons />
    </aside>
  );
}
