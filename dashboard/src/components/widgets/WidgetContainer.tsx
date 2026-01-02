import type { ReactNode } from 'react';
import clsx from 'clsx';

interface WidgetContainerProps {
  title: string;
  children: ReactNode;
  className?: string;
  size?: 'small' | 'medium' | 'large' | 'full';
}

export function WidgetContainer({
  title,
  children,
  className,
  size = 'medium',
}: WidgetContainerProps) {
  return (
    <div
      className={clsx(
        'card',
        size === 'small' && 'col-span-1',
        size === 'medium' && 'col-span-1 lg:col-span-2',
        size === 'large' && 'col-span-1 lg:col-span-3',
        size === 'full' && 'col-span-full',
        className
      )}
    >
      <h3 className="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">
        {title}
      </h3>
      {children}
    </div>
  );
}
