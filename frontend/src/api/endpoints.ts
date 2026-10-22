import { api, query } from './client';
import type {
  ActualizarUsuarioRequest,
  CarteraItem,
  Cliente,
  ClienteDatos,
  CobranzaItem,
  CrearClienteRequest,
  CrearUsuarioRequest,
  Cronograma,
  Deuda,
  EstadoCuenta,
  EstadoPrestamo,
  Evaluacion,
  LoginResponse,
  MedioPago,
  MorosoItem,
  PagedResult,
  Pago,
  PagoRegistrado,
  Prestamo,
  PrestamoDetalle,
  Producto,
  Simulacion,
  SimularRequest,
  Usuario,
  UsuarioDirectorio,
} from './types';

export const authApi = {
  login: (nombreUsuario: string, password: string) =>
    api<LoginResponse>('/auth/login', { method: 'POST', body: { nombreUsuario, password } }),
  yo: () => api<Usuario>('/auth/yo'),
  cambiarPassword: (passwordActual: string, passwordNueva: string) =>
    api<void>('/auth/cambiar-password', { method: 'POST', body: { passwordActual, passwordNueva } }),
  directorio: () => api<UsuarioDirectorio[]>('/auth/directorio'),
};

export const clientesApi = {
  buscar: (p: { buscar?: string; soloActivos?: boolean; pagina?: number; tamanoPagina?: number }) =>
    api<PagedResult<Cliente>>(`/clientes${query(p)}`),
  obtener: (id: number) => api<Cliente>(`/clientes/${id}`),
  crear: (datos: CrearClienteRequest) => api<Cliente>('/clientes', { method: 'POST', body: datos }),
  actualizar: (id: number, datos: ClienteDatos) =>
    api<Cliente>(`/clientes/${id}`, { method: 'PUT', body: datos }),
  desactivar: (id: number) => api<void>(`/clientes/${id}/desactivar`, { method: 'PATCH' }),
  activar: (id: number) => api<void>(`/clientes/${id}/activar`, { method: 'PATCH' }),
  estadoCuenta: (id: number) => api<EstadoCuenta>(`/clientes/${id}/estado-cuenta`),
};

export const productosApi = {
  listar: () => api<Producto[]>('/productos'),
};

export const prestamosApi = {
  buscar: (p: { estado?: EstadoPrestamo; clienteId?: number; pagina?: number; tamanoPagina?: number }) =>
    api<PagedResult<Prestamo>>(`/prestamos${query(p)}`),
  obtener: (id: number) => api<PrestamoDetalle>(`/prestamos/${id}`),
  simular: (datos: SimularRequest) => api<Simulacion>('/prestamos/simular', { method: 'POST', body: datos }),
  solicitar: (datos: { clienteId: number; productoId: number; monto: number; plazoMeses: number }) =>
    api<PrestamoDetalle>('/prestamos', { method: 'POST', body: datos }),
  evaluar: (id: number) => api<Evaluacion>(`/prestamos/${id}/evaluacion`),
  aprobar: (id: number, comentario?: string) =>
    api<PrestamoDetalle>(`/prestamos/${id}/aprobar`, { method: 'POST', body: { comentario } }),
  rechazar: (id: number, motivo: string) =>
    api<PrestamoDetalle>(`/prestamos/${id}/rechazar`, { method: 'POST', body: { motivo } }),
  desembolsar: (id: number) =>
    api<Cronograma>(`/prestamos/${id}/desembolsar`, { method: 'POST', body: {} }),
  cronograma: (id: number) => api<Cronograma>(`/prestamos/${id}/cronograma`),
};

export const pagosApi = {
  deuda: (prestamoId: number) => api<Deuda>(`/prestamos/${prestamoId}/deuda`),
  listar: (prestamoId: number) => api<Pago[]>(`/prestamos/${prestamoId}/pagos`),
  registrar: (prestamoId: number, datos: { monto: number; medioPago: MedioPago; numeroOperacion?: string }) =>
    api<PagoRegistrado>(`/prestamos/${prestamoId}/pagos`, { method: 'POST', body: datos }),
  anular: (prestamoId: number, pagoId: number, motivo: string) =>
    api<Pago>(`/prestamos/${prestamoId}/pagos/${pagoId}/anular`, { method: 'POST', body: { motivo } }),
};

export const reportesApi = {
  cartera: () => api<CarteraItem[]>('/reportes/cartera'),
  morosos: (diasMinimos = 1) => api<MorosoItem[]>(`/reportes/morosos${query({ diasMinimos })}`),
  cobranza: (desde: string, hasta: string) =>
    api<CobranzaItem[]>(`/reportes/cobranza${query({ desde, hasta })}`),
  procesarMora: () =>
    api<{ fechaCorte: string; cuotasActualizadas: number }>('/mora/procesar', { method: 'POST' }),
};

export const usuariosApi = {
  listar: () => api<Usuario[]>('/usuarios'),
  crear: (datos: CrearUsuarioRequest) => api<Usuario>('/usuarios', { method: 'POST', body: datos }),
  actualizar: (id: number, datos: ActualizarUsuarioRequest) =>
    api<Usuario>(`/usuarios/${id}`, { method: 'PUT', body: datos }),
  desactivar: (id: number) => api<void>(`/usuarios/${id}/desactivar`, { method: 'PATCH' }),
  activar: (id: number) => api<void>(`/usuarios/${id}/activar`, { method: 'PATCH' }),
  restablecerPassword: (id: number, passwordNueva: string) =>
    api<void>(`/usuarios/${id}/restablecer-password`, { method: 'POST', body: { passwordNueva } }),
};
