import { describe, expect, it } from 'vitest';
import { fecha, porcentaje, soles } from './format';

describe('format', () => {
  it('muestra soles con separador de miles y 2 decimales', () => {
    expect(soles(1234.5)).toMatch(/S\/\s?1,234\.50/);
  });

  it('no corre un día las fechas sin hora (no las toma como UTC)', () => {
    expect(fecha('2026-10-19')).toBe('19/10/2026');
  });

  it('muestra guion cuando no hay fecha', () => {
    expect(fecha(null)).toBe('—');
  });

  it('convierte fracción a porcentaje', () => {
    expect(porcentaje(0.35)).toBe('35.00 %');
    expect(porcentaje(0.0253240572, 4)).toBe('2.5324 %');
  });
});
