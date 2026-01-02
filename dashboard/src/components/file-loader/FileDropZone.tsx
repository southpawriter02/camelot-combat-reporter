import { useCallback, useState, type DragEvent } from 'react';
import { Upload } from 'lucide-react';
import clsx from 'clsx';

interface FileDropZoneProps {
  onFileDrop: (file: File) => void;
  accept?: string;
  disabled?: boolean;
}

export function FileDropZone({
  onFileDrop,
  accept = '.log,.txt',
  disabled = false,
}: FileDropZoneProps) {
  const [isDragOver, setIsDragOver] = useState(false);

  const handleDragOver = useCallback(
    (e: DragEvent<HTMLDivElement>) => {
      e.preventDefault();
      if (!disabled) {
        setIsDragOver(true);
      }
    },
    [disabled]
  );

  const handleDragLeave = useCallback((e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(false);
  }, []);

  const handleDrop = useCallback(
    (e: DragEvent<HTMLDivElement>) => {
      e.preventDefault();
      setIsDragOver(false);

      if (disabled) return;

      const files = e.dataTransfer.files;
      if (files.length > 0) {
        onFileDrop(files[0]!);
      }
    },
    [disabled, onFileDrop]
  );

  const handleFileSelect = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const files = e.target.files;
      if (files && files.length > 0) {
        onFileDrop(files[0]!);
      }
      // Reset input so the same file can be selected again
      e.target.value = '';
    },
    [onFileDrop]
  );

  return (
    <div
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      className={clsx(
        'relative flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-6 transition-colors',
        isDragOver
          ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
          : 'border-slate-300 hover:border-slate-400 dark:border-slate-600 dark:hover:border-slate-500',
        disabled && 'cursor-not-allowed opacity-50'
      )}
    >
      <Upload
        className={clsx(
          'mb-2 h-8 w-8',
          isDragOver ? 'text-blue-500' : 'text-slate-400'
        )}
      />
      <p className="mb-1 text-sm font-medium text-slate-700 dark:text-slate-200">
        {isDragOver ? 'Drop file here' : 'Drag & drop log file'}
      </p>
      <p className="mb-3 text-xs text-slate-500 dark:text-slate-400">
        or click to browse
      </p>
      <input
        type="file"
        accept={accept}
        onChange={handleFileSelect}
        disabled={disabled}
        className="absolute inset-0 cursor-pointer opacity-0"
        aria-label="Select log file"
      />
    </div>
  );
}
