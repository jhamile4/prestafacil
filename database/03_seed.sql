SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* =============================================================
   PrestaFácil — Datos de prueba
   Las fechas se calculan a partir de HOY para que siempre haya
   préstamos al día, en mora y pagados, sin importar cuándo se
   ejecute el script.
   ============================================================= */

USE PrestaFacilDB;
GO

SET NOCOUNT ON;

DECLARE @Hoy DATE = CAST(GETDATE() AS DATE);

/* -------------------------------------------------------------
   Usuarios
   PasswordHash es temporal: SQL Server no calcula BCrypt. Al iniciar en
   modo desarrollo, la API (DatosDemoSeeder) reemplaza estos valores por
   hashes BCrypt reales al iniciar en modo desarrollo.
   ------------------------------------------------------------- */
SET IDENTITY_INSERT Usuarios ON;
INSERT INTO Usuarios (Id, NombreUsuario, NombreCompleto, Email, PasswordHash, Rol) VALUES
(1, 'admin',    N'Rosa Quispe Huamán',    'admin@prestafacil.pe',    'PENDIENTE_HASH', 'ADMIN'),
(2, 'asesor1',  N'Carlos Mendoza Ríos',   'cmendoza@prestafacil.pe', 'PENDIENTE_HASH', 'ASESOR'),
(3, 'asesor2',  N'Lucía Vargas Torres',   'lvargas@prestafacil.pe',  'PENDIENTE_HASH', 'ASESOR'),
(4, 'cajero1',  N'Miguel Ángel Salazar',  'msalazar@prestafacil.pe', 'PENDIENTE_HASH', 'CAJERO');
SET IDENTITY_INSERT Usuarios OFF;

/* -------------------------------------------------------------
   Productos de préstamo
   ------------------------------------------------------------- */
SET IDENTITY_INSERT ProductosPrestamo ON;
INSERT INTO ProductosPrestamo (Id, Nombre, Descripcion, Tea, MontoMinimo, MontoMaximo, PlazoMinimoMeses, PlazoMaximoMeses, TasaMoraDiaria) VALUES
(1, N'Préstamo Personal',    N'Para gastos personales, salud o educación.',          0.350000,  1000.00, 15000.00, 6, 36, 0.001000),
(2, N'Préstamo Emprendedor', N'Capital de trabajo para negocios con 6+ meses.',      0.280000,  3000.00, 30000.00, 6, 24, 0.000800),
(3, N'Préstamo Express',     N'Montos pequeños de aprobación rápida.',               0.450000,   500.00,  3000.00, 3, 12, 0.001500);
SET IDENTITY_INSERT ProductosPrestamo OFF;

/* -------------------------------------------------------------
   Clientes
   ------------------------------------------------------------- */
SET IDENTITY_INSERT Clientes ON;
INSERT INTO Clientes (Id, Dni, Nombres, Apellidos, FechaNacimiento, Telefono, Email, Direccion, IngresoMensual, GastosMensuales, CreadoPor) VALUES
(1, '45879632', N'Ana María',  N'Flores Gutiérrez', '1990-03-14', '987654321', 'ana.flores@gmail.com',   N'Av. Los Olivos 245, Los Olivos',    4500.00, 1800.00, 2),
(2, '41236578', N'Jorge Luis', N'Ramírez Castillo', '1985-07-22', '976543210', 'jramirez@hotmail.com',   N'Jr. Huallaga 812, Cercado de Lima', 6500.00, 2500.00, 2),
(3, '70125489', N'Diana',      N'Paredes Soto',     '1998-11-05', '965432109', 'diana.paredes@gmail.com',N'Calle Las Begonias 150, San Isidro',3800.00, 1500.00, 3),
(4, '46985231', N'Pedro',      N'Huamán Ccori',     '1992-01-30', '954321098', NULL,                     N'Mz. B Lt. 12, Villa El Salvador',   2800.00, 1300.00, 3),
(5, '72563148', N'Valeria',    N'Chávez Medina',    '2000-05-18', '943210987', 'vchavez@outlook.com',    N'Av. Universitaria 3500, Comas',     5200.00, 2000.00, 2),
(6, '40789512', N'Roberto',    N'Gonzales Paz',     '1980-09-09', '932109876', 'rgonzales@gmail.com',    N'Av. Javier Prado 1200, La Victoria',9000.00, 3500.00, 3),
(7, '48521697', N'Silvia',     N'Torres Ñique',     '1995-12-12', '921098765', NULL,                     N'Jr. Ayacucho 340, Breña',           2200.00, 1200.00, 2),
(8, '73698521', N'Luis',       N'Espinoza Valle',   '2001-08-25', '910987654', 'luis.espinoza@gmail.com',N'Av. Perú 2100, San Martín de Porres',3200.00, 1400.00, 3);
SET IDENTITY_INSERT Clientes OFF;

