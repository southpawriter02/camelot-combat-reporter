import type { PerformanceRating as PerformanceRatingType } from 'camelot-combat-reporter';
import { PERFORMANCE_RATING_COLORS } from '@/lib/colorSchemes';
import clsx from 'clsx';

interface PerformanceRatingProps {
  rating: PerformanceRatingType;
  size?: 'sm' | 'md';
}

const RATING_LABELS: Record<PerformanceRatingType, string> = {
  EXCELLENT: 'Excellent',
  GOOD: 'Good',
  AVERAGE: 'Average',
  BELOW_AVERAGE: 'Below Avg',
  POOR: 'Poor',
};

export function PerformanceRatingBadge({ rating, size = 'sm' }: PerformanceRatingProps) {
  const color = PERFORMANCE_RATING_COLORS[rating];
  const label = RATING_LABELS[rating];

  return (
    <span
      className={clsx(
        'inline-flex items-center rounded-full font-medium',
        size === 'sm' && 'px-2 py-0.5 text-[10px]',
        size === 'md' && 'px-2.5 py-1 text-xs'
      )}
      style={{
        backgroundColor: `${color}20`,
        color: color,
      }}
    >
      {label}
    </span>
  );
}
