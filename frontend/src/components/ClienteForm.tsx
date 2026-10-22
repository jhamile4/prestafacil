import { useState, type FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { clientesApi } from '../api/endpoints';
import type { Cliente, ClienteDatos } from '../api/types';
import { errorCampo, errorGeneral } from '../lib/hooks';
import { Aviso, Boton, Entrada } from './ui';

/** Crea un cliente (sin `cliente`) o edita uno existente. El DNI no se puede cambiar. */
export function ClienteForm({ cliente, alGuardar, alCancelar }: { cliente?: Cliente; alGuardar: (c: Cliente) => void; alCancelar: () => void }) {
  const queryClient = useQueryClient();
  const [dni, setDni] = useState(cliente?.dni ?? '');
  const [datos, setDatos] = useState<Record<keyof ClienteDatos, string>>({
    nombres: cliente?.nombres ?? '',
    apellidos: cliente?.apellidos ?? '',
    fechaNacimiento: cliente?.fechaNacimiento ?? '',
    telefono: cliente?.telefono ?? '',
    email: cliente?.email ?? '',
    direccion: cliente?.direccion ?? '',
    ingresoMensual: cliente ? String(cliente.ingresoMensual) : '',
    gastosMensuales: cliente ? String(cliente.gastosMensuales) : '0',
  });

  const guardar = useMutation({
    mutationFn: () => {
      const cuerpo: ClienteDatos = {
        nombres: datos.nombres,
        apellidos: datos.apellidos,
        fechaNacimiento: datos.fechaNacimiento,
        telefono: datos.telefono || null,
        email: datos.email || null,
        direccion: datos.direccion || null,
        ingresoMensual: Number(datos.ingresoMensual),
        gastosMensuales: Number(datos.gastosMensuales || 0),
      };
      return cliente ? clientesApi.actualizar(cliente.id, cuerpo) : clientesApi.crear({ ...cuerpo, dni });
    },
    onSuccess: (guardado) => {
      queryClient.invalidateQueries({ queryKey: ['clientes'] });
      alGuardar(guardado);
    },
  });

  const cambiar = (campo: keyof ClienteDatos) => (e: { target: { value: string } }) =>
    setDatos((d) => ({ ...d, [campo]: e.target.value }));
  const err = (campo: string) => errorCampo(guardar.error, campo);

  function enviar(e: FormEvent) {
    e.preventDefault();
    guardar.mutate();
  }

  const neto = Number(datos.ingresoMensual || 0) - Number(datos.gastosMensuales || 0);

  return (
    <form onSubmit={enviar} className="space-y-4" noValidate>
      {errorGeneral(guardar.error) && <Aviso>{errorGeneral(guardar.error)}</Aviso>}

      <div className="grid gap-4 sm:grid-cols-2">
        <Entrada etiqueta="DNI" inputMode="numeric" maxLength={8} required disabled={!!cliente} value={dni} onChange={(e) => setDni(e.target.value.replace(/\D/g, ''))} error={err('dni')} ayuda={cliente ? 'El DNI no se puede modificar.' : '8 dígitos'} />
        <Entrada etiqueta="Fecha de nacimiento" type="date" required value={datos.fechaNacimiento} onChange={cambiar('fechaNacimiento')} error={err('fechaNacimiento')} />
        <Entrada etiqueta="Nombres" required value={datos.nombres} onChange={cambiar('nombres')} error={err('nombres')} />
        <Entrada etiqueta="Apellidos" required value={datos.apellidos} onChange={cambiar('apellidos')} error={err('apellidos')} />
        <Entrada etiqueta="Celular" inputMode="tel" maxLength={9} value={datos.telefono} onChange={cambiar('telefono')} error={err('telefono')} ayuda="Opcional · 9 dígitos" />
        <Entrada etiqueta="Correo" type="email" value={datos.email} onChange={cambiar('email')} error={err('email')} ayuda="Opcional" />
      </div>
      <Entrada etiqueta="Dirección" value={datos.direccion} onChange={cambiar('direccion')} error={err('direccion')} ayuda="Opcional" />
      <div className="grid gap-4 sm:grid-cols-2">
        <Entrada etiqueta="Ingreso mensual (S/)" type="number" min="0" step="0.01" required value={datos.ingresoMensual} onChange={cambiar('ingresoMensual')} error={err('ingresoMensual')} />
        <Entrada etiqueta="Gastos mensuales (S/)" type="number" min="0" step="0.01" value={datos.gastosMensuales} onChange={cambiar('gastosMensuales')} error={err('gastosMensuales')} />
      </div>
      <p className="text-sm text-stone-600">
        Ingreso neto: <strong className="tabular">S/ {neto.toFixed(2)}</strong> — la suma de sus cuotas no podrá superar el 40 %:{' '}
        <strong className="tabular">S/ {Math.max(0, neto * 0.4).toFixed(2)}</strong>.
      </p>

      <div className="flex justify-end gap-2 pt-2">
        <Boton variante="secundario" onClick={alCancelar}>Cancelar</Boton>
        <Boton type="submit" cargando={guardar.isPending}>{cliente ? 'Guardar cambios' : 'Registrar cliente'}</Boton>
      </div>
    </form>
  );
}