/* -------------------------------------------------------------
   Préstamos
   Escenarios:
     1. Ana       — al día (8 de 12 cuotas pagadas)
     2. Jorge     — EN MORA (cuotas vencidas)
     3. Diana     — PAGADO completo
     4. Pedro     — 1 cuota vencida hace pocos días
     5. Valeria   — PENDIENTE de aprobación
     6. Roberto   — APROBADO, falta desembolsar
     7. Silvia    — RECHAZADO por capacidad de pago
     8. Ana       — segundo préstamo activo (llega al máximo RN-05)
   ------------------------------------------------------------- */
SET IDENTITY_INSERT Prestamos ON;
INSERT INTO Prestamos (Id, ClienteId, ProductoId, Monto, PlazoMeses, Tea, Tem, CuotaMensual, TasaMoraDiaria, Estado, AsesorId,
                       FechaSolicitud, AprobadoPorId, FechaAprobacion, MotivoRechazo, DesembolsadoPorId, FechaDesembolso)
SELECT v.Id, v.ClienteId, v.ProductoId, v.Monto, v.Plazo, pr.Tea, 0, 0, pr.TasaMoraDiaria, v.Estado, v.AsesorId,
       DATEADD(DAY, -v.DiasDesdeSolicitud, @Hoy),
       CASE WHEN v.Estado = 'PENDIENTE' THEN NULL ELSE 1 END,
       CASE WHEN v.Estado = 'PENDIENTE' THEN NULL ELSE DATEADD(DAY, -v.DiasDesdeSolicitud + 1, @Hoy) END,
       v.MotivoRechazo,
       CASE WHEN v.FechaDesembolso IS NULL THEN NULL ELSE 4 END,
       v.FechaDesembolso
FROM (VALUES
    (1, 1, 1,  5000.00, 12, 'DESEMBOLSADO', 2, 245, NULL, DATEADD(MONTH, -8, @Hoy)),
    (2, 2, 2, 15000.00, 18, 'DESEMBOLSADO', 2, 185, NULL, DATEADD(MONTH, -6, @Hoy)),
    (3, 3, 1,  3000.00,  6, 'PAGADO',       3, 215, NULL, DATEADD(MONTH, -7, @Hoy)),
    (4, 4, 3,  1500.00,  6, 'DESEMBOLSADO', 3,  75, NULL, DATEADD(DAY, -10, DATEADD(MONTH, -2, @Hoy))),
    (5, 5, 1,  8000.00, 24, 'PENDIENTE',    2,   1, NULL, NULL),
    (6, 6, 2, 20000.00, 12, 'APROBADO',     3,   3, NULL, NULL),
    (7, 7, 1, 10000.00, 12, 'RECHAZADO',    2,   5, N'La cuota supera el 40 % del ingreso neto del cliente (RN-04).', NULL),
    (8, 1, 3,  1000.00,  3, 'DESEMBOLSADO', 2,  17, NULL, DATEADD(DAY, -15, @Hoy))
) v (Id, ClienteId, ProductoId, Monto, Plazo, Estado, AsesorId, DiasDesdeSolicitud, MotivoRechazo, FechaDesembolso)
INNER JOIN ProductosPrestamo pr ON pr.Id = v.ProductoId;
SET IDENTITY_INSERT Prestamos OFF;

-- TEM y cuota fija con el sistema francés (ver docs/01_requerimientos.md, sección 4.1)
UPDATE p
SET Tem = t.Tem,
    CuotaMensual = ROUND(p.Monto * t.Tem / (1 - POWER(1 + t.Tem, -CAST(p.PlazoMeses AS FLOAT))), 2)
FROM Prestamos p
-- 1.0E0 es FLOAT. Con "1.0 / 12" SQL Server hace división DECIMAL y la corta a 0.083333,
-- lo que da una TEM ligeramente menor que la real.
CROSS APPLY (SELECT POWER(1 + CAST(p.Tea AS FLOAT), 1.0E0 / 12) - 1 AS Tem) t;

-- Historial de estados
INSERT INTO HistorialEstadosPrestamo (PrestamoId, EstadoAnterior, EstadoNuevo, UsuarioId, Fecha, Comentario)
SELECT Id, NULL, 'PENDIENTE', AsesorId, FechaSolicitud, N'Solicitud registrada' FROM Prestamos
UNION ALL
SELECT Id, 'PENDIENTE', CASE WHEN Estado = 'RECHAZADO' THEN 'RECHAZADO' ELSE 'APROBADO' END,
       AprobadoPorId, FechaAprobacion, ISNULL(MotivoRechazo, N'Cumple las reglas de evaluación')
FROM Prestamos WHERE AprobadoPorId IS NOT NULL
UNION ALL
SELECT Id, 'APROBADO', 'DESEMBOLSADO', DesembolsadoPorId, FechaDesembolso, N'Desembolso en efectivo'
FROM Prestamos WHERE FechaDesembolso IS NOT NULL;
GO

