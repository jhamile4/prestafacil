import type { Rol } from '../api/types';

/**
 * Misma matriz de permisos que la API (docs/01_requerimientos.md, sección 2.1).
 * La interfaz la usa para mostrar u ocultar opciones; quien decide de verdad es la API.
 */
export const permisos = {
  editarClientes: ['Admin', 'Asesor'],
  solicitar: ['Admin', 'Asesor'],
  aprobar: ['Admin'],
  desembolsar: ['Admin', 'Cajero'],
  cobrar: ['Admin', 'Cajero'],
  anularPagos: ['Admin'],
  verMorosos: ['Admin', 'Asesor'],
  verReportes: ['Admin'],
  gestionarUsuarios: ['Admin'],
} as const satisfies Record<string, readonly Rol[]>;

export type Permiso = keyof typeof permisos;

export function tienePermiso(rol: Rol | undefined, permiso: Permiso): boolean {
  return rol !== undefined && (permisos[permiso] as readonly Rol[]).includes(rol);
}

export const nombreRol: Record<Rol, string> = {
  Admin: 'Administrador',
  Asesor: 'Asesor de créditos',
  Cajero: 'Cajero',
};
