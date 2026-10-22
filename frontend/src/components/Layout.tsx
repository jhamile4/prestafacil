import { useState, type FormEvent } from 'react';
import { NavLink, Outlet } from 'react-router';
import { BarChart3, Calculator, FileText, Home, KeyRound, LogOut, Menu, UserCog, Users, Wallet, X } from 'lucide-react';
import { useAuth } from '../auth/AuthContext';
import { nombreRol, type Permiso } from '../auth/permisos';
import { authApi } from '../api/endpoints';
import { ApiError } from '../api/client';
import { Aviso, Boton, Entrada, Modal } from './ui';

interface Opcion {
  a: string;
  texto: string;
  icono: typeof Home;
  permiso?: Permiso;
}

const opciones: Opcion[] = [
  { a: '/', texto: 'Inicio', icono: Home },
  { a: '/clientes', texto: 'Clientes', icono: Users },
  { a: '/simulador', texto: 'Simulador', icono: Calculator, permiso: 'solicitar' },
  { a: '/prestamos', texto: 'Préstamos', icono: FileText },
  { a: '/caja', texto: 'Caja', icono: Wallet, permiso: 'cobrar' },
  { a: '/reportes', texto: 'Reportes', icono: BarChart3, permiso: 'verMorosos' },
  { a: '/usuarios', texto: 'Usuarios', icono: UserCog, permiso: 'gestionarUsuarios' },
];

export function Layout() {
  const { usuario, logout, puede } = useAuth();
  const [menuAbierto, setMenuAbierto] = useState(false);
  const [cuentaAbierta, setCuentaAbierta] = useState(false);

  const visibles = opciones.filter((o) => !o.permiso || puede(o.permiso));

  const menu = (
    <nav aria-label="Menú principal" className="flex flex-1 flex-col gap-1 px-3">
      {visibles.map(({ a, texto, icono: Icono }) => (
        <NavLink
          key={a}
          to={a}
          end={a === '/'}
          onClick={() => setMenuAbierto(false)}
          className={({ isActive }) =>
            `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
              isActive ? 'bg-marca-700 text-white' : 'text-stone-300 hover:bg-white/10 hover:text-white'
            }`
          }
        >
          <Icono className="size-4" aria-hidden />
          {texto}
        </NavLink>
      ))}
    </nav>
  );

  const marca = (
    <div className="flex items-center gap-2.5 px-6 py-5">
      <img src="/favicon.svg" alt="" className="size-8" />
      <span className="text-lg font-bold text-white">PrestaFácil</span>
    </div>
  );

  return (
    <div className="min-h-dvh lg:flex">
      {/* Menú lateral en escritorio */}
      <aside className="hidden w-60 shrink-0 flex-col bg-stone-900 lg:flex">
        {marca}
        {menu}
        <p className="px-6 py-4 text-xs text-stone-500">Agencia de demostración</p>
      </aside>

      {/* Menú en celular */}
      {menuAbierto && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div className="absolute inset-0 bg-stone-900/50" onClick={() => setMenuAbierto(false)} />
          <aside className="relative flex h-full w-64 flex-col bg-stone-900">
            <button type="button" className="absolute top-5 right-4 text-stone-400" onClick={() => setMenuAbierto(false)} aria-label="Cerrar menú">
              <X className="size-5" />
            </button>
            {marca}
            {menu}
          </aside>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="no-imprimir sticky top-0 z-30 flex items-center justify-between gap-4 border-b border-stone-200 bg-white/90 px-4 py-3 backdrop-blur sm:px-6">
          <button type="button" className="rounded-md p-1.5 text-stone-600 hover:bg-stone-100 lg:hidden" onClick={() => setMenuAbierto(true)} aria-label="Abrir menú">
            <Menu className="size-5" />
          </button>
          <div className="ml-auto flex items-center gap-3">
            <div className="text-right">
              <p className="text-sm font-semibold text-stone-900">{usuario?.nombreCompleto}</p>
              <p className="text-xs text-stone-500">{usuario && nombreRol[usuario.rol]}</p>
            </div>
            <Boton variante="fantasma" onClick={() => setCuentaAbierta(true)} aria-label="Cambiar contraseña" title="Cambiar contraseña">
              <KeyRound className="size-4" />
            </Boton>
            <Boton variante="secundario" onClick={logout} icono={<LogOut className="size-4" />}>
              Salir
            </Boton>
          </div>
        </header>

        <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-6 sm:px-6 lg:py-8">
          <Outlet />
        </main>
      </div>

      <Modal abierto={cuentaAbierta} titulo="Cambiar contraseña" alCerrar={() => setCuentaAbierta(false)}>
        <CambiarPassword alTerminar={() => setCuentaAbierta(false)} />
      </Modal>
    </div>
  );
}

function CambiarPassword({ alTerminar }: { alTerminar: () => void }) {
  const [actual, setActual] = useState('');
  const [nueva, setNueva] = useState('');
  const [error, setError] = useState<ApiError | null>(null);
  const [listo, setListo] = useState(false);
  const [enviando, setEnviando] = useState(false);

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await authApi.cambiarPassword(actual, nueva);
      setListo(true);
      setTimeout(alTerminar, 1200);
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Error inesperado'));
    } finally {
      setEnviando(false);
    }
  }

  if (listo) return <Aviso tipo="exito">Contraseña actualizada.</Aviso>;

  return (
    <form onSubmit={enviar} className="space-y-4">
      {error && Object.keys(error.campos).length === 0 && <Aviso>{error.message}</Aviso>}
      <Entrada etiqueta="Contraseña actual" type="password" autoComplete="current-password" required value={actual} onChange={(e) => setActual(e.target.value)} error={error?.campos.passwordActual?.[0]} />
      <Entrada etiqueta="Nueva contraseña" type="password" autoComplete="new-password" required value={nueva} onChange={(e) => setNueva(e.target.value)} error={error?.campos.passwordNueva?.join(' ')} ayuda="Mínimo 8 caracteres, con mayúscula, minúscula y número." />
      <div className="flex justify-end gap-2">
        <Boton variante="secundario" onClick={alTerminar}>Cancelar</Boton>
        <Boton type="submit" cargando={enviando}>Guardar</Boton>
      </div>
    </form>
  );
}
