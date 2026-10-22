import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Calculator, Send, X } from 'lucide-react';
import { clientesApi, prestamosApi } from '../api/endpoints';
import type { Cliente, Simulacion } from '../api/types';
import { fecha, porcentaje, soles } from '../lib/format';
import { errorCampo, errorGeneral, mensajeError, useProductos } from '../lib/hooks';
import { EvaluacionPanel } from '../components/Evaluacion';
import { Aviso, Boton, Cargando, EncabezadoPagina, Entrada, Indicador, Seleccion, Tabla, Tarjeta, Td, Th, Vacio } from '../components/ui';

export function SimuladorPage() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const productos = useProductos();
  const [productoId, setProductoId] = useState('');
  const [monto, setMonto] = useState('5000');
  const [plazo, setPlazo] = useState('12');
  const [cliente, setCliente] = useState<Cliente | null>(null);
  const [resultado, setResultado] = useState<Simulacion | null>(null);

  // Viene desde la ficha del cliente: /simulador?clienteId=3
  const clienteId = Number(params.get('clienteId')) || null;
  const clienteInicial = useQuery({ queryKey: ['clientes', clienteId], queryFn: () => clientesApi.obtener(clienteId!), enabled: !!clienteId });
  useEffect(() => { if (clienteInicial.data) setCliente(clienteInicial.data); }, [clienteInicial.data]);

  useEffect(() => {
    if (!productoId && productos.data?.length) setProductoId(String(productos.data.find((p) => p.nombre.includes('Personal'))?.id ?? productos.data[0]!.id));
  }, [productos.data, productoId]);

  const producto = productos.data?.find((p) => p.id === Number(productoId));

  const simular = useMutation({
    mutationFn: () => prestamosApi.simular({ productoId: Number(productoId), monto: Number(monto), plazoMeses: Number(plazo), clienteId: cliente?.id }),
    onSuccess: setResultado,
    onError: () => setResultado(null),
  });

  const solicitar = useMutation({
    mutationFn: () => prestamosApi.solicitar({ clienteId: cliente!.id, productoId: Number(productoId), monto: Number(monto), plazoMeses: Number(plazo) }),
    onSuccess: (detalle) => navigate(`/prestamos/${detalle.prestamo.id}`),
  });

  // Si cambian los datos, el resultado anterior ya no corresponde
  const { reset: limpiarSolicitud } = solicitar;
  useEffect(() => { setResultado(null); limpiarSolicitud(); }, [productoId, monto, plazo, cliente, limpiarSolicitud]);

  function enviar(e: FormEvent) {
    e.preventDefault();
    simular.mutate();
  }

  return (
    <>
      <EncabezadoPagina titulo="Simulador de préstamos" descripcion="Calcula la cuota y el cronograma; si eliges un cliente, también su evaluación crediticia." />

      <div className="grid gap-6 xl:grid-cols-[380px_1fr]">
        <Tarjeta titulo="Datos del préstamo">
          {productos.isPending ? (
            <Cargando />
          ) : (
            <form onSubmit={enviar} className="space-y-4">
              <Seleccion etiqueta="Producto" value={productoId} onChange={(e) => setProductoId(e.target.value)}>
                {productos.data?.map((p) => <option key={p.id} value={p.id}>{p.nombre}</option>)}
              </Seleccion>
              {producto && (
                <p className="rounded-lg bg-stone-50 px-3 py-2 text-xs text-stone-600">
                  TEA {porcentaje(producto.tea)} · {soles(producto.montoMinimo)} a {soles(producto.montoMaximo)} · {producto.plazoMinimoMeses} a {producto.plazoMaximoMeses} meses · Mora {porcentaje(producto.tasaMoraDiaria, 2)} diario
                </p>
              )}
              <Entrada etiqueta="Monto (S/)" type="number" min="0" step="100" required value={monto} onChange={(e) => setMonto(e.target.value)} error={errorCampo(simular.error, 'monto')} />
              <Entrada etiqueta="Plazo (meses)" type="number" min="1" required value={plazo} onChange={(e) => setPlazo(e.target.value)} error={errorCampo(simular.error, 'plazoMeses')} />

              <BuscadorCliente cliente={cliente} alElegir={setCliente} />

              {errorGeneral(simular.error) && <Aviso>{errorGeneral(simular.error)}</Aviso>}
              <Boton type="submit" className="w-full" cargando={simular.isPending} icono={<Calculator className="size-4" />}>
                Simular
              </Boton>
            </form>
          )}
        </Tarjeta>

        <div className="min-w-0 space-y-6">
          {!resultado ? (
            <Tarjeta><Vacio titulo="Ingresa los datos y presiona Simular." descripcion="Elige un cliente para ver si califica y poder registrar la solicitud." /></Tarjeta>
          ) : (
            <>
              <div className="grid gap-4 sm:grid-cols-3">
                <Indicador etiqueta="Cuota mensual" valor={soles(resultado.cuotaMensual)} detalle={`TEM ${porcentaje(resultado.tem, 4)}`} tono="exito" />
                <Indicador etiqueta="Total de intereses" valor={soles(resultado.totalIntereses)} detalle={`TEA ${porcentaje(resultado.tea)}`} />
                <Indicador etiqueta="Total a pagar" valor={soles(resultado.totalAPagar)} detalle={`${resultado.plazoMeses} cuotas`} />
              </div>

              {resultado.evaluacion && cliente && (
                <Tarjeta
                  titulo={`Evaluación de ${cliente.nombreCompleto}`}
                  acciones={
                    <Boton icono={<Send className="size-4" />} cargando={solicitar.isPending} onClick={() => solicitar.mutate()}>
                      Registrar solicitud
                    </Boton>
                  }
                >
                  {solicitar.isError && <div className="mb-4"><Aviso>{mensajeError(solicitar.error)}</Aviso></div>}
                  {!resultado.evaluacion.apto && (
                    <div className="mb-4"><Aviso tipo="info">Puedes registrar la solicitud, pero el administrador no podrá aprobarla mientras no cumpla las reglas.</Aviso></div>
                  )}
                  <EvaluacionPanel evaluacion={resultado.evaluacion} />
                </Tarjeta>
              )}

              <Tarjeta titulo="Cronograma (sistema francés)">
                <Tabla>
                  <thead>
                    <tr>
                      <Th>N.º</Th>
                      <Th>Vencimiento</Th>
                      <Th derecha>Saldo inicial</Th>
                      <Th derecha>Capital</Th>
                      <Th derecha>Interés</Th>
                      <Th derecha>Cuota</Th>
                      <Th derecha>Saldo final</Th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-stone-100">
                    {resultado.cronograma.map((c) => (
                      <tr key={c.numero}>
                        <Td>{c.numero}</Td>
                        <Td>{fecha(c.fechaVencimiento)}</Td>
                        <Td derecha>{soles(c.saldoInicial)}</Td>
                        <Td derecha>{soles(c.capital)}</Td>
                        <Td derecha className="text-stone-500">{soles(c.interes)}</Td>
                        <Td derecha className="font-semibold">{soles(c.cuota)}</Td>
                        <Td derecha>{soles(c.saldoFinal)}</Td>
                      </tr>
                    ))}
                  </tbody>
                </Tabla>
              </Tarjeta>
            </>
          )}
        </div>
      </div>
    </>
  );
}

