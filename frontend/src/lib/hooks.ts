import { useQuery } from '@tanstack/react-query';
import { authApi, productosApi } from '../api/endpoints';
import { ApiError } from '../api/client';

/** Nombre de cada usuario por id, para mostrar quién aprobó, desembolsó o cobró. */
export function useNombresUsuarios() {
  const { data } = useQuery({
    queryKey: ['directorio'],
    queryFn: authApi.directorio,
    staleTime: 5 * 60_000,
  });
  return (id: number | null | undefined) =>
    id == null ? '—' : (data?.find((u) => u.id === id)?.nombreCompleto ?? `Usuario #${id}`);
}

export function useProductos() {
  return useQuery({ queryKey: ['productos'], queryFn: productosApi.listar, staleTime: 10 * 60_000 });
}

/** Mensaje legible de cualquier error de React Query. */
export function mensajeError(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  return 'Ocurrió un error inesperado.';
}

/** Primer error de validación de un campo (la API usa camelCase en las claves). */
export function errorCampo(error: unknown, campo: string): string | undefined {
  return error instanceof ApiError ? error.campos[campo]?.join(' ') : undefined;
}

/** Hay errores generales (no ligados a un campo) que mostrar arriba del formulario. */
export function errorGeneral(error: unknown): string | null {
  if (!error) return null;
  if (error instanceof ApiError && Object.keys(error.campos).length > 0) return null;
  return mensajeError(error);
}
