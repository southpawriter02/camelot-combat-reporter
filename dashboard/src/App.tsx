import { ConfigProvider } from '@/context/ConfigContext';
import { LogProvider } from '@/context/LogContext';
import { SessionProvider } from '@/context/SessionContext';
import { Layout } from '@/components/layout/Layout';
import { Dashboard } from '@/components/widgets/Dashboard';

function App() {
  return (
    <ConfigProvider>
      <LogProvider>
        <SessionProvider>
          <Layout>
            <Dashboard />
          </Layout>
        </SessionProvider>
      </LogProvider>
    </ConfigProvider>
  );
}

export default App;