/* -------------------------------------------------------------
   Cronogramas de los préstamos desembolsados
   Este procedimiento temporal (#) solo existe durante el script.
   En la aplicación, el cronograma lo genera la API en C#.
   ------------------------------------------------------------- */
CREATE PROCEDURE #GenerarCronograma @PrestamoId INT
AS
BEGIN
    DECLARE @Saldo DECIMAL(12,2), @Tem DECIMAL(12,10), @Cuota DECIMAL(12,2),
            @Plazo TINYINT, @Desembolso DATE, @i TINYINT = 1,
            @Interes DECIMAL(12,2), @Capital DECIMAL(12,2);

    SELECT @Saldo = Monto, @Tem = Tem, @Cuota = CuotaMensual,
           @Plazo = PlazoMeses, @Desembolso = FechaDesembolso
    FROM Prestamos WHERE Id = @PrestamoId;

    WHILE @i <= @Plazo
    BEGIN
        SET @Interes = ROUND(@Saldo * @Tem, 2);
        -- La última cuota lleva todo el saldo restante para cerrar en 0
        SET @Capital = CASE WHEN @i = @Plazo THEN @Saldo ELSE @Cuota - @Interes END;

        INSERT INTO Cuotas (PrestamoId, NumeroCuota, FechaVencimiento, SaldoInicial, Capital, Interes)
        VALUES (@PrestamoId, @i, DATEADD(MONTH, @i, @Desembolso), @Saldo, @Capital, @Interes);

        SET @Saldo -= @Capital;
        SET @i += 1;
    END
END
GO

/* Registra el pago completo de las primeras @Cantidad cuotas,
   dos días antes de cada vencimiento. */
CREATE PROCEDURE #PagarCuotas @PrestamoId INT, @Cantidad INT, @MedioPago VARCHAR(15)
AS
BEGIN
    DECLARE @CuotaId INT, @Capital DECIMAL(12,2), @Interes DECIMAL(12,2),
            @Vence DATE, @PagoId INT, @n INT = 0;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT TOP (@Cantidad) Id, Capital, Interes, FechaVencimiento
        FROM Cuotas WHERE PrestamoId = @PrestamoId ORDER BY NumeroCuota;

    OPEN cur;
    FETCH NEXT FROM cur INTO @CuotaId, @Capital, @Interes, @Vence;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @n += 1;
        INSERT INTO Pagos (PrestamoId, FechaPago, Monto, MedioPago, NumeroOperacion, CajeroId)
        VALUES (@PrestamoId, DATEADD(HOUR, 10, CAST(DATEADD(DAY, -2, @Vence) AS DATETIME2(0))),
                @Capital + @Interes, @MedioPago,
                CASE WHEN @MedioPago = 'EFECTIVO' THEN NULL
                     ELSE CONCAT('OP', @PrestamoId, RIGHT('000' + CAST(@n AS VARCHAR(3)), 3)) END,
                4);
        SET @PagoId = SCOPE_IDENTITY();

        INSERT INTO PagoDetalles (PagoId, CuotaId, MontoInteres, MontoCapital)
        VALUES (@PagoId, @CuotaId, @Interes, @Capital);

        UPDATE Cuotas
        SET CapitalPagado = Capital, InteresPagado = Interes,
            Estado = 'PAGADA', FechaPagoTotal = DATEADD(DAY, -2, @Vence)
        WHERE Id = @CuotaId;

        FETCH NEXT FROM cur INTO @CuotaId, @Capital, @Interes, @Vence;
    END
    CLOSE cur;
    DEALLOCATE cur;
END
GO

EXEC #GenerarCronograma 1;  EXEC #PagarCuotas 1, 8, 'YAPE';
EXEC #GenerarCronograma 2;  EXEC #PagarCuotas 2, 3, 'TRANSFERENCIA';
EXEC #GenerarCronograma 3;  EXEC #PagarCuotas 3, 6, 'EFECTIVO';
EXEC #GenerarCronograma 4;  EXEC #PagarCuotas 4, 1, 'PLIN';
EXEC #GenerarCronograma 8;
GO

INSERT INTO HistorialEstadosPrestamo (PrestamoId, EstadoAnterior, EstadoNuevo, UsuarioId, Fecha, Comentario)
SELECT 3, 'DESEMBOLSADO', 'PAGADO', 4, MAX(FechaPagoTotal), N'Se pagaron todas las cuotas'
FROM Cuotas WHERE PrestamoId = 3;

DROP PROCEDURE #GenerarCronograma;
DROP PROCEDURE #PagarCuotas;
GO

-- Marca las cuotas vencidas y calcula su mora a la fecha de hoy
EXEC sp_ActualizarCuotasVencidas;
GO

PRINT 'Datos de prueba cargados correctamente.';
GO
