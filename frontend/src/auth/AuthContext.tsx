import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { authApi } from '../api/endpoints';
import { registrarAlExpirarSesion, sesion } from '../api/client';
import type { Usuario } from '../api/types';
import { tienePermiso, type Permiso } from './permisos';

interface AuthValor {
  usuario: Usuario | null;
  /** true mientras se verifica el token guardado al abrir la app. */
  verificando: boolean;
  /** Mensaje cuando la sesión se cerró sola (token vencido o usuario desactivado). */
  avisoSesion: string | null;
  login: (nombreUsuario: string, password: string) => Promise<void>;
  logout: () => void;
  puede: (permiso: Permiso) => boolean;
}

const AuthContext = createContext<AuthValor | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [usuario, setUsuario] = useState<Usuario | null>(null);
  const [verificando, setVerificando] = useState(() => sesion.obtenerToken() !== null);
  const [avisoSesion, setAvisoSesion] = useState<string | null>(null);

  const cerrarSesion = useCallback(
    (aviso: string | null) => {
      sesion.borrarToken();
      setUsuario(null);
      setAvisoSesion(aviso);
      queryClient.clear();
    },
    [queryClient],
  );

  // Si la API responde 401 en cualquier pantalla, se vuelve al login
  useEffect(() => {
    registrarAlExpirarSesion(() =>
      cerrarSesion('Tu sesión expiró o tu usuario cambió. Vuelve a iniciar sesión.'),
    );
  }, [cerrarSesion]);

  // Al abrir la app con un token guardado, se valida contra la API
  useEffect(() => {
    if (!sesion.obtenerToken()) return;
    authApi
      .yo()
      .then(setUsuario)
      .catch(() => cerrarSesion(null))
      .finally(() => setVerificando(false));
  }, [cerrarSesion]);

  const login = useCallback(async (nombreUsuario: string, password: string) => {
    const respuesta = await authApi.login(nombreUsuario, password);
    sesion.guardarToken(respuesta.token);
    setAvisoSesion(null);
    setUsuario(respuesta.usuario);
  }, []);

  const valor = useMemo<AuthValor>(
    () => ({
      usuario,
      verificando,
      avisoSesion,
      login,
      logout: () => cerrarSesion(null),
      puede: (permiso) => tienePermiso(usuario?.rol, permiso),
    }),
    [usuario, verificando, avisoSesion, login, cerrarSesion],
  );

  return <AuthContext.Provider value={valor}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthValor {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth debe usarse dentro de <AuthProvider>');
  return ctx;
}
