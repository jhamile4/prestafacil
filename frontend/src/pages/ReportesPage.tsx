import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { RefreshCw } from 'lucide-react';
import { reportesApi } from '../api/endpoints';
import { useAuth } from '../auth/AuthContext';
import { hoyIso, soles } from '../lib/format';
import { mensajeError } from '../lib/hooks';
import { Aviso, Boton, Cargando, EncabezadoPagina, EstadoBadge, Entrada, Tabla, Tarjeta, Td, Th, Vacio } from '../components/ui';

const COLORES_MEDIO: Record<string, string> = {
  EFECTIVO: '#0f766e',
  YAPE: '#7c3aed',
  PLIN: '#0ea5e9',
  TRANSFERENCIA: '#f59e0b',
};

export function ReportesPage() {
  const { puede } = useAuth();
  return (
    <>
      <EncabezadoPagina titulo="Reportes" descripcion="Datos calculados por los procedimientos almacenados de SQL Server." acciones={puede('verReportes') && <ProcesarMora />} />
      <div className="space-y-6">
        {puede('verReportes') && <Cartera />}
        <Morosos />
        {puede('verReportes') && <Cobranza />}
      </div>
    </>
  );
}

function ProcesarMora() {
  const queryClient = useQueryClient();
  const procesar = useMutation({
    mutationFn: reportesApi.procesarMora,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['reportes'] }),
  });
  return (
    <div className="flex items-center gap-3">
      {procesar.data && <span className="text-sm text-stone-600">{procesar.data.cuotasActualizadas} cuota(s) actualizadas</span>}
      {procesar.isError && <span className="text-sm text-red-600">{mensajeError(procesar.error)}</span>}
      <Boton variante="secundario" icono={<RefreshCw className="size-4" />} cargando={procesar.isPending} onClick={() => procesar.mutate()} title="Se ejecuta solo cada 24 horas; aquí puedes forzarlo">
        Recalcular mora
      </Boton>
    </div>
  );
}

function Cartera() {
  const { data, isPending, isError, error } = useQuery({ queryKey: ['reportes', 'cartera'], queryFn: reportesApi.cartera });

  return (
    <Tarjeta titulo="Cartera por estado">
      {isPending ? <Cargando /> : isError ? <Aviso>{mensajeError(error)}</Aviso> : (
        <div className="grid gap-6 lg:grid-cols-2">
          <div className="h-64" aria-label="Gráfico de monto por estado">
            <ResponsiveContainer>
              <BarChart data={data.map((d) => ({ ...d, estado: d.estado.charAt(0) + d.estado.slice(1).toLowerCase() }))} margin={{ left: 12 }}>
                <CartesianGrid vertical={false} stroke="#e7e5e4" />
                <XAxis dataKey="estado" tickLine={false} axisLine={false} fontSize={12} />
                <YAxis tickLine={false} axisLine={false} fontSize={12} tickFormatter={(v: number) => `S/ ${(v / 1000).toFixed(0)}k`} />
                <Tooltip formatter={(v) => soles(Number(v))} cursor={{ fill: '#f5f5f4' }} />
                <Bar dataKey="montoTotal" name="Monto" fill="#0f766e" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
          <Tabla>
            <thead>
              <tr>
                <Th>Estado</Th>
                <Th derecha>Préstamos</Th>
                <Th derecha>Monto</Th>
                <Th derecha>Saldo de capital</Th>
              </tr>
            </thead>
            <tbody className="divide-y divide-stone-100">
              {data.map((d) => (
                <tr key={d.estado}>
                  <Td><EstadoBadge estado={d.estado} /></Td>
                  <Td derecha>{d.cantidadPrestamos}</Td>
                  <Td derecha>{soles(d.montoTotal)}</Td>
                  <Td derecha className="font-semibold">{soles(d.saldoCapitalPendiente)}</Td>
                </tr>
              ))}
            </tbody>
          </Tabla>
        </div>
      )}
    </Tarjeta>
  );
}

