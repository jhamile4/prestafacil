import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Ban, ChevronDown } from 'lucide-react';
import { pagosApi } from '../api/endpoints';
import type { Pago } from '../api/types';
import { fechaHora, soles } from '../lib/format';
import { mensajeError, useNombresUsuarios } from '../lib/hooks';
import { AreaTexto, Aviso, Boton, Insignia, Modal, Vacio } from './ui';

/** Pagos del préstamo con su distribución; el ADMIN puede anular el último válido (RN-11). */
export function PagosLista({ prestamoId, pagos, puedeAnular }: { prestamoId: number; pagos: Pago[]; puedeAnular: boolean }) {
  const nombre = useNombresUsuarios();
  const queryClient = useQueryClient();
  const [abierto, setAbierto] = useState<number | null>(null);
  const [anulando, setAnulando] = useState<Pago | null>(null);
  const [motivo, setMotivo] = useState('');

  const ultimoValido = pagos.find((p) => !p.anulado); // la API los devuelve del más reciente al más antiguo

  const anular = useMutation({
    mutationFn: () => pagosApi.anular(prestamoId, anulando!.id, motivo),
    onSuccess: () => {
      setAnulando(null);
      setMotivo('');
      queryClient.invalidateQueries({ queryKey: ['prestamos'] });
      queryClient.invalidateQueries({ queryKey: ['pagos', prestamoId] });
      queryClient.invalidateQueries({ queryKey: ['reportes'] });
    },
  });

  if (pagos.length === 0) return <Vacio titulo="Aún no hay pagos." />;

  return (
    <>
      <ul className="divide-y divide-stone-100">
        {pagos.map((p) => (
          <li key={p.id} className="py-3">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <button type="button" onClick={() => setAbierto(abierto === p.id ? null : p.id)} className="flex items-center gap-2 text-left" aria-expanded={abierto === p.id}>
                <ChevronDown className={`size-4 text-stone-400 transition ${abierto === p.id ? 'rotate-180' : ''}`} />
                <div>
                  <p className={`tabular font-semibold ${p.anulado ? 'text-stone-400 line-through' : 'text-stone-900'}`}>{soles(p.monto)}</p>
                  <p className="text-xs text-stone-500">
                    {fechaHora(p.fechaPago)} · {p.medioPago}{p.numeroOperacion ? ` (${p.numeroOperacion})` : ''} · {nombre(p.cajeroId)}
                  </p>
                </div>
              </button>
              <div className="flex items-center gap-2">
                {p.anulado && <Insignia tono="alerta">Anulado</Insignia>}
                {puedeAnular && p.id === ultimoValido?.id && (
                  <Boton variante="secundario" icono={<Ban className="size-4" />} onClick={() => setAnulando(p)}>Anular</Boton>
                )}
              </div>
            </div>
            {abierto === p.id && (
              <div className="mt-3 ml-6 rounded-lg bg-stone-50 p-3 text-sm">
                {p.anulado && <p className="mb-2 text-red-700">Motivo de anulación: {p.motivoAnulacion}</p>}
                <table className="w-full">
                  <thead>
                    <tr className="text-xs text-stone-500">
                      <th className="text-left font-medium">Cuota</th>
                      <th className="text-right font-medium">Mora</th>
                      <th className="text-right font-medium">Interés</th>
                      <th className="text-right font-medium">Capital</th>
                      <th className="text-right font-medium">Total</th>
                    </tr>
                  </thead>
                  <tbody className="tabular">
                    {p.detalles.map((d) => (
                      <tr key={d.numeroCuota}>
                        <td>N.º {d.numeroCuota}</td>
                        <td className="text-right">{soles(d.mora)}</td>
                        <td className="text-right">{soles(d.interes)}</td>
                        <td className="text-right">{soles(d.capital)}</td>
                        <td className="text-right font-semibold">{soles(d.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </li>
        ))}
      </ul>

      <Modal abierto={!!anulando} titulo="Anular pago" alCerrar={() => setAnulando(null)}>
        <form onSubmit={(e) => { e.preventDefault(); anular.mutate(); }} className="space-y-4">
          <p className="text-sm text-stone-600">
            Se anulará el pago de <strong>{anulando && soles(anulando.monto)}</strong> y sus abonos volverán a quedar pendientes. El pago no se borra: queda registrado como anulado.
          </p>
          {anular.isError && <Aviso>{mensajeError(anular.error)}</Aviso>}
          <AreaTexto etiqueta="Motivo" required value={motivo} onChange={(e) => setMotivo(e.target.value)} placeholder="Ej.: la transferencia fue rechazada por el banco" />
          <div className="flex justify-end gap-2">
            <Boton variante="secundario" onClick={() => setAnulando(null)}>Cancelar</Boton>
            <Boton type="submit" variante="peligro" cargando={anular.isPending} disabled={!motivo.trim()}>Anular pago</Boton>
          </div>
        </form>
      </Modal>
    </>
  );
}
