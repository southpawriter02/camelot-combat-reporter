import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useMemo,
  type ReactNode,
} from 'react';
import {
  CombatAnalyzer,
  type AnalysisResult,
  type CombatSession,
  type FightSummary,
  type TimelineView,
  type TimelineFilterConfig,
  type PlayerAggregateStats,
} from 'camelot-combat-reporter';
import { useLog } from './LogContext';

interface SessionContextValue {
  analysisResult: AnalysisResult | null;
  sessions: CombatSession[];
  selectedSession: CombatSession | null;
  sessionSummary: FightSummary | null;
  selectSession: (sessionId: string) => void;
  getTimeline: (filter?: Partial<TimelineFilterConfig>) => TimelineView | null;
  getAllPlayerStats: () => Map<string, PlayerAggregateStats>;
}

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const { parsedLog } = useLog();
  const [analysisResult, setAnalysisResult] = useState<AnalysisResult | null>(null);
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null);

  const analyzer = useMemo(() => new CombatAnalyzer(), []);

  // Analyze log when it changes
  useEffect(() => {
    if (parsedLog && parsedLog.events.length > 0) {
      const result = analyzer.analyzeEvents(parsedLog.events);
      setAnalysisResult(result);
      // Auto-select first session if available
      if (result.sessions.length > 0) {
        setSelectedSessionId(result.sessions[0]!.id);
      }
    } else {
      setAnalysisResult(null);
      setSelectedSessionId(null);
    }
  }, [parsedLog, analyzer]);

  const sessions = analysisResult?.sessions ?? [];

  const selectedSession = useMemo(
    () => sessions.find((s) => s.id === selectedSessionId) ?? null,
    [sessions, selectedSessionId]
  );

  const sessionSummary = useMemo(() => {
    if (!selectedSession) return null;
    return analyzer.getSummary(selectedSession);
  }, [selectedSession, analyzer]);

  const selectSession = useCallback((sessionId: string) => {
    setSelectedSessionId(sessionId);
  }, []);

  const getTimeline = useCallback(
    (filter?: Partial<TimelineFilterConfig>): TimelineView | null => {
      if (!selectedSession) return null;
      return analyzer.getTimeline(selectedSession, filter);
    },
    [selectedSession, analyzer]
  );

  const getAllPlayerStats = useCallback((): Map<string, PlayerAggregateStats> => {
    if (!sessions.length) return new Map();
    return analyzer.getAllPlayerStats(sessions);
  }, [sessions, analyzer]);

  return (
    <SessionContext.Provider
      value={{
        analysisResult,
        sessions,
        selectedSession,
        sessionSummary,
        selectSession,
        getTimeline,
        getAllPlayerStats,
      }}
    >
      {children}
    </SessionContext.Provider>
  );
}

export function useSession() {
  const context = useContext(SessionContext);
  if (!context) {
    throw new Error('useSession must be used within a SessionProvider');
  }
  return context;
}
