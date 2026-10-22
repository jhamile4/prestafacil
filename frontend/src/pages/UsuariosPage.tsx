import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { KeyRound, Pencil, Power, UserPlus } from 'lucide-react';
import { usuariosApi } from '../api/endpoints';
import type { Rol, Usuario } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { nombreRol } from '../auth/permisos';
import { fecha } from '../lib/format';
import { errorCampo, errorGeneral, mensajeError } from '../lib/hooks';
import { Aviso, Boton, Cargando, EncabezadoPagina, Entrada, Insignia, Modal, Seleccion, Tabla, Tarjeta, Td, Th } from '../components/ui';

type Edicion = { tipo: 'nuevo' } | { tipo: 'editar'; usuario: Usuario } | { tipo: 'password'; usuario: Usuario } | null;

export function UsuariosPage() {
  const { usuario: yo } = useAuth();
  const queryClient = useQueryClient();
  const [edicion, setEdicion] = useState<Edicion>(null);
  const { data, isPending, isError, error } = useQuery({ queryKey: ['usuarios'], queryFn: usuariosApi.listar });

  const cambiarEstado = useMutation({
    mutationFn: (u: Usuario) => (u.activo ? usuariosApi.desactivar(u.id) : usuariosApi.activar(u.id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['usuarios'] });
      queryClient.invalidateQueries({ queryKey: ['directorio'] });
    },
  });

  const cerrar = () => setEdicion(null);

  return (
    <>
      <EncabezadoPagina
        titulo="Usuarios"
        descripcion="Al desactivar a alguien o cambiarle el rol, su sesión deja de funcionar al instante."
        acciones={<Boton icono={<UserPlus className="size-4" />} onClick={() => setEdicion({ tipo: 'nuevo' })}>Nuevo usuario</Boton>}
      />
      {cambiarEstado.isError && <div className="mb-4"><Aviso>{mensajeError(cambiarEstado.error)}</Aviso></div>}

      <Tarjeta>
        {isPending ? <Cargando /> : isError ? <Aviso>{mensajeError(error)}</Aviso> : (
          <Tabla>
            <thead>
              <tr>
                <Th>Usuario</Th>
                <Th>Rol</Th>
                <Th>Correo</Th>
                <Th>Creado</Th>
                <Th>Estado</Th>
                <Th derecha>Acciones</Th>
              </tr>
            </thead>
            <tbody className="divide-y divide-stone-100">
              {data.map((u) => (
                <tr key={u.id}>
                  <Td>
                    <p className="font-semibold text-stone-900">{u.nombreCompleto}{u.id === yo?.id && <span className="ml-2 text-xs font-normal text-stone-500">(tú)</span>}</p>
                    <p className="text-xs text-stone-500">@{u.nombreUsuario}</p>
                  </Td>
                  <Td>{nombreRol[u.rol]}</Td>
                  <Td className="text-stone-600">{u.email}</Td>
                  <Td>{fecha(u.fechaCreacion)}</Td>
                  <Td>{u.activo ? <Insignia tono="marca">Activo</Insignia> : <Insignia tono="alerta">Desactivado</Insignia>}</Td>
                  <Td derecha>
                    <div className="flex justify-end gap-1">
                      <Boton variante="fantasma" title="Editar" aria-label={`Editar a ${u.nombreCompleto}`} onClick={() => setEdicion({ tipo: 'editar', usuario: u })}><Pencil className="size-4" /></Boton>
                      <Boton variante="fantasma" title="Restablecer contraseña" aria-label={`Restablecer contraseña de ${u.nombreCompleto}`} onClick={() => setEdicion({ tipo: 'password', usuario: u })}><KeyRound className="size-4" /></Boton>
                      {u.id !== yo?.id && (
                        <Boton variante="fantasma" title={u.activo ? 'Desactivar' : 'Activar'} aria-label={`${u.activo ? 'Desactivar' : 'Activar'} a ${u.nombreCompleto}`} onClick={() => cambiarEstado.mutate(u)}><Power className="size-4" /></Boton>
                      )}
                    </div>
                  </Td>
                </tr>
              ))}
            </tbody>
          </Tabla>
        )}
      </Tarjeta>

      <Modal abierto={edicion?.tipo === 'nuevo' || edicion?.tipo === 'editar'} titulo={edicion?.tipo === 'editar' ? 'Editar usuario' : 'Nuevo usuario'} alCerrar={cerrar}>
        {(edicion?.tipo === 'nuevo' || edicion?.tipo === 'editar') && (
          <FormUsuario usuario={edicion.tipo === 'editar' ? edicion.usuario : undefined} esYo={edicion.tipo === 'editar' && edicion.usuario.id === yo?.id} alTerminar={cerrar} />
        )}
      </Modal>
      <Modal abierto={edicion?.tipo === 'password'} titulo="Restablecer contraseña" alCerrar={cerrar}>
        {edicion?.tipo === 'password' && <FormPassword usuario={edicion.usuario} alTerminar={cerrar} />}
      </Modal>
    </>
  );
}

