import { Link, useNavigate, useSearchParams } from 'react-router';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { prestamosApi } from '../api/endpoints';
import type { EstadoPrestamo } from '../api/types';
import { fecha, porcentaje, soles } from '../lib/format';
import { mensajeError } from '../lib/hooks';
import { Aviso, Boton, Cargando, EncabezadoPagina, EstadoBadge, Tabla, Tarjeta, Td, Th, Vacio } from '../components/ui';

const pestanas: { valor: EstadoPrestamo | ''; texto: string }[] = [
  { valor: '', texto: 'Todos' },
  { valor: 'Pendiente', texto: 'Pendientes' },
  { valor: 'Aprobado', texto: 'Aprobados' },
  { valor: 'Desembolsado', texto: 'Vigentes' },
  { valor: 'Pagado', texto: 'Pagados' },
  { valor: 'Rechazado', texto: 'Rechazados' },
];

export function PrestamosPage() {
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const estado = (params.get('estado') ?? '') as EstadoPrestamo | '';
  const pagina = Number(params.get('pagina') ?? 1);

  const { data, isPending, isError, error, isFetching } = useQuery({
    queryKey: ['prestamos', { estado, pagina }],
    queryFn: () => prestamosApi.buscar({ estado: estado || undefined, pagina, tamanoPagina: 15 }),
    placeholderData: keepPreviousData,
  });

  return (
    <>
      <EncabezadoPagina titulo="Préstamos" descripcion="Solicitudes y préstamos de todos los clientes, del más reciente al más antiguo." />

      <div className="mb-4 flex flex-wrap gap-1 rounded-xl bg-stone-100 p-1" role="tablist" aria-label="Filtrar por estado">
        {pestanas.map((p) => (
          <button
            key={p.valor}
            role="tab"
            aria-selected={estado === p.valor}
            onClick={() => setParams(p.valor ? { estado: p.valor } : {})}
            className={`rounded-lg px-3.5 py-1.5 text-sm font-medium transition ${estado === p.valor ? 'bg-white text-stone-900 shadow-xs' : 'text-stone-600 hover:text-stone-900'}`}
          >
            {p.texto}
          </button>
        ))}
      </div>

      <Tarjeta>
        {isPending ? (
          <Cargando />
        ) : isError ? (
          <Aviso>{mensajeError(error)}</Aviso>
        ) : data.items.length === 0 ? (
          <Vacio titulo="No hay préstamos en este estado." />
        ) : (
          <div className={isFetching ? 'opacity-60' : ''}>
            <Tabla>
              <thead>
                <tr>
                  <Th>Código</Th>
                  <Th>Cliente</Th>
                  <Th>Producto</Th>
                  <Th derecha>Monto</Th>
                  <Th derecha>Cuota</Th>
                  <Th>Solicitud</Th>
                  <Th>Estado</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {data.items.map((p) => (
                  <tr key={p.id} className="cursor-pointer hover:bg-stone-50" onClick={() => navigate(`/prestamos/${p.id}`)}>
                    <Td><Link to={`/prestamos/${p.id}`} onClick={(e) => e.stopPropagation()} className="font-semibold text-marca-700 hover:underline">{p.codigo}</Link></Td>
                    <Td>{p.cliente}<p className="text-xs text-stone-500">DNI {p.clienteDni}</p></Td>
                    <Td>{p.producto}<p className="text-xs text-stone-500">TEA {porcentaje(p.tea)} · {p.plazoMeses} m</p></Td>
                    <Td derecha>{soles(p.monto)}</Td>
                    <Td derecha>{soles(p.cuotaMensual)}</Td>
                    <Td>{fecha(p.fechaSolicitud)}</Td>
                    <Td><EstadoBadge estado={p.estado} /></Td>
                  </tr>
                ))}
              </tbody>
            </Tabla>
            <div className="mt-4 flex items-center justify-between text-sm text-stone-600">
              <span>{data.total} préstamo{data.total === 1 ? '' : 's'}</span>
              <div className="flex items-center gap-2">
                <Boton variante="secundario" disabled={pagina <= 1} onClick={() => setParams((q) => { q.set('pagina', String(pagina - 1)); return q; })}>Anterior</Boton>
                <span>Página {data.pagina} de {Math.max(1, data.totalPaginas)}</span>
                <Boton variante="secundario" disabled={pagina >= data.totalPaginas} onClick={() => setParams((q) => { q.set('pagina', String(pagina + 1)); return q; })}>Siguiente</Boton>
              </div>
            </div>
          </div>
        )}
      </Tarjeta>
    </>
  );
}
