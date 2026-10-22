// Componentes visuales reutilizables. Estilos con Tailwind.
import { useEffect, useId, useRef, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react';
import { AlertCircle, CheckCircle2, Loader2, X } from 'lucide-react';
import type { EstadoCuota, EstadoPrestamo } from '../api/types';

const cx = (...clases: (string | false | null | undefined)[]) => clases.filter(Boolean).join(' ');

// ---------- Botón ----------

type Variante = 'primario' | 'secundario' | 'peligro' | 'fantasma';

const estilosBoton: Record<Variante, string> = {
  primario: 'bg-marca-700 text-white hover:bg-marca-800 focus-visible:outline-marca-700',
  secundario: 'bg-white text-stone-700 ring-1 ring-inset ring-stone-300 hover:bg-stone-50 focus-visible:outline-stone-400',
  peligro: 'bg-red-600 text-white hover:bg-red-700 focus-visible:outline-red-600',
  fantasma: 'text-stone-600 hover:bg-stone-100 focus-visible:outline-stone-400',
};

export function Boton({
  variante = 'primario',
  cargando = false,
  icono,
  className,
  children,
  disabled,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variante?: Variante; cargando?: boolean; icono?: ReactNode }) {
  return (
    <button
      type="button"
      disabled={disabled || cargando}
      className={cx(
        'inline-flex items-center justify-center gap-2 rounded-lg px-3.5 py-2 text-sm font-semibold transition-colors',
        'focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-60',
        estilosBoton[variante],
        className,
      )}
      {...props}
    >
      {cargando ? <Loader2 className="size-4 animate-spin" aria-hidden /> : icono}
      {children}
    </button>
  );
}

// ---------- Campos de formulario ----------

interface CampoBase {
  etiqueta: string;
  error?: string;
  ayuda?: string;
}

function Campo({ id, etiqueta, error, ayuda, children }: CampoBase & { id: string; children: ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-sm font-medium text-stone-700">
        {etiqueta}
      </label>
      {children}
      {error ? (
        <p id={`${id}-error`} className="text-sm text-red-600">{error}</p>
      ) : ayuda ? (
        <p className="text-xs text-stone-500">{ayuda}</p>
      ) : null}
    </div>
  );
}

const estiloControl = (error?: string) =>
  cx(
    'block w-full rounded-lg border bg-white px-3 py-2 text-sm shadow-xs transition',
    'focus:outline-none focus:ring-2 focus:ring-marca-500/40',
    error ? 'border-red-400 focus:border-red-500' : 'border-stone-300 focus:border-marca-500',
  );

export function Entrada({ etiqueta, error, ayuda, id, className, ...props }: CampoBase & InputHTMLAttributes<HTMLInputElement>) {
  const generado = useId();
  const campoId = id ?? generado;
  return (
    <Campo id={campoId} etiqueta={etiqueta} error={error} ayuda={ayuda}>
      <input
        id={campoId}
        aria-invalid={!!error}
        aria-describedby={error ? `${campoId}-error` : undefined}
        className={cx(estiloControl(error), className)}
        {...props}
      />
    </Campo>
  );
}

export function Seleccion({ etiqueta, error, ayuda, id, children, ...props }: CampoBase & SelectHTMLAttributes<HTMLSelectElement>) {
  const generado = useId();
  const campoId = id ?? generado;
  return (
    <Campo id={campoId} etiqueta={etiqueta} error={error} ayuda={ayuda}>
      <select id={campoId} aria-invalid={!!error} className={estiloControl(error)} {...props}>
        {children}
      </select>
    </Campo>
  );
}

export function AreaTexto({ etiqueta, error, ayuda, id, ...props }: CampoBase & TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const generado = useId();
  const campoId = id ?? generado;
  return (
    <Campo id={campoId} etiqueta={etiqueta} error={error} ayuda={ayuda}>
      <textarea id={campoId} rows={3} aria-invalid={!!error} className={estiloControl(error)} {...props} />
    </Campo>
  );
}

// ---------- Contenedores ----------

export function Tarjeta({ titulo, acciones, children, className }: { titulo?: ReactNode; acciones?: ReactNode; children: ReactNode; className?: string }) {
  return (
    <section className={cx('rounded-xl border border-stone-200 bg-white shadow-xs', className)}>
      {(titulo || acciones) && (
        <header className="flex flex-wrap items-center justify-between gap-3 border-b border-stone-100 px-5 py-3.5">
          {typeof titulo === 'string' ? <h2 className="text-base font-semibold text-stone-900">{titulo}</h2> : <div className="flex-1">{titulo}</div>}
          {acciones}
        </header>
      )}
      <div className="p-5">{children}</div>
    </section>
  );
}

export function EncabezadoPagina({ titulo, descripcion, acciones }: { titulo: string; descripcion?: ReactNode; acciones?: ReactNode }) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-stone-900">{titulo}</h1>
        {descripcion && <p className="mt-1 text-sm text-stone-500">{descripcion}</p>}
      </div>
      {acciones && <div className="flex flex-wrap gap-2">{acciones}</div>}
    </div>
  );
}

export function Indicador({ etiqueta, valor, detalle, tono = 'neutro' }: { etiqueta: string; valor: ReactNode; detalle?: ReactNode; tono?: 'neutro' | 'alerta' | 'exito' }) {
  const colores = { neutro: 'text-stone-900', alerta: 'text-red-600', exito: 'text-marca-700' };
  return (
    <div className="rounded-xl border border-stone-200 bg-white p-5 shadow-xs">
      <p className="text-sm font-medium text-stone-500">{etiqueta}</p>
      <p className={cx('tabular mt-2 text-2xl font-bold', colores[tono])}>{valor}</p>
      {detalle && <p className="mt-1 text-xs text-stone-500">{detalle}</p>}
    </div>
  );
}

