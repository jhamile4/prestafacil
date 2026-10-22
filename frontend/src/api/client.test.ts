import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError, query, registrarAlExpirarSesion, sesion } from './client';

function respuesta(status: number, cuerpo?: unknown) {
  return new Response(cuerpo === undefined ? null : JSON.stringify(cuerpo), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  });
}

afterEach(() => vi.unstubAllGlobals());

describe('api', () => {
  it('envía el token guardado en la cabecera Authorization', async () => {
    sesion.guardarToken('abc');
    const fetchMock = vi.fn().mockResolvedValue(respuesta(200, { ok: true }));
    vi.stubGlobal('fetch', fetchMock);

    await api('/clientes');

    const [url, opciones] = fetchMock.mock.calls[0]!;
    expect(url).toBe('/api/clientes');
    expect(opciones.headers.Authorization).toBe('Bearer abc');
  });

  it('convierte un ValidationProblemDetails en ApiError con errores por campo', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(respuesta(400, {
      title: 'Hay datos inválidos en la petición.',
      errors: { dni: ['El DNI debe tener exactamente 8 dígitos.'] },
    })));

    const error = (await api('/clientes', { method: 'POST', body: {} }).catch((e: unknown) => e)) as ApiError;

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(400);
    expect(error.campos.dni).toEqual(['El DNI debe tener exactamente 8 dígitos.']);
  });

  it('usa el detail de la regla de negocio como mensaje', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(respuesta(422, { detail: 'El pago supera la deuda (RN-10).' })));

    await expect(api('/prestamos/1/pagos')).rejects.toThrow('El pago supera la deuda (RN-10).');
  });

  it('avisa que la sesión expiró cuando un endpoint responde 401', async () => {
    sesion.guardarToken('vencido');
    const alExpirar = vi.fn();
    registrarAlExpirarSesion(alExpirar);
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(respuesta(401)));

    await expect(api('/clientes')).rejects.toThrow('Tu sesión expiró');
    expect(alExpirar).toHaveBeenCalledOnce();
  });

  it('no cierra la sesión por un 401 del login (contraseña incorrecta)', async () => {
    const alExpirar = vi.fn();
    registrarAlExpirarSesion(alExpirar);
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(respuesta(401, { detail: 'Usuario o contraseña incorrectos.' })));

    await expect(api('/auth/login', { method: 'POST', body: {} })).rejects.toThrow('Usuario o contraseña incorrectos.');
    expect(alExpirar).not.toHaveBeenCalled();
  });

  it('da un mensaje claro si la API está apagada', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    await expect(api('/clientes')).rejects.toThrow('No se pudo conectar con el servidor');
  });
});

describe('query', () => {
  it('omite los parámetros vacíos', () => {
    expect(query({ buscar: '', estado: 'Pendiente', pagina: 2, clienteId: undefined })).toBe('?estado=Pendiente&pagina=2');
    expect(query({})).toBe('');
  });
});
