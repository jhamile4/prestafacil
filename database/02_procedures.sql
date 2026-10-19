SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* =============================================================
   PrestaFácil — Procedimientos almacenados
   Los cálculos de negocio (cronograma, evaluación, aplicación de
   pagos) viven en la API en C# para poder probarlos con xUnit.
   Aquí van el proceso diario de mora y los reportes, que son
   consultas sobre muchos registros y conviene resolver en la BD.
   ============================================================= */

USE PrestaFacilDB;
GO

/* -------------------------------------------------------------
   Proceso diario de mora (RF-14, RN-08)
   Marca como VENCIDA toda cuota con saldo después de su fecha
   de vencimiento y recalcula su mora a la fecha de corte.
   Es idempotente: se puede ejecutar varias veces el mismo día.
   ------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_ActualizarCuotasVencidas
    @FechaCorte DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @FechaCorte = ISNULL(@FechaCorte, CAST(GETDATE() AS DATE));

    UPDATE c
    SET
        -- La mora nunca baja de lo que el cliente ya pagó por ese concepto
        Mora = CASE
                   WHEN calc.MoraCalculada < c.MoraPagada THEN c.MoraPagada
                   ELSE calc.MoraCalculada
               END,
        Estado = 'VENCIDA'
    FROM Cuotas c
    INNER JOIN Prestamos p ON p.Id = c.PrestamoId
    CROSS APPLY (
        SELECT CAST(ROUND(
                   (c.Capital + c.Interes - c.CapitalPagado - c.InteresPagado)
                   * p.TasaMoraDiaria
                   * DATEDIFF(DAY, c.FechaVencimiento, @FechaCorte), 2) AS DECIMAL(12,2)) AS MoraCalculada
    ) calc
    WHERE p.Estado = 'DESEMBOLSADO'
      AND c.Estado <> 'PAGADA'
      AND c.FechaVencimiento < @FechaCorte
      AND (c.Capital + c.Interes - c.CapitalPagado - c.InteresPagado) > 0;

    SELECT @@ROWCOUNT AS CuotasActualizadas;
END
GO

/* -------------------------------------------------------------
   Reporte de cartera (RF-16)
   Cantidad de préstamos, monto desembolsado y saldo de capital
   pendiente por estado.
   ------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_ReporteCartera
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Estado,
        COUNT(*)                         AS CantidadPrestamos,
        SUM(p.Monto)                     AS MontoTotal,
        SUM(ISNULL(s.SaldoCapital, 0))   AS SaldoCapitalPendiente
    FROM Prestamos p
    OUTER APPLY (
        SELECT SUM(c.Capital - c.CapitalPagado) AS SaldoCapital
        FROM Cuotas c
        WHERE c.PrestamoId = p.Id
    ) s
    GROUP BY p.Estado
    ORDER BY CASE p.Estado
                 WHEN 'PENDIENTE'    THEN 1
                 WHEN 'APROBADO'     THEN 2
                 WHEN 'DESEMBOLSADO' THEN 3
                 WHEN 'PAGADO'       THEN 4
                 WHEN 'RECHAZADO'    THEN 5
             END;
END
GO

/* -------------------------------------------------------------
   Reporte de morosos (RF-17)
   Un registro por préstamo con cuotas vencidas.
   ------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_ReporteMorosos
    @DiasMinimos INT  = 1,
    @FechaCorte  DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @FechaCorte = ISNULL(@FechaCorte, CAST(GETDATE() AS DATE));

    SELECT
        cl.Dni,
        cl.Apellidos + ', ' + cl.Nombres                        AS Cliente,
        cl.Telefono,
        p.Codigo                                                AS Prestamo,
        u.NombreCompleto                                        AS Asesor,
        COUNT(*)                                                AS CuotasVencidas,
        MAX(DATEDIFF(DAY, c.FechaVencimiento, @FechaCorte))     AS DiasAtraso,
        SUM(c.Capital + c.Interes - c.CapitalPagado - c.InteresPagado) AS DeudaVencida,
        SUM(c.Mora - c.MoraPagada)                              AS MoraPendiente,
        SUM(c.Pendiente)                                        AS TotalAPagar
    FROM Cuotas c
    INNER JOIN Prestamos p ON p.Id = c.PrestamoId
    INNER JOIN Clientes cl ON cl.Id = p.ClienteId
    INNER JOIN Usuarios u  ON u.Id = p.AsesorId
    WHERE p.Estado = 'DESEMBOLSADO'
      AND c.Estado = 'VENCIDA'
    GROUP BY cl.Dni, cl.Apellidos, cl.Nombres, cl.Telefono, p.Codigo, u.NombreCompleto
    HAVING MAX(DATEDIFF(DAY, c.FechaVencimiento, @FechaCorte)) >= @DiasMinimos
    ORDER BY DiasAtraso DESC, TotalAPagar DESC;
END
GO

/* -------------------------------------------------------------
   Reporte de cobranza (RF-18)
   Pagos válidos (no anulados) entre dos fechas, por cajero y
   medio de pago.
   ------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_ReporteCobranza
    @Desde DATE,
    @Hasta DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @Hasta < @Desde
    BEGIN
        RAISERROR('La fecha final no puede ser menor que la inicial.', 16, 1);
        RETURN;
    END

    SELECT
        u.NombreCompleto       AS Cajero,
        pg.MedioPago,
        COUNT(*)               AS CantidadPagos,
        SUM(pg.Monto)          AS TotalCobrado,
        SUM(d.Capital)         AS Capital,
        SUM(d.Interes)         AS Interes,
        SUM(d.Mora)            AS Mora
    FROM Pagos pg
    INNER JOIN Usuarios u ON u.Id = pg.CajeroId
    CROSS APPLY (
        SELECT SUM(MontoCapital) AS Capital,
               SUM(MontoInteres) AS Interes,
               SUM(MontoMora)    AS Mora
        FROM PagoDetalles
        WHERE PagoId = pg.Id
    ) d
    WHERE pg.Anulado = 0
      AND pg.FechaPago >= @Desde
      AND pg.FechaPago <  DATEADD(DAY, 1, @Hasta)
    GROUP BY u.NombreCompleto, pg.MedioPago
    ORDER BY u.NombreCompleto, TotalCobrado DESC;
END
GO

/* -------------------------------------------------------------
   Estado de cuenta del cliente (RF-04)
   Devuelve dos resultados: resumen por préstamo y cuotas por pagar.
   ------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_EstadoCuentaCliente
    @ClienteId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Clientes WHERE Id = @ClienteId)
    BEGIN
        RAISERROR('El cliente no existe.', 16, 1);
        RETURN;
    END

    -- 1. Resumen por préstamo
    SELECT
        p.Codigo,
        pr.Nombre                                     AS Producto,
        p.Monto,
        p.PlazoMeses,
        p.Tea,
        p.CuotaMensual,
        p.Estado,
        p.FechaDesembolso,
        COUNT(c.Id)                                   AS TotalCuotas,
        SUM(CASE WHEN c.Estado = 'PAGADA'  THEN 1 ELSE 0 END) AS CuotasPagadas,
        SUM(CASE WHEN c.Estado = 'VENCIDA' THEN 1 ELSE 0 END) AS CuotasVencidas,
        ISNULL(SUM(c.Capital - c.CapitalPagado), 0)   AS SaldoCapital,
        ISNULL(SUM(c.Pendiente), 0)                   AS DeudaTotal
    FROM Prestamos p
    INNER JOIN ProductosPrestamo pr ON pr.Id = p.ProductoId
    LEFT JOIN Cuotas c ON c.PrestamoId = p.Id
    WHERE p.ClienteId = @ClienteId
    GROUP BY p.Id, p.Codigo, pr.Nombre, p.Monto, p.PlazoMeses, p.Tea,
             p.CuotaMensual, p.Estado, p.FechaDesembolso, p.FechaSolicitud
    ORDER BY p.FechaSolicitud DESC;

    -- 2. Cuotas pendientes de pago
    SELECT
        p.Codigo         AS Prestamo,
        c.NumeroCuota,
        c.FechaVencimiento,
        c.MontoCuota,
        c.Mora,
        c.Pendiente,
        c.Estado
    FROM Cuotas c
    INNER JOIN Prestamos p ON p.Id = c.PrestamoId
    WHERE p.ClienteId = @ClienteId
      AND c.Estado <> 'PAGADA'
    ORDER BY c.FechaVencimiento, p.Codigo;
END
GO

PRINT 'Procedimientos almacenados creados correctamente.';
GO
