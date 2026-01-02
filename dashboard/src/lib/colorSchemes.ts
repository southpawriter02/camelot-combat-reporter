import type { DamageType } from 'camelot-combat-reporter';
import type { TimelineMarkerCategory, PerformanceRating } from 'camelot-combat-reporter';

export const DAMAGE_TYPE_COLORS: Record<DamageType | string, string> = {
  CRUSH: '#8b5cf6',
  SLASH: '#ef4444',
  THRUST: '#f97316',
  HEAT: '#dc2626',
  COLD: '#3b82f6',
  MATTER: '#84cc16',
  BODY: '#10b981',
  SPIRIT: '#a855f7',
  ENERGY: '#06b6d4',
  UNKNOWN: '#6b7280',
};

export const MARKER_CATEGORY_COLORS: Record<TimelineMarkerCategory, string> = {
  DAMAGE_OUTGOING: '#ef4444',
  DAMAGE_INCOMING: '#f97316',
  HEALING_OUTGOING: '#22c55e',
  HEALING_INCOMING: '#10b981',
  CROWD_CONTROL: '#a855f7',
  DEATH: '#dc2626',
};

export const PERFORMANCE_RATING_COLORS: Record<PerformanceRating, string> = {
  EXCELLENT: '#22c55e',
  GOOD: '#84cc16',
  AVERAGE: '#eab308',
  BELOW_AVERAGE: '#f97316',
  POOR: '#ef4444',
};

// Chart color palette for multiple entities
export const CHART_COLORS = [
  '#ef4444', // Red
  '#3b82f6', // Blue
  '#22c55e', // Green
  '#f97316', // Orange
  '#a855f7', // Purple
  '#06b6d4', // Cyan
  '#eab308', // Yellow
  '#ec4899', // Pink
  '#84cc16', // Lime
  '#6366f1', // Indigo
];

export function getEntityColor(index: number): string {
  return CHART_COLORS[index % CHART_COLORS.length]!;
}
