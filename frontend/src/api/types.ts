// Tipos que reflejan los DTO de la API (PrestaFacil.Application).

export type Rol = 'Admin' | 'Asesor' | 'Cajero';
export type EstadoPrestamo = 'Pendiente' | 'Aprobado' | 'Rechazado' | 'Desembolsado' | 'Pagado';
export type EstadoCuota = 'Pendiente' | 'Parcial' | 'Vencida' | 'Pagada';
export type MedioPago = 'Efectivo' | 'Transferencia' | 'Yape' | 'Plin';

export interface PagedResult<T> {
  items: T[];
  total: number;
  pagina: number;
  tamanoPagina: number;
  totalPaginas: number;
}

// ---------- Usuarios y sesión ----------

export interface Usuario {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  email: string;
  rol: Rol;
  activo: boolean;
  fechaCreacion: string;
}

export interface LoginResponse {
  token: string;
  tipoToken: string;
  expiraEn: string;
  usuario: Usuario;
}

export interface UsuarioDirectorio {
  id: number;
  nombreCompleto: string;
  rol: Rol;
}

export interface CrearUsuarioRequest {
  nombreUsuario: string;
  nombreCompleto: string;
  email: string;
  rol: Rol;
  password: string;
}

export interface ActualizarUsuarioRequest {
  nombreCompleto: string;
  email: string;
  rol: Rol;
}

// ---------- Clientes ----------

export interface Cliente {
  id: number;
  dni: string;
  nombres: string;
  apellidos: string;
  nombreCompleto: string;
  fechaNacimiento: string;
  edad: number;
  telefono: string | null;
  email: string | null;
  direccion: string | null;
  ingresoMensual: number;
  gastosMensuales: number;
  ingresoNeto: number;
  activo: boolean;
  fechaCreacion: string;
}

export interface ClienteDatos {
  nombres: string;
  apellidos: string;
  fechaNacimiento: string;
  telefono: string | null;
  email: string | null;
  direccion: string | null;
  ingresoMensual: number;
  gastosMensuales: number;
}

export interface CrearClienteRequest extends ClienteDatos {
  dni: string;
}

export interface PrestamoResumen {
  codigo: string;
  producto: string;
  monto: number;
  plazoMeses: number;
  tea: number;
  cuotaMensual: number;
  estado: string;
  fechaDesembolso: string | null;
  totalCuotas: number;
  cuotasPagadas: number;
  cuotasVencidas: number;
  saldoCapital: number;
  deudaTotal: number;
}

export interface CuotaPorPagar {
  prestamo: string;
  numeroCuota: number;
  fechaVencimiento: string;
  montoCuota: number;
  mora: number;
  pendiente: number;
  estado: string;
}

export interface EstadoCuenta {
  cliente: Cliente;
  prestamos: PrestamoResumen[];
  cuotasPorPagar: CuotaPorPagar[];
  deudaTotal: number;
}

// ---------- Productos y préstamos ----------

export interface Producto {
  id: number;
  nombre: string;
  descripcion: string | null;
  tea: number;
  montoMinimo: number;
  montoMaximo: number;
  plazoMinimoMeses: number;
  plazoMaximoMeses: number;
  tasaMoraDiaria: number;
}

export interface Regla {
  codigo: string;
  descripcion: string;
  cumple: boolean;
  detalle: string;
}

export interface Evaluacion {
  apto: boolean;
  cuotaNueva: number;
  ingresoNeto: number;
  limiteCuotas: number;
  reglas: Regla[];
}

export interface CuotaSimulada {
  numero: number;
  fechaVencimiento: string;
  saldoInicial: number;
  capital: number;
  interes: number;
  cuota: number;
  saldoFinal: number;
}

export interface Simulacion {
  producto: string;
  monto: number;
  plazoMeses: number;
  tea: number;
  tem: number;
  cuotaMensual: number;
  totalIntereses: number;
  totalAPagar: number;
  cronograma: CuotaSimulada[];
  evaluacion: Evaluacion | null;
}

export interface SimularRequest {
  productoId: number;
  monto: number;
  plazoMeses: number;
  clienteId?: number;
}

export interface Prestamo {
  id: number;
  codigo: string;
  clienteId: number;
  clienteDni: string;
  cliente: string;
  producto: string;
  monto: number;
  plazoMeses: number;
  tea: number;
  cuotaMensual: number;
  estado: EstadoPrestamo;
  fechaSolicitud: string;
  fechaAprobacion: string | null;
  fechaDesembolso: string | null;
  motivoRechazo: string | null;
}

export interface Historial {
  estadoAnterior: EstadoPrestamo | null;
  estadoNuevo: EstadoPrestamo;
  usuarioId: number;
  fecha: string;
  comentario: string | null;
}

export interface PrestamoDetalle {
  prestamo: Prestamo;
  tem: number;
  tasaMoraDiaria: number;
  asesorId: number;
  aprobadoPorId: number | null;
  desembolsadoPorId: number | null;
  historial: Historial[];
  evaluacion: Evaluacion | null;
}

export interface Cuota {
  numero: number;
  fechaVencimiento: string;
  saldoInicial: number;
  capital: number;
  interes: number;
  montoCuota: number;
  mora: number;
  totalPagado: number;
  pendiente: number;
  estado: EstadoCuota;
  fechaPagoTotal: string | null;
}

export interface Cronograma {
  codigo: string;
  estado: EstadoPrestamo;
  monto: number;
  cuotaMensual: number;
  totalIntereses: number;
  totalPagado: number;
  deudaPendiente: number;
  cuotas: Cuota[];
}

// ---------- Pagos ----------

export interface Deuda {
  prestamo: string;
  estado: EstadoPrestamo;
  fechaCorte: string;
  cuotasVencidas: number;
  diasAtrasoMaximo: number;
  saldoVencido: number;
  moraPendiente: number;
  totalParaPonerseAlDia: number;
  deudaTotal: number;
  proximaCuota: { numero: number; fechaVencimiento: string; pendiente: number } | null;
}

export interface PagoDetalle {
  numeroCuota: number;
  mora: number;
  interes: number;
  capital: number;
  total: number;
}

export interface Pago {
  id: number;
  prestamo: string;
  fechaPago: string;
  monto: number;
  medioPago: MedioPago;
  numeroOperacion: string | null;
  cajeroId: number;
  anulado: boolean;
  motivoAnulacion: string | null;
  fechaAnulacion: string | null;
  totalMora: number;
  totalInteres: number;
  totalCapital: number;
  detalles: PagoDetalle[];
}

export interface PagoRegistrado {
  pago: Pago;
  estadoPrestamo: EstadoPrestamo;
  deudaRestante: Deuda;
}

// ---------- Reportes ----------

export interface CarteraItem {
  estado: string;
  cantidadPrestamos: number;
  montoTotal: number;
  saldoCapitalPendiente: number;
}

export interface MorosoItem {
  dni: string;
  cliente: string;
  telefono: string | null;
  prestamo: string;
  asesor: string;
  cuotasVencidas: number;
  diasAtraso: number;
  deudaVencida: number;
  moraPendiente: number;
  totalAPagar: number;
}

export interface CobranzaItem {
  cajero: string;
  medioPago: string;
  cantidadPagos: number;
  totalCobrado: number;
  capital: number;
  interes: number;
  mora: number;
}
