SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* =============================================================
   PrestaFácil — Esquema de base de datos
   Motor: SQL Server 2019+
   Ejecutar en SSMS o con:  sqlcmd -S .\SQLEXPRESS -E -i 01_schema.sql
   ============================================================= */

IF DB_ID('PrestaFacilDB') IS NOT NULL
BEGIN
    ALTER DATABASE PrestaFacilDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PrestaFacilDB;
END
GO

CREATE DATABASE PrestaFacilDB;
GO

USE PrestaFacilDB;
GO

/* -------------------------------------------------------------
   1. Usuarios del sistema (RF-19 a RF-21)
   ------------------------------------------------------------- */
CREATE TABLE Usuarios (
    Id              INT IDENTITY(1,1)   NOT NULL,
    NombreUsuario   VARCHAR(50)         NOT NULL,
    NombreCompleto  NVARCHAR(150)       NOT NULL,
    Email           VARCHAR(150)        NOT NULL,
    PasswordHash    VARCHAR(255)        NOT NULL,
    Rol             VARCHAR(10)         NOT NULL,
    Activo          BIT                 NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
    FechaCreacion   DATETIME2(0)        NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Usuarios PRIMARY KEY (Id),
    CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE (NombreUsuario),
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
    CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN ('ADMIN', 'ASESOR', 'CAJERO'))
);
GO

/* -------------------------------------------------------------
   2. Clientes (RF-01 a RF-04, RN-01)
   ------------------------------------------------------------- */
