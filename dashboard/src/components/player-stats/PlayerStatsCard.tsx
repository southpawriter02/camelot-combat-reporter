import { useMemo } from 'react';
import { useSession } from '@/context/SessionContext';
import { WidgetContainer } from '../widgets/WidgetContainer';
import { PerformanceRatingBadge } from './PerformanceRating';

export function PlayerStatsCard() {
  const { getAllPlayerStats } = useSession();

  const playerStats = useMemo(() => {
    const stats = getAllPlayerStats();
    return Array.from(stats.entries())
      .map(([name, stat]) => ({ name, ...stat }))
      .sort((a, b) => b.avgPerformanceScore - a.avgPerformanceScore)
      .slice(0, 10);
  }, [getAllPlayerStats]);

  if (playerStats.length === 0) {
    return (
      <WidgetContainer title="Player Statistics" size="full">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          No player statistics available
        </p>
      </WidgetContainer>
    );
  }

  return (
    <WidgetContainer title="Player Statistics" size="full">
      <div className="overflow-x-auto">
        <table className="w-full text-xs">
          <thead>
            <tr className="border-b border-slate-200 text-left dark:border-slate-700">
              <th className="pb-2 font-medium text-slate-500 dark:text-slate-400">Player</th>
              <th className="pb-2 font-medium text-slate-500 dark:text-slate-400">Sessions</th>
              <th className="pb-2 font-medium text-slate-500 dark:text-slate-400">K/D/A</th>
              <th className="pb-2 font-medium text-slate-500 dark:text-slate-400">Avg DPS</th>
              <th className="pb-2 font-medium text-slate-500 dark:text-slate-400">Avg HPS</th>
              <th className="pb-2 font-medium text-slate-500 dark:text-slate-400">Score</th>
              <th className="pb-2 font-medium text-slate-500 dark:text-slate-400">Rating</th>
            </tr>
          </thead>
          <tbody>
            {playerStats.map((player) => (
              <tr
                key={player.name}
                className="border-b border-slate-100 dark:border-slate-700/50"
              >
                <td className="py-2 font-medium text-slate-700 dark:text-slate-200">
                  {player.name}
                </td>
                <td className="py-2 text-slate-600 dark:text-slate-300">
                  {player.totalSessions}
                </td>
                <td className="py-2 text-slate-600 dark:text-slate-300">
                  <span className="text-green-600">{player.totalKills}</span>
                  {' / '}
                  <span className="text-red-600">{player.totalDeaths}</span>
                  {' / '}
                  <span className="text-blue-600">{player.totalAssists}</span>
                </td>
                <td className="py-2 text-slate-600 dark:text-slate-300">
                  {player.avgDPS.toFixed(1)}
                </td>
                <td className="py-2 text-slate-600 dark:text-slate-300">
                  {player.avgHPS.toFixed(1)}
                </td>
                <td className="py-2 text-slate-600 dark:text-slate-300">
                  {player.avgPerformanceScore.toFixed(0)}
                </td>
                <td className="py-2">
                  <PerformanceRatingBadge
                    rating={player.bestFight?.performanceRating ?? 'AVERAGE'}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </WidgetContainer>
  );
}
