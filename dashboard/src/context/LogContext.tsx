import {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from 'react';
import { parseLogFile, type BrowserParsedLog } from '@/lib/browserFileReader';

interface LogContextValue {
  parsedLog: BrowserParsedLog | null;
  isLoading: boolean;
  error: Error | null;
  loadFile: (file: File) => Promise<void>;
  clearLog: () => void;
}

const LogContext = createContext<LogContextValue | null>(null);

export function LogProvider({ children }: { children: ReactNode }) {
  const [parsedLog, setParsedLog] = useState<BrowserParsedLog | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const loadFile = useCallback(async (file: File) => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await parseLogFile(file);
      setParsedLog(result);
    } catch (err) {
      setError(err instanceof Error ? err : new Error('Failed to parse log file'));
      setParsedLog(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const clearLog = useCallback(() => {
    setParsedLog(null);
    setError(null);
  }, []);

  return (
    <LogContext.Provider
      value={{
        parsedLog,
        isLoading,
        error,
        loadFile,
        clearLog,
      }}
    >
      {children}
    </LogContext.Provider>
  );
}

export function useLog() {
  const context = useContext(LogContext);
  if (!context) {
    throw new Error('useLog must be used within a LogProvider');
  }
  return context;
}
