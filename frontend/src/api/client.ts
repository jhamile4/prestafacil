// Cliente HTTP: agrega el token, convierte los errores ProblemDetails en
// ApiError y avisa cuando la sesión deja de ser válida (401).

const CLAVE_TOKEN = 'prestafacil.token';

export class ApiError extends Error {
  readonly status: number;
  /** Errores por campo de una respuesta 400 (ValidationProblemDetails). */
  readonly campos: Record<string, string[]>;

  constructor(status: number, mensaje: string, campos: Record<string, string[]> = {}) {
    super(mensaje);
    this.status = status;
    this.campos = campos;
  }
}

export const sesion = {
  obtenerToken: () => localStorage.getItem(CLAVE_TOKEN),
  guardarToken: (token: string) => localStorage.setItem(CLAVE_TOKEN, token),
  borrarToken: () => localStorage.removeItem(CLAVE_TOKEN),
};

let alExpirarSesion: () => void = () => {};

/** El AuthProvider registra aquí qué hacer cuando la API responde 401. */
export function registrarAlExpirarSesion(fn: () => void) {
  alExpirarSesion = fn;
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

async function leerError(respuesta: Response): Promise<ApiError> {
  let cuerpo: ProblemDetails = {};
  try {
    cuerpo = (await respuesta.json()) as ProblemDetails;
  } catch {
    // Respuesta sin JSON (por ejemplo, la API está apagada detrás del proxy)
  }

  const porDefecto: Record<number, string> = {
    401: 'Tu sesión expiró. Vuelve a iniciar sesión.',
    403: 'No tienes permiso para realizar esta acción.',
    429: 'Demasiados intentos. Espera un minuto e inténtalo de nuevo.',
    502: 'No se pudo conectar con el servidor.',
    503: 'No se pudo conectar con el servidor.',
    504: 'No se pudo conectar con el servidor.',
  };

  const mensaje =
    cuerpo.detail ?? porDefecto[respuesta.status] ?? cuerpo.title ?? `Error ${respuesta.status}`;
  return new ApiError(respuesta.status, mensaje, cuerpo.errors ?? {});
}

export async function api<T>(
  ruta: string,
  opciones: { method?: string; body?: unknown; signal?: AbortSignal } = {},
): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const token = sesion.obtenerToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (opciones.body !== undefined) headers['Content-Type'] = 'application/json';

  let respuesta: Response;
  try {
    respuesta = await fetch(`/api${ruta}`, {
      method: opciones.method ?? 'GET',
      headers,
      body: opciones.body !== undefined ? JSON.stringify(opciones.body) : undefined,
      signal: opciones.signal,
    });
  } catch (e) {
    if (e instanceof DOMException && e.name === 'AbortError') throw e;
    throw new ApiError(0, 'No se pudo conectar con el servidor. Revisa que la API esté encendida.');
  }

  if (!respuesta.ok) {
    const error = await leerError(respuesta);
    // Un 401 fuera del login significa que el token venció o fue revocado
    if (respuesta.status === 401 && token && ruta !== '/auth/login') alExpirarSesion();
    throw error;
  }

  if (respuesta.status === 204) return undefined as T;
  return (await respuesta.json()) as T;
}

/** Arma un query string ignorando valores vacíos. */
export function query(params: Record<string, string | number | boolean | null | undefined>) {
  const qs = new URLSearchParams();
  for (const [clave, valor] of Object.entries(params)) {
    if (valor !== undefined && valor !== null && valor !== '') qs.set(clave, String(valor));
  }
  const texto = qs.toString();
  return texto ? `?${texto}` : '';
}
