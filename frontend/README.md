# PrestaFácil — Frontend

Aplicación web en React + TypeScript que consume la API de PrestaFácil. Las instrucciones generales están en el [README principal](../README.md).

## Scripts

| Comando | Qué hace |
|---|---|
| `npm run dev` | Servidor de desarrollo en http://localhost:5173 (redirige `/api` a `http://localhost:5039`; se puede cambiar con `VITE_API_PROXY`) |
| `npm test` | Pruebas con Vitest y Testing Library |
| `npm run typecheck` | Verificación de tipos con TypeScript estricto |
| `npm run build` | Build de producción en `dist/` |

## Organización

| Carpeta | Contenido |
|---|---|
| `src/api` | `client.ts` (fetch con token, errores ProblemDetails, aviso de sesión vencida), tipos y funciones por recurso |
| `src/auth` | Contexto de sesión y matriz de permisos por rol (la misma que aplica la API) |
| `src/components` | Componentes visuales (`ui.tsx`), layout con menú por rol, formularios reutilizables |
| `src/pages` | Una página por ruta; se cargan bajo demanda con `React.lazy` |
| `src/lib` | Formato de soles y fechas, hooks compartidos |

Los datos del servidor se manejan con **TanStack Query** (caché, estados de carga e invalidación después de cada operación). La interfaz oculta las opciones que el rol no puede usar, pero quien decide de verdad es la API.
