import { describe, expect, it } from 'vitest';
import { tienePermiso } from './permisos';

// Debe coincidir con la matriz de la API (docs/01_requerimientos.md, sección 2.1)
describe('permisos', () => {
  it.each([
    ['Asesor', 'solicitar', true],
    ['Asesor', 'aprobar', false],
    ['Asesor', 'cobrar', false],
    ['Asesor', 'verMorosos', true],
    ['Cajero', 'cobrar', true],
    ['Cajero', 'desembolsar', true],
    ['Cajero', 'solicitar', false],
    ['Cajero', 'verMorosos', false],
    ['Admin', 'gestionarUsuarios', true],
    ['Admin', 'anularPagos', true],
  ] as const)('%s → %s: %s', (rol, permiso, esperado) => {
    expect(tienePermiso(rol, permiso)).toBe(esperado);
  });

  it('sin sesión no hay permisos', () => {
    expect(tienePermiso(undefined, 'solicitar')).toBe(false);
  });
});
