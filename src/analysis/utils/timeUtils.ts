/**
 * Format a duration in milliseconds to a human-readable string
 * @param ms Duration in milliseconds
 * @returns Formatted string like "2m 35s" or "45s"
 */
export function formatDuration(ms: number): string {
  const totalSeconds = Math.floor(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;

  if (minutes > 0) {
    return `${minutes}m ${seconds}s`;
  }
  return `${seconds}s`;
}

/**
 * Calculate duration between two dates in milliseconds
 */
export function calculateDuration(start: Date, end: Date): number {
  return end.getTime() - start.getTime();
}

/**
 * Calculate relative time from a base timestamp
 */
export function getRelativeTime(timestamp: Date, baseTime: Date): number {
  return timestamp.getTime() - baseTime.getTime();
}

/**
 * Format a timestamp as HH:MM:SS
 */
export function formatTimestamp(date: Date): string {
  const hours = date.getHours().toString().padStart(2, '0');
  const minutes = date.getMinutes().toString().padStart(2, '0');
  const seconds = date.getSeconds().toString().padStart(2, '0');
  return `${hours}:${minutes}:${seconds}`;
}

/**
 * Format relative time as +MM:SS from fight start
 */
export function formatRelativeTime(ms: number): string {
  const totalSeconds = Math.floor(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `+${minutes}:${seconds.toString().padStart(2, '0')}`;
}