function Morosos() {
  const [dias, setDias] = useState(1);
  const { data, isPending, isError, error } = useQuery({ queryKey: ['reportes', 'morosos', dias], queryFn: () => reportesApi.morosos(dias) });

  return (
    <Tarjeta
      titulo="Morosos"
      acciones={
        <label className="flex items-center gap-2 text-sm text-stone-600">
          Con al menos
          <select value={dias} onChange={(e) => setDias(Number(e.target.value))} className="rounded-md border border-stone-300 px-2 py-1 text-sm">
            {[1, 8, 30, 60, 90].map((d) => <option key={d} value={d}>{d}</option>)}
          </select>
          días de atraso
        </label>
      }
    >
      {isPending ? <Cargando /> : isError ? <Aviso>{mensajeError(error)}</Aviso> : data.length === 0 ? <Vacio titulo="No hay clientes con ese atraso." /> : (
        <Tabla>
          <thead>
            <tr>
              <Th>Cliente</Th>
              <Th>Préstamo</Th>
              <Th>Asesor</Th>
              <Th derecha>Cuotas</Th>
              <Th derecha>Días</Th>
              <Th derecha>Vencido</Th>
              <Th derecha>Mora</Th>
              <Th derecha>Total</Th>
            </tr>
          </thead>
          <tbody className="divide-y divide-stone-100">
            {data.map((m) => (
              <tr key={m.prestamo}>
                <Td>{m.cliente}<p className="text-xs text-stone-500">DNI {m.dni} · {m.telefono ?? 'sin teléfono'}</p></Td>
                <Td>{m.prestamo}</Td>
                <Td>{m.asesor}</Td>
                <Td derecha>{m.cuotasVencidas}</Td>
                <Td derecha><span className="font-semibold text-red-600">{m.diasAtraso}</span></Td>
                <Td derecha>{soles(m.deudaVencida)}</Td>
                <Td derecha className="text-red-600">{soles(m.moraPendiente)}</Td>
                <Td derecha className="font-semibold">{soles(m.totalAPagar)}</Td>
              </tr>
            ))}
          </tbody>
        </Tabla>
      )}
    </Tarjeta>
  );
}

function Cobranza() {
  const hoy = hoyIso();
  const [desde, setDesde] = useState(`${hoy.slice(0, 8)}01`);
  const [hasta, setHasta] = useState(hoy);
  const { data, isPending, isError, error } = useQuery({ queryKey: ['reportes', 'cobranza', desde, hasta], queryFn: () => reportesApi.cobranza(desde, hasta), enabled: !!desde && !!hasta });

  const porMedio = Object.values(
    (data ?? []).reduce<Record<string, { medio: string; total: number }>>((acc, c) => {
      acc[c.medioPago] ??= { medio: c.medioPago, total: 0 };
      acc[c.medioPago]!.total += c.totalCobrado;
      return acc;
    }, {}),
  );
  const total = porMedio.reduce((s, m) => s + m.total, 0);

  return (
    <Tarjeta
      titulo="Cobranza"
      acciones={
        <div className="flex items-end gap-2">
          <Entrada etiqueta="Desde" type="date" value={desde} max={hasta} onChange={(e) => setDesde(e.target.value)} />
          <Entrada etiqueta="Hasta" type="date" value={hasta} min={desde} onChange={(e) => setHasta(e.target.value)} />
        </div>
      }
    >
      {isPending ? <Cargando /> : isError ? <Aviso>{mensajeError(error)}</Aviso> : data.length === 0 ? <Vacio titulo="No hubo cobros en ese rango." /> : (
        <div className="grid gap-6 lg:grid-cols-[260px_1fr]">
          <div>
            <div className="h-52" aria-label="Gráfico de cobranza por medio de pago">
              <ResponsiveContainer>
                <PieChart>
                  <Pie data={porMedio} dataKey="total" nameKey="medio" innerRadius={55} outerRadius={85} paddingAngle={porMedio.length > 1 ? 2 : 0}>
                    {porMedio.map((m) => <Cell key={m.medio} fill={COLORES_MEDIO[m.medio] ?? '#a8a29e'} />)}
                  </Pie>
                  <Tooltip formatter={(v) => soles(Number(v))} />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <ul className="mt-2 space-y-1 text-sm">
              {porMedio.map((m) => (
                <li key={m.medio} className="flex items-center justify-between">
                  <span className="flex items-center gap-2"><span className="size-2.5 rounded-full" style={{ background: COLORES_MEDIO[m.medio] ?? '#a8a29e' }} />{m.medio.charAt(0) + m.medio.slice(1).toLowerCase()}</span>
                  <span className="tabular font-medium">{soles(m.total)}</span>
                </li>
              ))}
              <li className="flex justify-between border-t border-stone-200 pt-1 font-semibold"><span>Total</span><span className="tabular">{soles(total)}</span></li>
            </ul>
          </div>
          <Tabla>
            <thead>
              <tr>
                <Th>Cajero</Th>
                <Th>Medio</Th>
                <Th derecha>Pagos</Th>
                <Th derecha>Capital</Th>
                <Th derecha>Interés</Th>
                <Th derecha>Mora</Th>
                <Th derecha>Total</Th>
              </tr>
            </thead>
            <tbody className="divide-y divide-stone-100">
              {data.map((c) => (
                <tr key={`${c.cajero}-${c.medioPago}`}>
                  <Td>{c.cajero}</Td>
                  <Td>{c.medioPago.charAt(0) + c.medioPago.slice(1).toLowerCase()}</Td>
                  <Td derecha>{c.cantidadPagos}</Td>
                  <Td derecha>{soles(c.capital)}</Td>
                  <Td derecha>{soles(c.interes)}</Td>
                  <Td derecha>{soles(c.mora)}</Td>
                  <Td derecha className="font-semibold">{soles(c.totalCobrado)}</Td>
                </tr>
              ))}
            </tbody>
          </Tabla>
        </div>
      )}
    </Tarjeta>
  );
}