CREATE TABLE Clientes (
    Id               INT IDENTITY(1,1)  NOT NULL,
    Dni              CHAR(8)            NOT NULL,
    Nombres          NVARCHAR(100)      NOT NULL,
    Apellidos        NVARCHAR(100)      NOT NULL,
    FechaNacimiento  DATE               NOT NULL,
    Telefono         VARCHAR(15)        NULL,
    Email            VARCHAR(150)       NULL,
    Direccion        NVARCHAR(200)      NULL,
    IngresoMensual   DECIMAL(12,2)      NOT NULL,
    GastosMensuales  DECIMAL(12,2)      NOT NULL CONSTRAINT DF_Clientes_Gastos DEFAULT (0),
    Activo           BIT                NOT NULL CONSTRAINT DF_Clientes_Activo DEFAULT (1),
    FechaCreacion    DATETIME2(0)       NOT NULL CONSTRAINT DF_Clientes_FechaCreacion DEFAULT (SYSDATETIME()),
    CreadoPor        INT                NOT NULL,

    CONSTRAINT PK_Clientes PRIMARY KEY (Id),
    CONSTRAINT UQ_Clientes_Dni UNIQUE (Dni),
    CONSTRAINT FK_Clientes_Usuarios FOREIGN KEY (CreadoPor) REFERENCES Usuarios (Id),
    CONSTRAINT CK_Clientes_Dni CHECK (Dni LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT CK_Clientes_Ingreso CHECK (IngresoMensual > 0),
    CONSTRAINT CK_Clientes_Gastos CHECK (GastosMensuales >= 0)
);
GO

CREATE INDEX IX_Clientes_Apellidos_Nombres ON Clientes (Apellidos, Nombres);
GO

/* -------------------------------------------------------------
   3. Productos de préstamo (RF-05)
   Tasas guardadas como fracción: 0.30 = 30 %
   ------------------------------------------------------------- */
CREATE TABLE ProductosPrestamo (
    Id                INT IDENTITY(1,1)  NOT NULL,
    Nombre            NVARCHAR(100)      NOT NULL,
    Descripcion       NVARCHAR(300)      NULL,
    Tea               DECIMAL(9,6)       NOT NULL,
    MontoMinimo       DECIMAL(12,2)      NOT NULL,
    MontoMaximo       DECIMAL(12,2)      NOT NULL,
    PlazoMinimoMeses  TINYINT            NOT NULL,
    PlazoMaximoMeses  TINYINT            NOT NULL,
    TasaMoraDiaria    DECIMAL(9,6)       NOT NULL,
    Activo            BIT                NOT NULL CONSTRAINT DF_Productos_Activo DEFAULT (1),

    CONSTRAINT PK_ProductosPrestamo PRIMARY KEY (Id),
    CONSTRAINT UQ_ProductosPrestamo_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_Productos_Tea CHECK (Tea > 0 AND Tea < 2),
    CONSTRAINT CK_Productos_Montos CHECK (MontoMinimo > 0 AND MontoMaximo >= MontoMinimo),
    CONSTRAINT CK_Productos_Plazos CHECK (PlazoMinimoMeses > 0 AND PlazoMaximoMeses >= PlazoMinimoMeses),
    CONSTRAINT CK_Productos_Mora CHECK (TasaMoraDiaria >= 0 AND TasaMoraDiaria < 0.01)
);
GO

/* -------------------------------------------------------------
   4. Préstamos (RF-06 a RF-11, RN-03)
   La TEA y la tasa de mora se copian del producto para que un
   cambio posterior no afecte a los préstamos ya creados.
   ------------------------------------------------------------- */
CREATE TABLE Prestamos (
    Id                INT IDENTITY(1,1)  NOT NULL,
    Codigo            AS ('PR-' + RIGHT('000000' + CAST(Id AS VARCHAR(10)), 6)) PERSISTED,
    ClienteId         INT                NOT NULL,
    ProductoId        INT                NOT NULL,
    Monto             DECIMAL(12,2)      NOT NULL,
    PlazoMeses        TINYINT            NOT NULL,
    Tea               DECIMAL(9,6)       NOT NULL,
    Tem               DECIMAL(12,10)     NOT NULL,
    CuotaMensual      DECIMAL(12,2)      NOT NULL,
    TasaMoraDiaria    DECIMAL(9,6)       NOT NULL,
    Estado           VARCHAR(12)        NOT NULL CONSTRAINT DF_Prestamos_Estado DEFAULT ('PENDIENTE'),
    AsesorId          INT                NOT NULL,
    FechaSolicitud    DATETIME2(0)       NOT NULL CONSTRAINT DF_Prestamos_FechaSolicitud DEFAULT (SYSDATETIME()),
    AprobadoPorId     INT                NULL,
    FechaAprobacion   DATETIME2(0)       NULL,
    MotivoRechazo     NVARCHAR(300)      NULL,
    DesembolsadoPorId INT                NULL,
    FechaDesembolso   DATE               NULL,

    CONSTRAINT PK_Prestamos PRIMARY KEY (Id),
    CONSTRAINT UQ_Prestamos_Codigo UNIQUE (Codigo),
    CONSTRAINT FK_Prestamos_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes (Id),
    CONSTRAINT FK_Prestamos_Productos FOREIGN KEY (ProductoId) REFERENCES ProductosPrestamo (Id),
    CONSTRAINT FK_Prestamos_Asesor FOREIGN KEY (AsesorId) REFERENCES Usuarios (Id),
    CONSTRAINT FK_Prestamos_AprobadoPor FOREIGN KEY (AprobadoPorId) REFERENCES Usuarios (Id),
    CONSTRAINT FK_Prestamos_DesembolsadoPor FOREIGN KEY (DesembolsadoPorId) REFERENCES Usuarios (Id),
    CONSTRAINT CK_Prestamos_Monto CHECK (Monto > 0),
    CONSTRAINT CK_Prestamos_Plazo CHECK (PlazoMeses > 0),
    CONSTRAINT CK_Prestamos_Estado CHECK (Estado IN ('PENDIENTE', 'APROBADO', 'RECHAZADO', 'DESEMBOLSADO', 'PAGADO')),
    -- Un rechazo siempre lleva motivo
    CONSTRAINT CK_Prestamos_MotivoRechazo CHECK (Estado <> 'RECHAZADO' OR MotivoRechazo IS NOT NULL),
    -- Un préstamo desembolsado o pagado siempre tiene fecha de desembolso
    CONSTRAINT CK_Prestamos_Desembolso CHECK (Estado NOT IN ('DESEMBOLSADO', 'PAGADO') OR FechaDesembolso IS NOT NULL)
);
GO

CREATE INDEX IX_Prestamos_Cliente_Estado ON Prestamos (ClienteId, Estado);
CREATE INDEX IX_Prestamos_Estado ON Prestamos (Estado);
GO

/* -------------------------------------------------------------
   5. Cronograma de cuotas (RF-10, RF-11, RN-08)
   Cada cuota guarda lo que debe y lo que ya se pagó por concepto.
   ------------------------------------------------------------- */
CREATE TABLE Cuotas (
    Id                INT IDENTITY(1,1)  NOT NULL,
    PrestamoId        INT                NOT NULL,
    NumeroCuota       TINYINT            NOT NULL,
    FechaVencimiento  DATE               NOT NULL,
    SaldoInicial      DECIMAL(12,2)      NOT NULL,
    Capital           DECIMAL(12,2)      NOT NULL,
    Interes           DECIMAL(12,2)      NOT NULL,
    MontoCuota        AS (Capital + Interes) PERSISTED,
    Mora              DECIMAL(12,2)      NOT NULL CONSTRAINT DF_Cuotas_Mora DEFAULT (0),
    CapitalPagado     DECIMAL(12,2)      NOT NULL CONSTRAINT DF_Cuotas_CapitalPagado DEFAULT (0),
    InteresPagado     DECIMAL(12,2)      NOT NULL CONSTRAINT DF_Cuotas_InteresPagado DEFAULT (0),
    MoraPagada        DECIMAL(12,2)      NOT NULL CONSTRAINT DF_Cuotas_MoraPagada DEFAULT (0),
    Pendiente         AS (Capital + Interes + Mora - CapitalPagado - InteresPagado - MoraPagada) PERSISTED,
    Estado            VARCHAR(10)        NOT NULL CONSTRAINT DF_Cuotas_Estado DEFAULT ('PENDIENTE'),
    FechaPagoTotal    DATE               NULL,

    CONSTRAINT PK_Cuotas PRIMARY KEY (Id),
    CONSTRAINT FK_Cuotas_Prestamos FOREIGN KEY (PrestamoId) REFERENCES Prestamos (Id),
    CONSTRAINT UQ_Cuotas_Prestamo_Numero UNIQUE (PrestamoId, NumeroCuota),
    CONSTRAINT CK_Cuotas_Estado CHECK (Estado IN ('PENDIENTE', 'PARCIAL', 'VENCIDA', 'PAGADA')),
    CONSTRAINT CK_Cuotas_Montos CHECK (Capital >= 0 AND Interes >= 0 AND Mora >= 0),
    -- Nunca se paga más de lo que se debe en cada concepto
    CONSTRAINT CK_Cuotas_Pagos CHECK (
        CapitalPagado BETWEEN 0 AND Capital
        AND InteresPagado BETWEEN 0 AND Interes
        AND MoraPagada BETWEEN 0 AND Mora
    )
);
GO

CREATE INDEX IX_Cuotas_Estado_Vencimiento ON Cuotas (Estado, FechaVencimiento) INCLUDE (PrestamoId);
GO

/* -------------------------------------------------------------
   6. Pagos y su distribución por cuota (RF-12, RF-13, RN-09 a RN-11)
   ------------------------------------------------------------- */
CREATE TABLE Pagos (
    Id                INT IDENTITY(1,1)  NOT NULL,
    PrestamoId        INT                NOT NULL,
    FechaPago         DATETIME2(0)       NOT NULL CONSTRAINT DF_Pagos_FechaPago DEFAULT (SYSDATETIME()),
    Monto             DECIMAL(12,2)      NOT NULL,
    MedioPago         VARCHAR(15)        NOT NULL,
    NumeroOperacion   VARCHAR(50)        NULL,
    CajeroId          INT                NOT NULL,
    Anulado           BIT                NOT NULL CONSTRAINT DF_Pagos_Anulado DEFAULT (0),
    MotivoAnulacion   NVARCHAR(300)      NULL,
    AnuladoPorId      INT                NULL,
    FechaAnulacion    DATETIME2(0)       NULL,

    CONSTRAINT PK_Pagos PRIMARY KEY (Id),
    CONSTRAINT FK_Pagos_Prestamos FOREIGN KEY (PrestamoId) REFERENCES Prestamos (Id),
    CONSTRAINT FK_Pagos_Cajero FOREIGN KEY (CajeroId) REFERENCES Usuarios (Id),
    CONSTRAINT FK_Pagos_AnuladoPor FOREIGN KEY (AnuladoPorId) REFERENCES Usuarios (Id),
    CONSTRAINT CK_Pagos_Monto CHECK (Monto > 0),
    CONSTRAINT CK_Pagos_MedioPago CHECK (MedioPago IN ('EFECTIVO', 'TRANSFERENCIA', 'YAPE', 'PLIN')),
    -- Los pagos digitales siempre tienen número de operación
    CONSTRAINT CK_Pagos_NumeroOperacion CHECK (MedioPago = 'EFECTIVO' OR NumeroOperacion IS NOT NULL),
    CONSTRAINT CK_Pagos_Anulacion CHECK (Anulado = 0 OR (MotivoAnulacion IS NOT NULL AND AnuladoPorId IS NOT NULL))
);
GO

CREATE INDEX IX_Pagos_Prestamo ON Pagos (PrestamoId);
CREATE INDEX IX_Pagos_FechaPago ON Pagos (FechaPago) INCLUDE (Monto, MedioPago, CajeroId, Anulado);
GO

CREATE TABLE PagoDetalles (
    Id            INT IDENTITY(1,1)  NOT NULL,
    PagoId        INT                NOT NULL,
    CuotaId       INT                NOT NULL,
    MontoMora     DECIMAL(12,2)      NOT NULL CONSTRAINT DF_PagoDetalles_Mora DEFAULT (0),
    MontoInteres  DECIMAL(12,2)      NOT NULL CONSTRAINT DF_PagoDetalles_Interes DEFAULT (0),
    MontoCapital  DECIMAL(12,2)      NOT NULL CONSTRAINT DF_PagoDetalles_Capital DEFAULT (0),

    CONSTRAINT PK_PagoDetalles PRIMARY KEY (Id),
    CONSTRAINT FK_PagoDetalles_Pagos FOREIGN KEY (PagoId) REFERENCES Pagos (Id),
    CONSTRAINT FK_PagoDetalles_Cuotas FOREIGN KEY (CuotaId) REFERENCES Cuotas (Id),
    CONSTRAINT UQ_PagoDetalles_Pago_Cuota UNIQUE (PagoId, CuotaId),
    CONSTRAINT CK_PagoDetalles_Montos CHECK (
        MontoMora >= 0 AND MontoInteres >= 0 AND MontoCapital >= 0
        AND MontoMora + MontoInteres + MontoCapital > 0
    )
);
GO

/* -------------------------------------------------------------
   7. Historial de estados del préstamo (RN-12)
   ------------------------------------------------------------- */
CREATE TABLE HistorialEstadosPrestamo (
    Id              INT IDENTITY(1,1)  NOT NULL,
    PrestamoId      INT                NOT NULL,
    EstadoAnterior  VARCHAR(12)        NULL,
    EstadoNuevo     VARCHAR(12)        NOT NULL,
    UsuarioId       INT                NOT NULL,
    Fecha           DATETIME2(0)       NOT NULL CONSTRAINT DF_Historial_Fecha DEFAULT (SYSDATETIME()),
    Comentario      NVARCHAR(300)      NULL,

    CONSTRAINT PK_HistorialEstadosPrestamo PRIMARY KEY (Id),
    CONSTRAINT FK_Historial_Prestamos FOREIGN KEY (PrestamoId) REFERENCES Prestamos (Id),
    CONSTRAINT FK_Historial_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios (Id)
);
GO

CREATE INDEX IX_Historial_Prestamo ON HistorialEstadosPrestamo (PrestamoId, Fecha);
GO

PRINT 'Esquema PrestaFacilDB creado correctamente.';
GO
