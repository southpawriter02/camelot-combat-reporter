import { useCallback } from 'react';
import { FileText, AlertCircle, Loader2 } from 'lucide-react';
import { useLog } from '@/context/LogContext';
import { isValidLogFile } from '@/lib/browserFileReader';
import { FileDropZone } from './FileDropZone';

export function FileLoader() {
  const { parsedLog, isLoading, error, loadFile } = useLog();

  const handleFileDrop = useCallback(
    async (file: File) => {
      if (!isValidLogFile(file)) {
        return;
      }
      await loadFile(file);
    },
    [loadFile]
  );

  if (isLoading) {
    return (
      <div className="card">
        <div className="flex items-center gap-3">
          <Loader2 className="h-5 w-5 animate-spin text-blue-500" />
          <span className="text-sm text-slate-600 dark:text-slate-300">
            Parsing log file...
          </span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="card">
        <div className="mb-3 flex items-center gap-2 text-red-600 dark:text-red-400">
          <AlertCircle className="h-5 w-5" />
          <span className="text-sm font-medium">Error parsing file</span>
        </div>
        <p className="mb-3 text-xs text-slate-600 dark:text-slate-400">
          {error.message}
        </p>
        <FileDropZone onFileDrop={handleFileDrop} />
      </div>
    );
  }

  if (parsedLog) {
    return (
      <div className="card">
        <div className="mb-2 flex items-center gap-2">
          <FileText className="h-5 w-5 text-blue-500" />
          <span className="text-sm font-medium text-slate-700 dark:text-slate-200">
            File Loaded
          </span>
        </div>
        <dl className="space-y-1 text-xs">
          <div className="flex justify-between">
            <dt className="text-slate-500 dark:text-slate-400">Name:</dt>
            <dd className="font-medium text-slate-700 dark:text-slate-200 truncate max-w-[140px]">
              {parsedLog.filename}
            </dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-slate-500 dark:text-slate-400">Lines:</dt>
            <dd className="font-medium text-slate-700 dark:text-slate-200">
              {parsedLog.totalLines.toLocaleString()}
            </dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-slate-500 dark:text-slate-400">Events:</dt>
            <dd className="font-medium text-slate-700 dark:text-slate-200">
              {parsedLog.parsedLines.toLocaleString()}
            </dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-slate-500 dark:text-slate-400">Parse time:</dt>
            <dd className="font-medium text-slate-700 dark:text-slate-200">
              {parsedLog.parseEndTime.getTime() - parsedLog.parseStartTime.getTime()}ms
            </dd>
          </div>
        </dl>
      </div>
    );
  }

  return (
    <div className="card">
      <h2 className="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">
        Load Combat Log
      </h2>
      <FileDropZone onFileDrop={handleFileDrop} />
      <p className="mt-2 text-xs text-slate-500 dark:text-slate-400">
        Supports .log and .txt files up to 100MB
      </p>
    </div>
  );
}
