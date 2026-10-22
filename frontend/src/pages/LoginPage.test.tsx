import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { AuthProvider } from '../auth/AuthContext';
import { LoginPage } from './LoginPage';

function renderLogin() {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <AuthProvider>
        <MemoryRouter initialEntries={['/login']}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/" element={<p>Pantalla de inicio</p>} />
          </Routes>
        </MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

const json = (status: number, cuerpo: unknown) =>
  new Response(JSON.stringify(cuerpo), { status, headers: { 'Content-Type': 'application/json' } });

afterEach(() => vi.unstubAllGlobals());

describe('LoginPage', () => {
  it('muestra el mensaje de la API si las credenciales son incorrectas', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json(401, { detail: 'Usuario o contraseña incorrectos.' })));
    renderLogin();

    await userEvent.type(screen.getByLabelText('Usuario'), 'admin');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'mala');
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Usuario o contraseña incorrectos.');
    expect(localStorage.getItem('prestafacil.token')).toBeNull();
  });

  it('guarda el token y entra al inicio si el login es correcto', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json(200, {
      token: 'token-jwt',
      tipoToken: 'Bearer',
      expiraEn: '2026-10-21T20:00:00Z',
      usuario: { id: 1, nombreUsuario: 'admin', nombreCompleto: 'Rosa Quispe', email: 'a@b.pe', rol: 'Admin', activo: true, fechaCreacion: '2026-10-01' },
    })));
    renderLogin();

    await userEvent.type(screen.getByLabelText('Usuario'), 'admin');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'Correcta2026!');
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }));

    expect(await screen.findByText('Pantalla de inicio')).toBeInTheDocument();
    expect(localStorage.getItem('prestafacil.token')).toBe('token-jwt');
  });
});
