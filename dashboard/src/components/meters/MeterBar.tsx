import clsx from 'clsx';

interface MeterBarProps {
  percentage: number;
  color?: string;
  className?: string;
}

export function MeterBar({ percentage, color = '#ef4444', className }: MeterBarProps) {
  return (
    <div
      className={clsx(
        'h-1.5 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-700',
        className
      )}
    >
      <div
        className="h-full rounded-full transition-all duration-300"
        style={{
          width: `${Math.min(100, Math.max(0, percentage))}%`,
          backgroundColor: color,
        }}
      />
    </div>
  );
}
