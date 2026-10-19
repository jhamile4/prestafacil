# PrestaFácil

Sistema de gestión de préstamos personales para cajas y financieras pequeñas. Cubre el ciclo completo del crédito: registro del cliente, evaluación de capacidad de pago, aprobación, desembolso, cronograma de cuotas (sistema francés), pagos, mora y reportes.

> 🚧 **En desarrollo.** Fase actual: diseño de base de datos (terminada).

## Stack

| Capa | Tecnología |
|---|---|
| Base de datos | SQL Server, procedimientos almacenados |
| Backend | ASP.NET Core Web API (.NET), C#, Entity Framework Core *(próximamente)* |
| Seguridad | JWT, roles (ADMIN, ASESOR, CAJERO), BCrypt *(próximamente)* |
| Calidad | xUnit, Swagger, Docker, GitHub Actions *(próximamente)* |
| Frontend | React *(próximamente)* |

## Documentación

- [Requerimientos y reglas de negocio](docs/01_requerimientos.md)
- [Modelo de datos y decisiones de diseño](docs/02_modelo_datos.md)

## Crear la base de datos

Requisito: SQL Server (Express funciona). Ejecuta los scripts en orden, ya sea en SSMS o con `sqlcmd`:

```bash
cd database
sqlcmd -S .\SQLEXPRESS -E -i 01_schema.sql
sqlcmd -S .\SQLEXPRESS -E -i 02_procedures.sql
sqlcmd -S .\SQLEXPRESS -E -i 03_seed.sql
```

> ⚠️ `01_schema.sql` borra y vuelve a crear la base `PrestaFacilDB`.

Pruébalo:

```sql
USE PrestaFacilDB;
EXEC sp_ReporteCartera;
EXEC sp_ReporteMorosos;
EXEC sp_EstadoCuentaCliente @ClienteId = 1;
```

## Hoja de ruta

- [x] Requerimientos y reglas de negocio
- [x] Modelo de datos, procedimientos almacenados y datos de prueba
- [ ] API base en capas y módulo de clientes
- [ ] Préstamos: simulador, evaluación, cronograma
- [ ] Pagos y mora
- [ ] Autenticación JWT y roles
- [ ] Pruebas unitarias, Docker y CI
- [ ] Frontend en React y despliegue

## Autora

**Jhamile Macavilca** — Estudiante de Diseño y Desarrollo de Software, Tecsup
[GitHub](https://github.com/jhamile4) · [LinkedIn](https://www.linkedin.com/in/jhamile-jhersy-macavilca-nolasco-239800361/)