/** Par etiqueta-valor para fichas de detalle. */
export function Dato({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">{etiqueta}</dt>
      <dd className="mt-1 text-sm text-stone-900">{children}</dd>
    </div>
  );
}

// ---------- Estados ----------

const coloresEstado: Record<EstadoPrestamo | EstadoCuota, string> = {
  Pendiente: 'bg-amber-50 text-amber-800 ring-amber-200',
  Aprobado: 'bg-sky-50 text-sky-800 ring-sky-200',
  Rechazado: 'bg-stone-100 text-stone-600 ring-stone-300',
  Desembolsado: 'bg-marca-50 text-marca-800 ring-marca-100',
  Pagado: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
  Parcial: 'bg-sky-50 text-sky-800 ring-sky-200',
  Vencida: 'bg-red-50 text-red-700 ring-red-200',
  Pagada: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
};

/** Acepta "Pendiente" o "PENDIENTE" (los procedimientos almacenados devuelven mayúsculas). */
export function EstadoBadge({ estado }: { estado: string }) {
  const normal = (estado.charAt(0) + estado.slice(1).toLowerCase()) as EstadoPrestamo | EstadoCuota;
  return (
    <span className={cx('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ring-1 ring-inset', coloresEstado[normal] ?? coloresEstado.Rechazado)}>
      {normal}
    </span>
  );
}

export function Insignia({ children, tono = 'neutro' }: { children: ReactNode; tono?: 'neutro' | 'marca' | 'alerta' }) {
  const colores = {
    neutro: 'bg-stone-100 text-stone-700',
    marca: 'bg-marca-50 text-marca-800',
    alerta: 'bg-red-50 text-red-700',
  };
  return <span className={cx('inline-flex rounded-md px-2 py-0.5 text-xs font-medium', colores[tono])}>{children}</span>;
}

// ---------- Mensajes ----------

export function Aviso({ tipo = 'error', children }: { tipo?: 'error' | 'exito' | 'info'; children: ReactNode }) {
  const estilos = {
    error: 'border-red-200 bg-red-50 text-red-800',
    exito: 'border-emerald-200 bg-emerald-50 text-emerald-800',
    info: 'border-sky-200 bg-sky-50 text-sky-800',
  };
  const Icono = tipo === 'exito' ? CheckCircle2 : AlertCircle;
  return (
    <div role={tipo === 'error' ? 'alert' : 'status'} className={cx('flex gap-2.5 rounded-lg border px-4 py-3 text-sm', estilos[tipo])}>
      <Icono className="mt-0.5 size-4 shrink-0" aria-hidden />
      <div>{children}</div>
    </div>
  );
}

export function Cargando({ texto = 'Cargando…' }: { texto?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-12 text-sm text-stone-500">
      <Loader2 className="size-5 animate-spin" aria-hidden /> {texto}
    </div>
  );
}

export function Vacio({ titulo, descripcion, accion }: { titulo: string; descripcion?: string; accion?: ReactNode }) {
  return (
    <div className="rounded-lg border border-dashed border-stone-300 px-6 py-10 text-center">
      <p className="font-medium text-stone-700">{titulo}</p>
      {descripcion && <p className="mt-1 text-sm text-stone-500">{descripcion}</p>}
      {accion && <div className="mt-4">{accion}</div>}
    </div>
  );
}

// ---------- Modal ----------

export function Modal({ abierto, titulo, alCerrar, children, ancho = 'max-w-lg' }: { abierto: boolean; titulo: string; alCerrar: () => void; children: ReactNode; ancho?: string }) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialogo = ref.current;
    if (!dialogo) return;
    if (abierto && !dialogo.open) dialogo.showModal();
    if (!abierto && dialogo.open) dialogo.close();
  }, [abierto]);

  return (
    <dialog
      ref={ref}
      onClose={alCerrar}
      onClick={(e) => e.target === ref.current && alCerrar()}
      className={cx('m-auto w-[calc(100%-2rem)] rounded-xl p-0 shadow-xl backdrop:bg-stone-900/40', ancho)}
    >
      {abierto && (
        <div>
          <header className="flex items-center justify-between border-b border-stone-100 px-5 py-4">
            <h2 className="text-lg font-semibold text-stone-900">{titulo}</h2>
            <button type="button" onClick={alCerrar} className="rounded-md p-1 text-stone-500 hover:bg-stone-100" aria-label="Cerrar">
              <X className="size-5" />
            </button>
          </header>
          <div className="p-5">{children}</div>
        </div>
      )}
    </dialog>
  );
}

// ---------- Tabla ----------

export function Tabla({ children }: { children: ReactNode }) {
  return (
    <div className="-mx-5 overflow-x-auto">
      <table className="min-w-full divide-y divide-stone-200 text-sm">{children}</table>
    </div>
  );
}

export function Th({ children, derecha }: { children?: ReactNode; derecha?: boolean }) {
  return (
    <th scope="col" className={cx('whitespace-nowrap px-5 py-2.5 text-xs font-semibold uppercase tracking-wide text-stone-500', derecha ? 'text-right' : 'text-left')}>
      {children}
    </th>
  );
}

export function Td({ children, derecha, className }: { children?: ReactNode; derecha?: boolean; className?: string }) {
  return <td className={cx('whitespace-nowrap px-5 py-3', derecha && 'tabular text-right', className)}>{children}</td>;
}
