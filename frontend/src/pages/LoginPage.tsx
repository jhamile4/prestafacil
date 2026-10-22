import { useState, type FormEvent } from 'react';
import { Navigate } from 'react-router';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';
import { Aviso, Boton, Entrada } from '../components/ui';

export function LoginPage() {
  const { usuario, login, avisoSesion } = useAuth();
  const [nombreUsuario, setNombreUsuario] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  if (usuario) return <Navigate to="/" replace />;

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await login(nombreUsuario, password);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo iniciar sesión.');
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="grid min-h-dvh lg:grid-cols-2">
      <div className="hidden flex-col justify-between bg-stone-900 p-12 text-white lg:flex">
        <div className="flex items-center gap-2.5">
          <img src="/favicon.svg" alt="" className="size-9" />
          <span className="text-xl font-bold">PrestaFácil</span>
        </div>
        <div>
          <h1 className="text-4xl font-bold leading-tight">
            Préstamos claros,
            <br />
            <span className="text-marca-500">de la solicitud al último pago.</span>
          </h1>
          <ul className="mt-8 space-y-3 text-stone-300">
            <li>✓ Evaluación crediticia automática</li>
            <li>✓ Cronograma por sistema francés</li>
            <li>✓ Cobranza con mora calculada al día</li>
          </ul>
        </div>
        <p className="text-sm text-stone-500">Proyecto de portafolio · ASP.NET Core + React</p>
      </div>

      <div className="flex items-center justify-center p-6">
        <form onSubmit={enviar} className="w-full max-w-sm space-y-5">
          <div className="lg:hidden flex items-center gap-2.5">
            <img src="/favicon.svg" alt="" className="size-9" />
            <span className="text-xl font-bold text-stone-900">PrestaFácil</span>
          </div>
          <div>
            <h2 className="text-2xl font-bold text-stone-900">Iniciar sesión</h2>
            <p className="mt-1 text-sm text-stone-500">Ingresa con tu usuario de la agencia.</p>
          </div>

          {avisoSesion && !error && <Aviso tipo="info">{avisoSesion}</Aviso>}
          {error && <Aviso>{error}</Aviso>}

          <Entrada etiqueta="Usuario" autoComplete="username" autoFocus required value={nombreUsuario} onChange={(e) => setNombreUsuario(e.target.value)} />
          <Entrada etiqueta="Contraseña" type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} />
          <Boton type="submit" cargando={enviando} className="w-full">
            Ingresar
          </Boton>

          <p className="rounded-lg bg-stone-100 px-3 py-2.5 text-xs text-stone-600">
            Usuarios de prueba: <strong>admin</strong>, <strong>asesor1</strong>, <strong>cajero1</strong>. La contraseña de
            demostración se configura en la API (<code>Demo:PasswordInicial</code>).
          </p>
        </form>
      </div>
    </div>
  );
}
