import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Search, UserPlus } from 'lucide-react';
import { clientesApi } from '../api/endpoints';
import { useAuth } from '../auth/AuthContext';
import { soles } from '../lib/format';
import { mensajeError } from '../lib/hooks';
import { ClienteForm } from '../components/ClienteForm';
import { Aviso, Boton, Cargando, EncabezadoPagina, Insignia, Modal, Tabla, Tarjeta, Td, Th, Vacio } from '../components/ui';

const TAMANO = 10;

export function ClientesPage() {
  const { puede } = useAuth();
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const [texto, setTexto] = useState(params.get('buscar') ?? '');
  const buscar = params.get('buscar') ?? '';
  const pagina = Number(params.get('pagina') ?? 1);
  const soloActivos = params.get('inactivos') !== '1';
  const nuevoAbierto = params.get('nuevo') === '1';

  // Espera a que el usuario deje de escribir antes de consultar la API
  useEffect(() => {
    const t = setTimeout(() => {
      if (texto !== buscar) setParams((p) => { p.set('buscar', texto); p.delete('pagina'); return p; }, { replace: true });
    }, 350);
    return () => clearTimeout(t);
  }, [texto, buscar, setParams]);

  const { data, isPending, isError, error, isFetching } = useQuery({
    queryKey: ['clientes', { buscar, pagina, soloActivos }],
    queryFn: () => clientesApi.buscar({ buscar, pagina, tamanoPagina: TAMANO, soloActivos }),
    placeholderData: keepPreviousData,
  });

  const irAPagina = (n: number) => setParams((p) => { p.set('pagina', String(n)); return p; });
  const cerrarNuevo = () => setParams((p) => { p.delete('nuevo'); return p; });

  return (
    <>
      <EncabezadoPagina
        titulo="Clientes"
        descripcion="Busca por DNI o por nombre; no importan las tildes ni las mayúsculas."
        acciones={puede('editarClientes') && (
          <Boton icono={<UserPlus className="size-4" />} onClick={() => setParams((p) => { p.set('nuevo', '1'); return p; })}>
            Nuevo cliente
          </Boton>
        )}
      />

      <Tarjeta>
        <div className="mb-4 flex flex-wrap items-center gap-4">
          <div className="relative min-w-64 flex-1">
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-stone-400" aria-hidden />
            <input
              type="search"
              aria-label="Buscar cliente"
              placeholder="DNI, nombres o apellidos…"
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
              className="w-full rounded-lg border border-stone-300 py-2 pr-3 pl-9 text-sm focus:border-marca-500 focus:ring-2 focus:ring-marca-500/40 focus:outline-none"
            />
          </div>
          <label className="flex items-center gap-2 text-sm text-stone-600">
            <input type="checkbox" checked={!soloActivos} onChange={(e) => setParams((p) => { if (e.target.checked) p.set('inactivos', '1'); else p.delete('inactivos'); p.delete('pagina'); return p; })} className="size-4 rounded border-stone-300 accent-marca-700" />
            Incluir desactivados
          </label>
        </div>

        {isPending ? (
          <Cargando />
        ) : isError ? (
          <Aviso>{mensajeError(error)}</Aviso>
        ) : data.items.length === 0 ? (
          <Vacio titulo="No se encontraron clientes." descripcion={buscar ? `No hay coincidencias para “${buscar}”.` : undefined} />
        ) : (
          <div className={isFetching ? 'opacity-60 transition-opacity' : ''}>
            <Tabla>
              <thead>
                <tr>
                  <Th>Cliente</Th>
                  <Th>DNI</Th>
                  <Th>Contacto</Th>
                  <Th derecha>Ingreso neto</Th>
                  <Th>Estado</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {data.items.map((c) => (
                  <tr key={c.id} className="cursor-pointer hover:bg-stone-50" onClick={() => navigate(`/clientes/${c.id}`)}>
                    <Td>
                      <Link to={`/clientes/${c.id}`} className="font-semibold text-stone-900 hover:text-marca-700" onClick={(e) => e.stopPropagation()}>
                        {c.nombreCompleto}
                      </Link>
                      <p className="text-xs text-stone-500">{c.edad} años</p>
                    </Td>
                    <Td className="tabular">{c.dni}</Td>
                    <Td className="text-stone-600">{c.telefono ?? '—'}<p className="text-xs text-stone-500">{c.email ?? ''}</p></Td>
                    <Td derecha>{soles(c.ingresoNeto)}</Td>
                    <Td>{c.activo ? <Insignia tono="marca">Activo</Insignia> : <Insignia>Desactivado</Insignia>}</Td>
                  </tr>
                ))}
              </tbody>
            </Tabla>
            <div className="mt-4 flex items-center justify-between text-sm text-stone-600">
              <span>{data.total} cliente{data.total === 1 ? '' : 's'}</span>
              <div className="flex items-center gap-2">
                <Boton variante="secundario" disabled={pagina <= 1} onClick={() => irAPagina(pagina - 1)}>Anterior</Boton>
                <span>Página {data.pagina} de {Math.max(1, data.totalPaginas)}</span>
                <Boton variante="secundario" disabled={pagina >= data.totalPaginas} onClick={() => irAPagina(pagina + 1)}>Siguiente</Boton>
              </div>
            </div>
          </div>
        )}
      </Tarjeta>

      <Modal abierto={nuevoAbierto && puede('editarClientes')} titulo="Nuevo cliente" alCerrar={cerrarNuevo} ancho="max-w-2xl">
        <ClienteForm alCancelar={cerrarNuevo} alGuardar={(c) => navigate(`/clientes/${c.id}`)} />
      </Modal>
    </>
  );
}
