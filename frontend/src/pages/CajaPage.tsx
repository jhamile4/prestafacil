import { useEffect, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CheckCircle2, Printer, Search } from 'lucide-react';
import { clientesApi, pagosApi, prestamosApi } from '../api/endpoints';
import type { MedioPago, PagoRegistrado, Prestamo } from '../api/types';
import { fecha, fechaHora, soles } from '../lib/format';
import { errorCampo, errorGeneral, mensajeError } from '../lib/hooks';
import { Aviso, Boton, Cargando, EncabezadoPagina, Entrada, EstadoBadge, Seleccion, Tarjeta, Vacio } from '../components/ui';

const medios: MedioPago[] = ['Efectivo', 'Yape', 'Plin', 'Transferencia'];

export function CajaPage() {
  const [params, setParams] = useSearchParams();
  const prestamoId = Number(params.get('prestamo')) || null;

  return (
    <>
      <EncabezadoPagina titulo="Caja" descripcion="Busca el préstamo, revisa cuánto debe hoy y registra el pago." />
      <div className="grid gap-6 xl:grid-cols-[360px_1fr]">
        <Buscador seleccionado={prestamoId} alElegir={(id) => setParams({ prestamo: String(id) })} />
        {prestamoId ? <div className="min-w-0"><Cobro key={prestamoId} prestamoId={prestamoId} /></div> : (
          <Tarjeta><Vacio titulo="Elige un préstamo para cobrar." descripcion="Busca por DNI, por nombre o por código (ej. PR-000002)." /></Tarjeta>
        )}
      </div>
    </>
  );
}

function Buscador({ seleccionado, alElegir }: { seleccionado: number | null; alElegir: (id: number) => void }) {
  const [texto, setTexto] = useState('');
  const [termino, setTermino] = useState('');
  const [clienteId, setClienteId] = useState<number | null>(null);

  const clientes = useQuery({
    queryKey: ['clientes', { buscar: termino, caja: true }],
    queryFn: () => clientesApi.buscar({ buscar: termino, tamanoPagina: 8 }),
    enabled: termino.length >= 2,
  });

  // Si la búsqueda devuelve un solo cliente, se elige automáticamente
  useEffect(() => {
    if (clientes.data?.items.length === 1) setClienteId(clientes.data.items[0]!.id);
  }, [clientes.data]);

  const prestamos = useQuery({
    queryKey: ['prestamos', { clienteId, estado: 'Desembolsado' }],
    queryFn: () => prestamosApi.buscar({ clienteId: clienteId!, estado: 'Desembolsado' }),
    enabled: !!clienteId,
  });

  function enviar(e: FormEvent) {
    e.preventDefault();
    setClienteId(null);
    const valor = texto.trim();
    // Por código (PR-000002 o PR2): directo al préstamo
    const codigo = /^PR-?(\d+)$/i.exec(valor);
    if (codigo) {
      setTermino('');
      alElegir(Number(codigo[1]));
      return;
    }
    setTermino(valor);
  }

  return (
    <Tarjeta titulo="Buscar préstamo">
      <form onSubmit={enviar} className="flex gap-2">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-stone-400" aria-hidden />
          <input
            aria-label="DNI, nombre o código del préstamo"
            placeholder="DNI, nombre o PR-000002"
            value={texto}
            onChange={(e) => setTexto(e.target.value)}
            className="w-full rounded-lg border border-stone-300 py-2 pr-3 pl-9 text-sm focus:border-marca-500 focus:ring-2 focus:ring-marca-500/40 focus:outline-none"
          />
        </div>
        <Boton type="submit">Buscar</Boton>
      </form>

      <div className="mt-4 space-y-3">
        {clientes.isFetching && <Cargando />}
        {clientes.data && !clienteId && clientes.data.items.length > 1 && (
          <ul className="divide-y divide-stone-100 rounded-lg border border-stone-200">
            {clientes.data.items.map((c) => (
              <li key={c.id}>
                <button type="button" onClick={() => setClienteId(c.id)} className="w-full px-3 py-2 text-left text-sm hover:bg-stone-50">
                  <span className="font-medium">{c.nombreCompleto}</span> <span className="text-stone-500">· {c.dni}</span>
                </button>
              </li>
            ))}
          </ul>
        )}
        {clientes.data?.items.length === 0 && <p className="text-sm text-stone-500">No se encontraron clientes.</p>}

        {prestamos.data && (
          prestamos.data.items.length === 0 ? (
            <p className="text-sm text-stone-500">El cliente no tiene préstamos vigentes.</p>
          ) : (
            <ul className="space-y-2">
              {prestamos.data.items.map((p: Prestamo) => (
                <li key={p.id}>
                  <button
                    type="button"
                    onClick={() => alElegir(p.id)}
                    className={`w-full rounded-lg border px-3 py-2.5 text-left transition ${seleccionado === p.id ? 'border-marca-500 bg-marca-50' : 'border-stone-200 hover:bg-stone-50'}`}
                  >
                    <p className="text-sm font-semibold text-stone-900">{p.codigo} · {p.producto}</p>
                    <p className="text-xs text-stone-500">{p.cliente} · cuota {soles(p.cuotaMensual)}</p>
                  </button>
                </li>
              ))}
            </ul>
          )
        )}
      </div>
    </Tarjeta>
  );
}

