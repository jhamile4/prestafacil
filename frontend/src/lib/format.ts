const formatoSoles = new Intl.NumberFormat('es-PE', {
  style: 'currency',
  currency: 'PEN',
  minimumFractionDigits: 2,
});

const formatoFecha = new Intl.DateTimeFormat('es-PE', { day: '2-digit', month: '2-digit', year: 'numeric' });
const formatoFechaHora = new Intl.DateTimeFormat('es-PE', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

/** 1234.5 → "S/ 1,234.50" */
export function soles(valor: number): string {
  return formatoSoles.format(valor);
}

/**
 * "2026-10-19" → "19/10/2026". Las fechas sin hora se leen como fecha local:
 * new Date("2026-10-19") las tomaría como UTC y en Perú mostraría el día anterior.
 */
export function fecha(valor: string | null | undefined): string {
  if (!valor) return '—';
  const soloFecha = /^\d{4}-\d{2}-\d{2}$/.exec(valor);
  const d = soloFecha ? new Date(`${valor}T00:00:00`) : new Date(valor);
  return formatoFecha.format(d);
}

export function fechaHora(valor: string | null | undefined): string {
  return valor ? formatoFechaHora.format(new Date(valor)) : '—';
}

/** 0.35 → "35.00 %" */
export function porcentaje(fraccion: number, decimales = 2): string {
  return `${(fraccion * 100).toFixed(decimales)} %`;
}

/** Fecha de hoy en formato yyyy-MM-dd, para inputs de tipo date. */
export function hoyIso(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}
