import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Banknote, Check, Wallet, X } from 'lucide-react';
import { pagosApi, prestamosApi } from '../api/endpoints';
import { useAuth } from '../auth/AuthContext';
import { fecha, fechaHora, porcentaje, soles } from '../lib/format';
import { mensajeError, useNombresUsuarios } from '../lib/hooks';
import { EvaluacionPanel } from '../components/Evaluacion';
import { PagosLista } from '../components/PagosLista';
import { AreaTexto, Aviso, Boton, Cargando, Dato, EncabezadoPagina, EstadoBadge, Modal, Tabla, Tarjeta, Td, Th } from '../components/ui';

type Accion = 'aprobar' | 'rechazar' | 'desembolsar' | null;

export function PrestamoDetallePage() {
  const id = Number(useParams().id);
  const { puede } = useAuth();
  const nombre = useNombresUsuarios();
  const queryClient = useQueryClient();
  const [accion, setAccion] = useState<Accion>(null);
  const [texto, setTexto] = useState('');

  const detalle = useQuery({ queryKey: ['prestamos', id], queryFn: () => prestamosApi.obtener(id) });
  const estado = detalle.data?.prestamo.estado;
  const tieneCronograma = estado === 'Desembolsado' || estado === 'Pagado';
  const cronograma = useQuery({ queryKey: ['prestamos', id, 'cronograma'], queryFn: () => prestamosApi.cronograma(id), enabled: tieneCronograma });
  const pagos = useQuery({ queryKey: ['pagos', id], queryFn: () => pagosApi.listar(id), enabled: tieneCronograma });

  const ejecutar = useMutation({
    mutationFn: async () => {
      if (accion === 'aprobar') await prestamosApi.aprobar(id, texto || undefined);
      if (accion === 'rechazar') await prestamosApi.rechazar(id, texto);
      if (accion === 'desembolsar') await prestamosApi.desembolsar(id);
    },
    onSuccess: () => {
      cerrar();
      queryClient.invalidateQueries({ queryKey: ['prestamos'] });
      queryClient.invalidateQueries({ queryKey: ['reportes'] });
      queryClient.invalidateQueries({ queryKey: ['clientes'] });
    },
  });

  function cerrar() {
    setAccion(null);
    setTexto('');
    ejecutar.reset();
  }

  if (detalle.isPending) return <Cargando />;
  if (detalle.isError) return <Aviso>{mensajeError(detalle.error)}</Aviso>;

  const { prestamo: p, evaluacion, historial } = detalle.data;
  const c = cronograma.data;
  const avance = c ? c.cuotas.filter((q) => q.estado === 'Pagada').length : 0;

  return (
    <>
      <Link to="/prestamos" className="mb-4 inline-flex items-center gap-1 text-sm text-stone-500 hover:text-stone-800">
        <ArrowLeft className="size-4" /> Préstamos
      </Link>
      <EncabezadoPagina
        titulo={`Préstamo ${p.codigo}`}
        descripcion={<><Link to={`/clientes/${p.clienteId}`} className="font-medium text-marca-700 hover:underline">{p.cliente}</Link> · DNI {p.clienteDni} · <EstadoBadge estado={p.estado} /></>}
        acciones={
          <>
            {p.estado === 'Pendiente' && puede('aprobar') && (
              <>
                <Boton variante="secundario" icono={<X className="size-4" />} onClick={() => setAccion('rechazar')}>Rechazar</Boton>
                <Boton icono={<Check className="size-4" />} onClick={() => setAccion('aprobar')}>Aprobar</Boton>
              </>
            )}
            {p.estado === 'Aprobado' && puede('desembolsar') && (
              <Boton icono={<Banknote className="size-4" />} onClick={() => setAccion('desembolsar')}>Desembolsar</Boton>
            )}
            {p.estado === 'Desembolsado' && puede('cobrar') && (
              <Link to={`/caja?prestamo=${p.id}`} className="inline-flex items-center gap-2 rounded-lg bg-marca-700 px-3.5 py-2 text-sm font-semibold text-white hover:bg-marca-800">
                <Wallet className="size-4" /> Cobrar
              </Link>
            )}
          </>
        }
      />

      {p.estado === 'Rechazado' && <div className="mb-6"><Aviso>Rechazado: {p.motivoRechazo}</Aviso></div>}

      <div className="grid gap-6 xl:grid-cols-3">
        <div className="min-w-0 space-y-6 xl:col-span-2">
          <Tarjeta titulo="Condiciones">
            <dl className="grid grid-cols-2 gap-5 sm:grid-cols-4">
              <Dato etiqueta="Producto">{p.producto}</Dato>
              <Dato etiqueta="Monto"><span className="tabular font-semibold">{soles(p.monto)}</span></Dato>
              <Dato etiqueta="Plazo">{p.plazoMeses} meses</Dato>
              <Dato etiqueta="Cuota mensual"><span className="tabular font-semibold">{soles(p.cuotaMensual)}</span></Dato>
              <Dato etiqueta="TEA">{porcentaje(p.tea)}</Dato>
              <Dato etiqueta="TEM">{porcentaje(detalle.data.tem, 4)}</Dato>
              <Dato etiqueta="Mora diaria">{porcentaje(detalle.data.tasaMoraDiaria)}</Dato>
              <Dato etiqueta="Desembolso">{fecha(p.fechaDesembolso)}</Dato>
            </dl>
          </Tarjeta>

          {evaluacion && (
            <Tarjeta titulo="Evaluación crediticia a la fecha">
              <EvaluacionPanel evaluacion={evaluacion} />
            </Tarjeta>
          )}
        </div>

        <div className="space-y-6">
          <Tarjeta titulo="Historial">
            <ol className="relative space-y-5 border-l border-stone-200 pl-5">
              {historial.map((h, i) => (
                <li key={i}>
                  <span className="absolute -left-1.5 mt-1.5 size-3 rounded-full border-2 border-white bg-marca-500" />
                  <div className="flex items-center gap-2"><EstadoBadge estado={h.estadoNuevo} /></div>
                  <p className="mt-1 text-sm text-stone-700">{h.comentario}</p>
                  <p className="text-xs text-stone-500">{nombre(h.usuarioId)} · {fechaHora(h.fecha)}</p>
                </li>
              ))}
            </ol>
          </Tarjeta>
        </div>
      </div>

      {tieneCronograma && (
        <Tarjeta className="mt-6" titulo="Cronograma de cuotas" acciones={c && <span className="text-sm text-stone-500">{avance} de {c.cuotas.length} pagadas</span>}>
          {cronograma.isPending ? (
            <Cargando />
          ) : cronograma.isError ? (
            <Aviso>{mensajeError(cronograma.error)}</Aviso>
          ) : (
            <>
              <div className="mb-4 grid gap-3 sm:grid-cols-3">
                <Resumen etiqueta="Pagado" valor={soles(cronograma.data.totalPagado)} />
                <Resumen etiqueta="Intereses del préstamo" valor={soles(cronograma.data.totalIntereses)} />
                <Resumen etiqueta="Deuda pendiente" valor={soles(cronograma.data.deudaPendiente)} destacado />
              </div>
              <div className="mb-4 h-2 rounded-full bg-stone-100">
                <div className="h-2 rounded-full bg-marca-500" style={{ width: `${(avance / cronograma.data.cuotas.length) * 100}%` }} />
              </div>
              <Tabla>
                <thead>
                  <tr>
                    <Th>N.º</Th>
                    <Th>Vence</Th>
                    <Th derecha>Capital</Th>
                    <Th derecha>Interés</Th>
                    <Th derecha>Cuota</Th>
                    <Th derecha>Mora</Th>
                    <Th derecha>Pagado</Th>
                    <Th derecha>Pendiente</Th>
                    <Th>Estado</Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-stone-100">
                  {cronograma.data.cuotas.map((q) => (
                    <tr key={q.numero} className={q.estado === 'Vencida' ? 'bg-red-50/50' : ''}>
                      <Td>{q.numero}</Td>
                      <Td>{fecha(q.fechaVencimiento)}</Td>
                      <Td derecha>{soles(q.capital)}</Td>
                      <Td derecha className="text-stone-500">{soles(q.interes)}</Td>
                      <Td derecha>{soles(q.montoCuota)}</Td>
                      <Td derecha className={q.mora > 0 ? 'text-red-600' : 'text-stone-400'}>{soles(q.mora)}</Td>
                      <Td derecha>{soles(q.totalPagado)}</Td>
                      <Td derecha className="font-semibold">{soles(q.pendiente)}</Td>
                      <Td><EstadoBadge estado={q.estado} /></Td>
                    </tr>
                  ))}
                </tbody>
              </Tabla>
            </>
          )}
        </Tarjeta>
      )}

      {tieneCronograma && (
        <Tarjeta titulo="Pagos" className="mt-6">
          {pagos.isPending ? <Cargando /> : pagos.isError ? <Aviso>{mensajeError(pagos.error)}</Aviso> : (
            <PagosLista prestamoId={id} pagos={pagos.data} puedeAnular={puede('anularPagos')} />
          )}
        </Tarjeta>
      )}

      <Modal abierto={accion !== null} titulo={accion === 'aprobar' ? 'Aprobar solicitud' : accion === 'rechazar' ? 'Rechazar solicitud' : 'Desembolsar préstamo'} alCerrar={cerrar}>
        <form onSubmit={(e) => { e.preventDefault(); ejecutar.mutate(); }} className="space-y-4">
          {accion === 'aprobar' && (
            <>
              <p className="text-sm text-stone-600">La evaluación se vuelve a revisar al aprobar. Quien registró la solicitud no puede aprobarla.</p>
              <AreaTexto etiqueta="Comentario (opcional)" value={texto} onChange={(e) => setTexto(e.target.value)} />
            </>
          )}
          {accion === 'rechazar' && (
            <AreaTexto etiqueta="Motivo del rechazo" required value={texto} onChange={(e) => setTexto(e.target.value)} placeholder="Ej.: no sustenta ingresos" />
          )}
          {accion === 'desembolsar' && (
            <p className="text-sm text-stone-600">
              Se entregarán <strong>{soles(p.monto)}</strong> a {p.cliente} y se generará el cronograma de {p.plazoMeses} cuotas de {soles(p.cuotaMensual)}, con la primera dentro de un mes.
            </p>
          )}
          {ejecutar.isError && <Aviso>{mensajeError(ejecutar.error)}</Aviso>}
          <div className="flex justify-end gap-2">
            <Boton variante="secundario" onClick={cerrar}>Cancelar</Boton>
            <Boton type="submit" variante={accion === 'rechazar' ? 'peligro' : 'primario'} cargando={ejecutar.isPending} disabled={accion === 'rechazar' && !texto.trim()}>
              Confirmar
            </Boton>
          </div>
        </form>
      </Modal>
    </>
  );
}

function Resumen({ etiqueta, valor, destacado }: { etiqueta: string; valor: string; destacado?: boolean }) {
  return (
    <div className="rounded-lg bg-stone-50 px-3 py-2">
      <p className="text-xs text-stone-500">{etiqueta}</p>
      <p className={`tabular text-lg font-semibold ${destacado ? 'text-marca-700' : 'text-stone-900'}`}>{valor}</p>
    </div>
  );
}
