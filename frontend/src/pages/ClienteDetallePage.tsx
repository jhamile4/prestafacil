import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Calculator, Pencil, Power } from 'lucide-react';
import { clientesApi, prestamosApi } from '../api/endpoints';
import { useAuth } from '../auth/AuthContext';
import { fecha, porcentaje, soles } from '../lib/format';
import { mensajeError } from '../lib/hooks';
import { ClienteForm } from '../components/ClienteForm';
import { Aviso, Boton, Cargando, Dato, EncabezadoPagina, EstadoBadge, Indicador, Insignia, Modal, Tabla, Tarjeta, Td, Th, Vacio } from '../components/ui';

export function ClienteDetallePage() {
  const id = Number(useParams().id);
  const { puede } = useAuth();
  const queryClient = useQueryClient();
  const [editando, setEditando] = useState(false);

  const cuenta = useQuery({ queryKey: ['clientes', id, 'estado-cuenta'], queryFn: () => clientesApi.estadoCuenta(id) });
  // Ids de los préstamos para poder enlazarlos (el estado de cuenta trae solo el código)
  const prestamos = useQuery({ queryKey: ['prestamos', { clienteId: id }], queryFn: () => prestamosApi.buscar({ clienteId: id, tamanoPagina: 50 }) });

  const cambiarEstado = useMutation({
    mutationFn: (activar: boolean) => (activar ? clientesApi.activar(id) : clientesApi.desactivar(id)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['clientes'] }),
  });

  if (cuenta.isPending) return <Cargando />;
  if (cuenta.isError) return <Aviso>{mensajeError(cuenta.error)}</Aviso>;

  const { cliente, prestamos: resumen, cuotasPorPagar, deudaTotal } = cuenta.data;
  const idPorCodigo = new Map(prestamos.data?.items.map((p) => [p.codigo, p.id]));
  const vencidas = cuotasPorPagar.filter((c) => c.estado === 'VENCIDA');

  return (
    <>
      <Link to="/clientes" className="mb-4 inline-flex items-center gap-1 text-sm text-stone-500 hover:text-stone-800">
        <ArrowLeft className="size-4" /> Clientes
      </Link>
      <EncabezadoPagina
        titulo={cliente.nombreCompleto}
        descripcion={<>DNI {cliente.dni} · {cliente.edad} años · {cliente.activo ? <Insignia tono="marca">Activo</Insignia> : <Insignia>Desactivado</Insignia>}</>}
        acciones={
          <>
            {puede('solicitar') && cliente.activo && (
              <Link to={`/simulador?clienteId=${cliente.id}`} className="inline-flex items-center gap-2 rounded-lg bg-marca-700 px-3.5 py-2 text-sm font-semibold text-white hover:bg-marca-800">
                <Calculator className="size-4" /> Nueva solicitud
              </Link>
            )}
            {puede('editarClientes') && (
              <>
                <Boton variante="secundario" icono={<Pencil className="size-4" />} onClick={() => setEditando(true)}>Editar</Boton>
                <Boton variante="secundario" icono={<Power className="size-4" />} cargando={cambiarEstado.isPending} onClick={() => cambiarEstado.mutate(!cliente.activo)}>
                  {cliente.activo ? 'Desactivar' : 'Activar'}
                </Boton>
              </>
            )}
          </>
        }
      />

      {cambiarEstado.isError && <div className="mb-4"><Aviso>{mensajeError(cambiarEstado.error)}</Aviso></div>}

      <div className="grid gap-4 sm:grid-cols-3">
        <Indicador etiqueta="Deuda total" valor={soles(deudaTotal)} detalle={`${resumen.filter((p) => p.estado === 'DESEMBOLSADO').length} préstamo(s) vigente(s)`} />
        <Indicador etiqueta="Cuotas vencidas" valor={vencidas.length} tono={vencidas.length ? 'alerta' : 'exito'} detalle={vencidas.length ? `${soles(vencidas.reduce((s, c) => s + c.pendiente, 0))} por regularizar` : 'Al día'} />
        <Indicador etiqueta="Ingreso neto" valor={soles(cliente.ingresoNeto)} detalle={`Capacidad de pago: ${soles(cliente.ingresoNeto * 0.4)} al mes`} />
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-3">
        <Tarjeta titulo="Datos del cliente">
          <dl className="grid grid-cols-2 gap-4">
            <Dato etiqueta="Nacimiento">{fecha(cliente.fechaNacimiento)}</Dato>
            <Dato etiqueta="Celular">{cliente.telefono ?? '—'}</Dato>
            <div className="col-span-2"><Dato etiqueta="Correo">{cliente.email ?? '—'}</Dato></div>
            <div className="col-span-2"><Dato etiqueta="Dirección">{cliente.direccion ?? '—'}</Dato></div>
            <Dato etiqueta="Ingreso">{soles(cliente.ingresoMensual)}</Dato>
            <Dato etiqueta="Gastos">{soles(cliente.gastosMensuales)}</Dato>
            <Dato etiqueta="Registrado">{fecha(cliente.fechaCreacion)}</Dato>
          </dl>
        </Tarjeta>

        <Tarjeta titulo="Préstamos" className="min-w-0 xl:col-span-2">
          {resumen.length === 0 ? (
            <Vacio titulo="Sin préstamos todavía." />
          ) : (
            <Tabla>
              <thead>
                <tr>
                  <Th>Código</Th>
                  <Th>Producto</Th>
                  <Th derecha>Monto</Th>
                  <Th derecha>Cuotas</Th>
                  <Th derecha>Deuda</Th>
                  <Th>Estado</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {resumen.map((p) => {
                  const pid = idPorCodigo.get(p.codigo);
                  return (
                    <tr key={p.codigo}>
                      <Td>
                        {pid ? <Link to={`/prestamos/${pid}`} className="font-semibold text-marca-700 hover:underline">{p.codigo}</Link> : p.codigo}
                      </Td>
                      <Td>{p.producto}<p className="text-xs text-stone-500">TEA {porcentaje(p.tea)} · {p.plazoMeses} meses</p></Td>
                      <Td derecha>{soles(p.monto)}</Td>
                      <Td derecha>
                        {p.totalCuotas ? `${p.cuotasPagadas}/${p.totalCuotas}` : '—'}
                        {p.cuotasVencidas > 0 && <p className="text-xs font-semibold text-red-600">{p.cuotasVencidas} vencida(s)</p>}
                      </Td>
                      <Td derecha>{soles(p.deudaTotal)}</Td>
                      <Td><EstadoBadge estado={p.estado} /></Td>
                    </tr>
                  );
                })}
              </tbody>
            </Tabla>
          )}
        </Tarjeta>
      </div>

      {cuotasPorPagar.length > 0 && (
        <Tarjeta titulo="Próximas cuotas por pagar" className="mt-6">
          <Tabla>
            <thead>
              <tr>
                <Th>Préstamo</Th>
                <Th>N.º</Th>
                <Th>Vence</Th>
                <Th derecha>Cuota</Th>
                <Th derecha>Mora</Th>
                <Th derecha>Pendiente</Th>
                <Th>Estado</Th>
              </tr>
            </thead>
            <tbody className="divide-y divide-stone-100">
              {cuotasPorPagar.slice(0, 8).map((c) => (
                <tr key={`${c.prestamo}-${c.numeroCuota}`}>
                  <Td>{c.prestamo}</Td>
                  <Td>{c.numeroCuota}</Td>
                  <Td>{fecha(c.fechaVencimiento)}</Td>
                  <Td derecha>{soles(c.montoCuota)}</Td>
                  <Td derecha className={c.mora > 0 ? 'text-red-600' : ''}>{soles(c.mora)}</Td>
                  <Td derecha className="font-semibold">{soles(c.pendiente)}</Td>
                  <Td><EstadoBadge estado={c.estado} /></Td>
                </tr>
              ))}
            </tbody>
          </Tabla>
        </Tarjeta>
      )}

      <Modal abierto={editando} titulo="Editar cliente" alCerrar={() => setEditando(false)} ancho="max-w-2xl">
        <ClienteForm cliente={cliente} alCancelar={() => setEditando(false)} alGuardar={() => setEditando(false)} />
      </Modal>
    </>
  );
}
