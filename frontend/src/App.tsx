import { lazy, Suspense, type ComponentType, type ReactNode } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router';
import { useAuth } from './auth/AuthContext';
import type { Permiso } from './auth/permisos';
import { Layout } from './components/Layout';
import { Cargando } from './components/ui';
import { LoginPage } from './pages/LoginPage';

// Cada página se descarga recién cuando se abre (por ejemplo, Recharts solo con Reportes)
const pagina = <K extends string>(cargar: () => Promise<Record<K, ComponentType>>, nombre: K) =>
  lazy(() => cargar().then((m) => ({ default: m[nombre] })));
const InicioPage = pagina(() => import('./pages/InicioPage'), 'InicioPage');
const ClientesPage = pagina(() => import('./pages/ClientesPage'), 'ClientesPage');
const ClienteDetallePage = pagina(() => import('./pages/ClienteDetallePage'), 'ClienteDetallePage');
const SimuladorPage = pagina(() => import('./pages/SimuladorPage'), 'SimuladorPage');
const PrestamosPage = pagina(() => import('./pages/PrestamosPage'), 'PrestamosPage');
const PrestamoDetallePage = pagina(() => import('./pages/PrestamoDetallePage'), 'PrestamoDetallePage');
const CajaPage = pagina(() => import('./pages/CajaPage'), 'CajaPage');
const ReportesPage = pagina(() => import('./pages/ReportesPage'), 'ReportesPage');
const UsuariosPage = pagina(() => import('./pages/UsuariosPage'), 'UsuariosPage');

/** Exige sesión; si se indica un permiso, también el rol correspondiente. */
function Protegida({ children, permiso }: { children: ReactNode; permiso?: Permiso }) {
  const { usuario, verificando, puede } = useAuth();
  if (verificando) return <Cargando texto="Verificando sesión…" />;
  if (!usuario) return <Navigate to="/login" replace />;
  if (permiso && !puede(permiso)) return <Navigate to="/" replace />;
  return <>{children}</>;
}

export function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={<Cargando />}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            element={
              <Protegida>
                <Layout />
              </Protegida>
            }
          >
            <Route index element={<InicioPage />} />
            <Route path="clientes" element={<ClientesPage />} />
            <Route path="clientes/:id" element={<ClienteDetallePage />} />
            <Route path="simulador" element={<Protegida permiso="solicitar"><SimuladorPage /></Protegida>} />
            <Route path="prestamos" element={<PrestamosPage />} />
            <Route path="prestamos/:id" element={<PrestamoDetallePage />} />
            <Route path="caja" element={<Protegida permiso="cobrar"><CajaPage /></Protegida>} />
            <Route path="reportes" element={<Protegida permiso="verMorosos"><ReportesPage /></Protegida>} />
            <Route path="usuarios" element={<Protegida permiso="gestionarUsuarios"><UsuariosPage /></Protegida>} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
}