function BuscadorCliente({ cliente, alElegir }: { cliente: Cliente | null; alElegir: (c: Cliente | null) => void }) {
  const [texto, setTexto] = useState('');
  const [buscar, setBuscar] = useState('');

  useEffect(() => {
    const t = setTimeout(() => setBuscar(texto.trim()), 300);
    return () => clearTimeout(t);
  }, [texto]);

  const resultados = useQuery({
    queryKey: ['clientes', { buscar, pagina: 1, soloActivos: true, tamano: 5 }],
    queryFn: () => clientesApi.buscar({ buscar, tamanoPagina: 5 }),
    enabled: buscar.length >= 2,
  });

  if (cliente) {
    return (
      <div>
        <p className="mb-1.5 text-sm font-medium text-stone-700">Cliente</p>
        <div className="flex items-center justify-between rounded-lg border border-marca-500/40 bg-marca-50 px-3 py-2">
          <div>
            <p className="text-sm font-semibold text-stone-900">{cliente.nombreCompleto}</p>
            <p className="text-xs text-stone-600">DNI {cliente.dni} · neto {soles(cliente.ingresoNeto)}</p>
          </div>
          <button type="button" onClick={() => alElegir(null)} className="rounded p-1 text-stone-500 hover:bg-white" aria-label="Quitar cliente">
            <X className="size-4" />
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="relative">
      <Entrada etiqueta="Cliente (opcional)" placeholder="DNI o nombre…" value={texto} onChange={(e) => setTexto(e.target.value)} ayuda="Para ver su evaluación y registrar la solicitud." />
      {buscar.length >= 2 && resultados.data && (
        <ul className="absolute z-10 mt-1 w-full overflow-hidden rounded-lg border border-stone-200 bg-white shadow-lg">
          {resultados.data.items.length === 0 ? (
            <li className="px-3 py-2 text-sm text-stone-500">Sin resultados</li>
          ) : (
            resultados.data.items.map((c) => (
              <li key={c.id}>
                <button type="button" onClick={() => { alElegir(c); setTexto(''); }} className="w-full px-3 py-2 text-left text-sm hover:bg-stone-50">
                  <span className="font-medium">{c.nombreCompleto}</span> <span className="text-stone-500">· {c.dni}</span>
                </button>
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
}