function Cobro({ prestamoId }: { prestamoId: number }) {
  const queryClient = useQueryClient();
  const [monto, setMonto] = useState('');
  const [medio, setMedio] = useState<MedioPago>('Efectivo');
  const [operacion, setOperacion] = useState('');
  const [recibo, setRecibo] = useState<PagoRegistrado | null>(null);

  const prestamo = useQuery({ queryKey: ['prestamos', prestamoId], queryFn: () => prestamosApi.obtener(prestamoId) });
  const vigente = prestamo.data?.prestamo.estado === 'Desembolsado';
  const deuda = useQuery({ queryKey: ['prestamos', prestamoId, 'deuda'], queryFn: () => pagosApi.deuda(prestamoId), enabled: vigente });

  const cobrar = useMutation({
    mutationFn: () => pagosApi.registrar(prestamoId, { monto: Number(monto), medioPago: medio, numeroOperacion: medio === 'Efectivo' ? undefined : operacion }),
    onSuccess: (r) => {
      setRecibo(r);
      setMonto('');
      setOperacion('');
      queryClient.invalidateQueries({ queryKey: ['prestamos'] });
      queryClient.invalidateQueries({ queryKey: ['pagos', prestamoId] });
      queryClient.invalidateQueries({ queryKey: ['reportes'] });
      queryClient.invalidateQueries({ queryKey: ['clientes'] });
    },
  });

  if (prestamo.isPending) return <Tarjeta><Cargando /></Tarjeta>;
  if (prestamo.isError) return <Aviso>{mensajeError(prestamo.error)}</Aviso>;

  const p = prestamo.data.prestamo;
  const encabezado = (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div>
        <Link to={`/prestamos/${p.id}`} className="text-lg font-semibold text-stone-900 hover:text-marca-700">{p.codigo}</Link>
        <p className="text-sm text-stone-500">{p.cliente} · DNI {p.clienteDni} · {p.producto}</p>
      </div>
      <EstadoBadge estado={p.estado} />
    </div>
  );

  if (recibo) {
    return (
      <Tarjeta titulo={encabezado}>
        <div className="flex items-center gap-3 rounded-lg bg-emerald-50 px-4 py-3 text-emerald-800">
          <CheckCircle2 className="size-6" />
          <div>
            <p className="font-semibold">Pago registrado: {soles(recibo.pago.monto)}</p>
            <p className="text-sm">{recibo.pago.medioPago}{recibo.pago.numeroOperacion ? ` · Op. ${recibo.pago.numeroOperacion}` : ''} · {fechaHora(recibo.pago.fechaPago)}</p>
          </div>
        </div>

        <h3 className="mt-5 mb-2 text-sm font-semibold text-stone-900">Cómo se aplicó (cuota más antigua primero: mora → interés → capital)</h3>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-stone-200 text-xs text-stone-500">
              <th className="py-1.5 text-left font-medium">Cuota</th>
              <th className="py-1.5 text-right font-medium">Mora</th>
              <th className="py-1.5 text-right font-medium">Interés</th>
              <th className="py-1.5 text-right font-medium">Capital</th>
              <th className="py-1.5 text-right font-medium">Total</th>
            </tr>
          </thead>
          <tbody className="tabular divide-y divide-stone-100">
            {recibo.pago.detalles.map((d) => (
              <tr key={d.numeroCuota}>
                <td className="py-1.5">N.º {d.numeroCuota}</td>
                <td className="py-1.5 text-right">{soles(d.mora)}</td>
                <td className="py-1.5 text-right">{soles(d.interes)}</td>
                <td className="py-1.5 text-right">{soles(d.capital)}</td>
                <td className="py-1.5 text-right font-semibold">{soles(d.total)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="mt-5 rounded-lg bg-stone-50 p-4 text-sm">
          {recibo.estadoPrestamo === 'Pagado' ? (
            <p className="font-semibold text-emerald-700">🎉 El préstamo quedó cancelado por completo.</p>
          ) : (
            <p>
              Deuda restante: <strong className="tabular">{soles(recibo.deudaRestante.deudaTotal)}</strong>
              {recibo.deudaRestante.cuotasVencidas > 0
                ? ` · aún tiene ${recibo.deudaRestante.cuotasVencidas} cuota(s) vencida(s)`
                : recibo.deudaRestante.proximaCuota && ` · próxima cuota el ${fecha(recibo.deudaRestante.proximaCuota.fechaVencimiento)}`}
            </p>
          )}
        </div>

        <div className="mt-5 flex justify-end gap-2">
          <Boton variante="secundario" icono={<Printer className="size-4" />} onClick={() => window.print()}>Imprimir</Boton>
          <Boton onClick={() => { setRecibo(null); cobrar.reset(); }}>Nuevo cobro</Boton>
        </div>
      </Tarjeta>
    );
  }

  if (!vigente) {
    return <Tarjeta titulo={encabezado}><Aviso tipo="info">Este préstamo no está vigente, así que no recibe pagos.</Aviso></Tarjeta>;
  }

  if (deuda.isPending) return <Tarjeta titulo={encabezado}><Cargando /></Tarjeta>;
  if (deuda.isError) return <Aviso>{mensajeError(deuda.error)}</Aviso>;
  const d = deuda.data;

  const atajos = [
    d.totalParaPonerseAlDia > 0 && { texto: 'Ponerse al día', valor: d.totalParaPonerseAlDia },
    d.proximaCuota && { texto: `Hasta la cuota ${d.proximaCuota.numero}`, valor: d.totalParaPonerseAlDia + d.proximaCuota.pendiente },
    { texto: 'Cancelar todo', valor: d.deudaTotal },
  ].filter(Boolean) as { texto: string; valor: number }[];

  return (
    <Tarjeta titulo={encabezado}>
      <div className="grid gap-3 sm:grid-cols-4">
        <Cifra etiqueta="Cuotas vencidas" valor={String(d.cuotasVencidas)} alerta={d.cuotasVencidas > 0} detalle={d.diasAtrasoMaximo ? `${d.diasAtrasoMaximo} días de atraso` : 'Al día'} />
        <Cifra etiqueta="Mora a hoy" valor={soles(d.moraPendiente)} alerta={d.moraPendiente > 0} />
        <Cifra etiqueta="Para ponerse al día" valor={soles(d.totalParaPonerseAlDia)} alerta={d.totalParaPonerseAlDia > 0} />
        <Cifra etiqueta="Para cancelar" valor={soles(d.deudaTotal)} />
      </div>
      {d.proximaCuota && (
        <p className="mt-3 text-sm text-stone-600">
          Próxima cuota: N.º {d.proximaCuota.numero} de <strong className="tabular">{soles(d.proximaCuota.pendiente)}</strong>, vence el {fecha(d.proximaCuota.fechaVencimiento)}.
        </p>
      )}

      <form onSubmit={(e) => { e.preventDefault(); cobrar.mutate(); }} className="mt-6 space-y-4 border-t border-stone-100 pt-5">
        {errorGeneral(cobrar.error) && <Aviso>{errorGeneral(cobrar.error)}</Aviso>}
        <div className="flex flex-wrap gap-2">
          {atajos.map((a) => (
            <button key={a.texto} type="button" onClick={() => setMonto(a.valor.toFixed(2))} className="rounded-full border border-stone-300 px-3 py-1 text-sm hover:border-marca-500 hover:bg-marca-50">
              {a.texto}: <span className="tabular font-semibold">{soles(a.valor)}</span>
            </button>
          ))}
        </div>
        <div className="grid gap-4 sm:grid-cols-3">
          <Entrada etiqueta="Monto (S/)" type="number" min="0.01" step="0.01" required value={monto} onChange={(e) => setMonto(e.target.value)} error={errorCampo(cobrar.error, 'monto')} />
          <Seleccion etiqueta="Medio de pago" value={medio} onChange={(e) => setMedio(e.target.value as MedioPago)}>
            {medios.map((m) => <option key={m}>{m}</option>)}
          </Seleccion>
          {medio !== 'Efectivo' && (
            <Entrada etiqueta="N.º de operación" required value={operacion} onChange={(e) => setOperacion(e.target.value)} error={errorCampo(cobrar.error, 'numeroOperacion')} />
          )}
        </div>
        <div className="flex justify-end">
          <Boton type="submit" cargando={cobrar.isPending} disabled={!monto}>Registrar pago</Boton>
        </div>
      </form>
    </Tarjeta>
  );
}

function Cifra({ etiqueta, valor, detalle, alerta }: { etiqueta: string; valor: string; detalle?: string; alerta?: boolean }) {
  return (
    <div className={`rounded-lg px-3 py-2.5 ${alerta ? 'bg-red-50' : 'bg-stone-50'}`}>
      <p className="text-xs text-stone-500">{etiqueta}</p>
      <p className={`tabular text-lg font-semibold ${alerta ? 'text-red-700' : 'text-stone-900'}`}>{valor}</p>
      {detalle && <p className="text-xs text-stone-500">{detalle}</p>}
    </div>
  );
}