function FormUsuario({ usuario, esYo, alTerminar }: { usuario?: Usuario; esYo: boolean; alTerminar: () => void }) {
  const queryClient = useQueryClient();
  const [nombreUsuario, setNombreUsuario] = useState('');
  const [nombreCompleto, setNombreCompleto] = useState(usuario?.nombreCompleto ?? '');
  const [email, setEmail] = useState(usuario?.email ?? '');
  const [rol, setRol] = useState<Rol>(usuario?.rol ?? 'Asesor');
  const [password, setPassword] = useState('');

  const guardar = useMutation({
    mutationFn: () => usuario
      ? usuariosApi.actualizar(usuario.id, { nombreCompleto, email, rol })
      : usuariosApi.crear({ nombreUsuario, nombreCompleto, email, rol, password }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['usuarios'] });
      queryClient.invalidateQueries({ queryKey: ['directorio'] });
      alTerminar();
    },
  });
  const err = (c: string) => errorCampo(guardar.error, c);

  function enviar(e: FormEvent) {
    e.preventDefault();
    guardar.mutate();
  }

  return (
    <form onSubmit={enviar} className="space-y-4" noValidate>
      {errorGeneral(guardar.error) && <Aviso>{errorGeneral(guardar.error)}</Aviso>}
      {!usuario && <Entrada etiqueta="Usuario" required value={nombreUsuario} onChange={(e) => setNombreUsuario(e.target.value)} error={err('nombreUsuario')} ayuda="Para iniciar sesión, ej. jperez" />}
      <Entrada etiqueta="Nombre completo" required value={nombreCompleto} onChange={(e) => setNombreCompleto(e.target.value)} error={err('nombreCompleto')} />
      <Entrada etiqueta="Correo" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} error={err('email')} />
      <Seleccion etiqueta="Rol" value={rol} onChange={(e) => setRol(e.target.value as Rol)} disabled={esYo} ayuda={esYo ? 'No puedes quitarte el rol de administrador.' : undefined}>
        {(['Admin', 'Asesor', 'Cajero'] as Rol[]).map((r) => <option key={r} value={r}>{nombreRol[r]}</option>)}
      </Seleccion>
      {!usuario && <Entrada etiqueta="Contraseña inicial" type="password" autoComplete="new-password" required value={password} onChange={(e) => setPassword(e.target.value)} error={err('password')} ayuda="Mínimo 8 caracteres, con mayúscula, minúscula y número." />}
      <div className="flex justify-end gap-2">
        <Boton variante="secundario" onClick={alTerminar}>Cancelar</Boton>
        <Boton type="submit" cargando={guardar.isPending}>{usuario ? 'Guardar' : 'Crear usuario'}</Boton>
      </div>
    </form>
  );
}

function FormPassword({ usuario, alTerminar }: { usuario: Usuario; alTerminar: () => void }) {
  const [password, setPassword] = useState('');
  const guardar = useMutation({ mutationFn: () => usuariosApi.restablecerPassword(usuario.id, password), onSuccess: alTerminar });

  return (
    <form onSubmit={(e) => { e.preventDefault(); guardar.mutate(); }} className="space-y-4" noValidate>
      <p className="text-sm text-stone-600">Nueva contraseña para <strong>{usuario.nombreCompleto}</strong>. Compártela por un canal seguro.</p>
      {errorGeneral(guardar.error) && <Aviso>{errorGeneral(guardar.error)}</Aviso>}
      <Entrada etiqueta="Nueva contraseña" type="password" autoComplete="new-password" required value={password} onChange={(e) => setPassword(e.target.value)} error={errorCampo(guardar.error, 'passwordNueva')} ayuda="Mínimo 8 caracteres, con mayúscula, minúscula y número." />
      <div className="flex justify-end gap-2">
        <Boton variante="secundario" onClick={alTerminar}>Cancelar</Boton>
        <Boton type="submit" cargando={guardar.isPending}>Restablecer</Boton>
      </div>
    </form>
  );
}
