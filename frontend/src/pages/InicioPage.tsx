import { Link } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { ArrowRight, Calculator, UserPlus, Wallet } from 'lucide-react';
import { useAuth } from '../auth/AuthContext';
import { prestamosApi, reportesApi } from '../api/endpoints';
import type { EstadoPrestamo } from '../api/types';
import { fecha, soles } from '../lib/format';
import { mensajeError } from '../lib/hooks';
import { Aviso, Cargando, EncabezadoPagina, EstadoBadge, Indicador, Tabla, Tarjeta, Td, Th, Vacio } from '../components/ui';

export function InicioPage() {
  const { usuario, puede } = useAuth();
  const nombre = usuario?.nombreCompleto.split(' ')[0];

  return (
    <>
      <EncabezadoPagina
        titulo={`Hola, ${nombre}`}
        descripcion="Esto es lo que necesita tu atención hoy."
        acciones={
          <>
            {puede('editarClientes') && (
              <Link to="/clientes?nuevo=1" className="inline-flex items-center gap-2 rounded-lg bg-white px-3.5 py-2 text-sm font-semibold text-stone-700 ring-1 ring-stone-300 hover:bg-stone-50">
                <UserPlus className="size-4" /> Nuevo cliente
              </Link>
            )}
            {puede('solicitar') && (
              <Link to="/simulador" className="inline-flex items-center gap-2 rounded-lg bg-marca-700 px-3.5 py-2 text-sm font-semibold text-white hover:bg-marca-800">
                <Calculator className="size-4" /> Simular préstamo
              </Link>
            )}
            {puede('cobrar') && (
              <Link to="/caja" className="inline-flex items-center gap-2 rounded-lg bg-marca-700 px-3.5 py-2 text-sm font-semibold text-white hover:bg-marca-800">
                <Wallet className="size-4" /> Ir a caja
              </Link>
            )}
          </>
        }
      />

      {puede('verReportes') && <ResumenCartera />}

      <div className="mt-6 grid gap-6 xl:grid-cols-2">
        {puede('aprobar') || puede('solicitar') ? (
          <ListaPrestamos estado="Pendiente" titulo="Solicitudes por aprobar" vacio="No hay solicitudes pendientes." />
        ) : null}
        {puede('desembolsar') && (
          <ListaPrestamos estado="Aprobado" titulo="Aprobados por desembolsar" vacio="No hay préstamos esperando desembolso." />
        )}
        {puede('verMorosos') && <Morosos />}
        {!puede('verMorosos') && <ListaPrestamos estado="Desembolsado" titulo="Préstamos vigentes" vacio="No hay préstamos vigentes." />}
      </div>
    </>
  );
}

function ResumenCartera() {
  const cartera = useQuery({ queryKey: ['reportes', 'cartera'], queryFn: reportesApi.cartera });
  const morosos = useQuery({ queryKey: ['reportes', 'morosos', 1], queryFn: () => reportesApi.morosos(1) });

  if (cartera.isPending) return <Cargando />;
  if (cartera.isError) return <Aviso>{mensajeError(cartera.error)}</Aviso>;

  const fila = (estado: string) => cartera.data.find((c) => c.estado === estado);
  const vigentes = fila('DESEMBOLSADO');
  const totalVencido = morosos.data?.reduce((s, m) => s + m.totalAPagar, 0) ?? 0;

  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <Indicador etiqueta="Préstamos vigentes" valor={vigentes?.cantidadPrestamos ?? 0} detalle={`${soles(vigentes?.montoTotal ?? 0)} desembolsados`} />
      <Indicador etiqueta="Saldo de capital por cobrar" valor={soles(vigentes?.saldoCapitalPendiente ?? 0)} tono="exito" />
      <Indicador etiqueta="Préstamos en mora" valor={morosos.data?.length ?? '—'} detalle={`${soles(totalVencido)} vencido con mora`} tono={morosos.data?.length ? 'alerta' : 'neutro'} />
      <Indicador etiqueta="Solicitudes pendientes" valor={fila('PENDIENTE')?.cantidadPrestamos ?? 0} detalle={`${fila('APROBADO')?.cantidadPrestamos ?? 0} aprobadas por desembolsar`} />
    </div>
  );
}

function ListaPrestamos({ estado, titulo, vacio }: { estado: EstadoPrestamo; titulo: string; vacio: string }) {
  const { data, isPending, isError, error } = useQuery({
    queryKey: ['prestamos', { estado, pagina: 1, tamanoPagina: 6 }],
    queryFn: () => prestamosApi.buscar({ estado, tamanoPagina: 6 }),
  });

  return (
    <Tarjeta
      titulo={titulo}
      acciones={
        <Link to={`/prestamos?estado=${estado}`} className="inline-flex items-center gap-1 text-sm font-medium text-marca-700 hover:underline">
          Ver todos <ArrowRight className="size-4" />
        </Link>
      }
    >
      {isPending ? (
        <Cargando />
      ) : isError ? (
        <Aviso>{mensajeError(error)}</Aviso>
      ) : data.items.length === 0 ? (
        <Vacio titulo={vacio} />
      ) : (
        <Tabla>
          <thead>
            <tr>
              <Th>Código</Th>
              <Th>Cliente</Th>
              <Th derecha>Monto</Th>
              <Th>Estado</Th>
            </tr>
          </thead>
          <tbody className="divide-y divide-stone-100">
            {data.items.map((p) => (
              <tr key={p.id} className="hover:bg-stone-50">
                <Td>
                  <Link to={`/prestamos/${p.id}`} className="font-semibold text-marca-700 hover:underline">{p.codigo}</Link>
                  <p className="text-xs text-stone-500">{fecha(p.fechaSolicitud)}</p>
                </Td>
                <Td>{p.cliente}</Td>
                <Td derecha>{soles(p.monto)}</Td>
                <Td><EstadoBadge estado={p.estado} /></Td>
              </tr>
            ))}
          </tbody>
        </Tabla>
      )}
    </Tarjeta>
  );
}

function Morosos() {
  const { data, isPending, isError, error } = useQuery({ queryKey: ['reportes', 'morosos', 1], queryFn: () => reportesApi.morosos(1) });

  return (
    <Tarjeta
      titulo="Clientes en mora"
      acciones={<Link to="/reportes" className="inline-flex items-center gap-1 text-sm font-medium text-marca-700 hover:underline">Ver reporte <ArrowRight className="size-4" /></Link>}
    >
      {isPending ? (
        <Cargando />
      ) : isError ? (
        <Aviso>{mensajeError(error)}</Aviso>
      ) : data.length === 0 ? (
        <Vacio titulo="No hay clientes en mora." />
      ) : (
        <Tabla>
          <thead>
            <tr>
              <Th>Cliente</Th>
              <Th>Préstamo</Th>
              <Th derecha>Días</Th>
              <Th derecha>Por cobrar</Th>
            </tr>
          </thead>
          <tbody className="divide-y divide-stone-100">
            {data.slice(0, 6).map((m) => (
              <tr key={m.prestamo}>
                <Td>
                  <p className="font-medium">{m.cliente}</p>
                  <p className="text-xs text-stone-500">DNI {m.dni} · {m.telefono ?? 'sin teléfono'}</p>
                </Td>
                <Td>{m.prestamo}</Td>
                <Td derecha><span className="font-semibold text-red-600">{m.diasAtraso}</span></Td>
                <Td derecha>{soles(m.totalAPagar)}</Td>
              </tr>
            ))}
          </tbody>
        </Tabla>
      )}
    </Tarjeta>
  );
}
