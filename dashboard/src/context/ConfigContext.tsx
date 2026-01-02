import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  type ReactNode,
} from 'react';

export interface WidgetConfig {
  id: string;
  visible: boolean;
  order: number;
}

const DEFAULT_WIDGETS: WidgetConfig[] = [
  { id: 'damage-meter', visible: true, order: 0 },
  { id: 'healing-meter', visible: true, order: 1 },
  { id: 'dps-timeline', visible: true, order: 2 },
  { id: 'damage-breakdown', visible: true, order: 3 },
  { id: 'healing-breakdown', visible: true, order: 4 },
  { id: 'event-timeline', visible: true, order: 5 },
  { id: 'deaths-cc', visible: true, order: 6 },
  { id: 'player-stats', visible: false, order: 7 },
];

interface ConfigContextValue {
  widgets: WidgetConfig[];
  toggleWidget: (widgetId: string) => void;
  reorderWidgets: (widgetIds: string[]) => void;
  theme: 'light' | 'dark';
  toggleTheme: () => void;
  sidebarOpen: boolean;
  toggleSidebar: () => void;
}

const ConfigContext = createContext<ConfigContextValue | null>(null);

const STORAGE_KEY = 'camelot-dashboard-config';

interface StoredConfig {
  widgets: WidgetConfig[];
  theme: 'light' | 'dark';
}

function loadStoredConfig(): StoredConfig | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      return JSON.parse(stored);
    }
  } catch {
    // Ignore errors
  }
  return null;
}

function saveConfig(config: StoredConfig) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(config));
  } catch {
    // Ignore errors
  }
}

export function ConfigProvider({ children }: { children: ReactNode }) {
  const stored = loadStoredConfig();
  const [widgets, setWidgets] = useState<WidgetConfig[]>(stored?.widgets ?? DEFAULT_WIDGETS);
  const [theme, setTheme] = useState<'light' | 'dark'>(stored?.theme ?? 'light');
  const [sidebarOpen, setSidebarOpen] = useState(true);

  // Apply theme to document
  useEffect(() => {
    if (theme === 'dark') {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }, [theme]);

  // Save config when it changes
  useEffect(() => {
    saveConfig({ widgets, theme });
  }, [widgets, theme]);

  const toggleWidget = useCallback((widgetId: string) => {
    setWidgets((prev) =>
      prev.map((w) => (w.id === widgetId ? { ...w, visible: !w.visible } : w))
    );
  }, []);

  const reorderWidgets = useCallback((widgetIds: string[]) => {
    setWidgets((prev) =>
      widgetIds.map((id, index) => {
        const widget = prev.find((w) => w.id === id);
        return widget ? { ...widget, order: index } : { id, visible: true, order: index };
      })
    );
  }, []);

  const toggleTheme = useCallback(() => {
    setTheme((prev) => (prev === 'light' ? 'dark' : 'light'));
  }, []);

  const toggleSidebar = useCallback(() => {
    setSidebarOpen((prev) => !prev);
  }, []);

  return (
    <ConfigContext.Provider
      value={{
        widgets,
        toggleWidget,
        reorderWidgets,
        theme,
        toggleTheme,
        sidebarOpen,
        toggleSidebar,
      }}
    >
      {children}
    </ConfigContext.Provider>
  );
}

export function useConfig() {
  const context = useContext(ConfigContext);
  if (!context) {
    throw new Error('useConfig must be used within a ConfigProvider');
  }
  return context;
}
