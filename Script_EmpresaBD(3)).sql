USE master;
GO

IF DB_ID('EmpresaBD') IS NOT NULL
    DROP DATABASE EmpresaBD;
GO

CREATE DATABASE EmpresaBD;
GO

USE EmpresaBD;
GO



-- =============================================
-- TABLA CARGO
-- =============================================

CREATE TABLE Cargo (
    IdCargo INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Sueldo DECIMAL(10,2) NOT NULL CHECK (Sueldo > 0),
    Activo BIT NOT NULL DEFAULT 1
);
GO

-- =============================================
-- TABLA EMPLEADO
-- =============================================

CREATE TABLE Empleado (
    IdEmpleado INT IDENTITY(1,1) PRIMARY KEY,
    IdCargo INT NOT NULL,
    NombreCompleto VARCHAR(70) NOT NULL,
    Dni CHAR(8) NOT NULL UNIQUE,
    Usuario VARCHAR(20) NOT NULL UNIQUE,
    Contrasena VARCHAR(200) NOT NULL,
    Telefono VARCHAR(9) NULL CHECK (Telefono LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO


-- =============================================
-- TABLA CATEGORIA
-- =============================================

CREATE TABLE Categoria (
    IdCategoria INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL UNIQUE,
    Activo BIT NOT NULL DEFAULT 1
);
GO

-- =============================================
-- TABLA CLIENTE
-- =============================================

CREATE TABLE Cliente (
    IdCliente INT IDENTITY(1,1) PRIMARY KEY,
    Dni CHAR(8) NULL UNIQUE
        CHECK (Dni IS NULL OR (LEN(Dni) = 8 AND Dni NOT LIKE '%[^0-9]%')),
    NombreCompleto VARCHAR(50) NOT NULL,
    Telefono VARCHAR(9) NULL CHECK (Telefono LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    FechaRegistro DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Activo BIT NOT NULL DEFAULT 1
);
GO

-- =============================================
-- TABLA MESA
-- =============================================

CREATE TABLE Mesa (
    IdMesa INT IDENTITY(1,1) PRIMARY KEY,
    Numero INT NOT NULL UNIQUE CHECK (Numero > 0),
    Estado VARCHAR(20) NOT NULL DEFAULT 'Libre' CHECK (Estado IN ('Libre','Ocupada','Reservada')),
    Activo BIT NOT NULL DEFAULT 1
);
GO

-- =============================================
-- TABLA PRODUCTO
-- =============================================

CREATE TABLE Producto (
    IdProducto INT IDENTITY(1,1) PRIMARY KEY,
    IdCategoria INT NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Precio DECIMAL(10,2) NOT NULL CHECK (Precio > 0),
    RutaImagen VARCHAR(500) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO


-- =============================================
-- TABLA CONFIGURACION SISTEMA
-- =============================================

CREATE TABLE ConfiguracionSistema (
    ConfigID INT PRIMARY KEY CHECK (ConfigID = 1) DEFAULT 1,
    NombreNegocio NVARCHAR(80) NOT NULL DEFAULT 'Chifa Percy',
    RUC CHAR(11) NOT NULL CHECK (RUC NOT LIKE '%[^0-9]%'),
    RazonSocial NVARCHAR(120) NOT NULL,
    IGV_Porcentaje DECIMAL(5,2) NOT NULL DEFAULT 18.00,
    Moneda CHAR(3) NOT NULL DEFAULT 'PEN' CHECK (Moneda IN ('PEN','USD')),
    NumeroYape VARCHAR(9) NULL CHECK (LEN(NumeroYape) = 9 AND NumeroYape LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    Correo NVARCHAR(100) NULL CHECK (Correo IS NULL OR (Correo LIKE '%@%' AND Correo LIKE '%.%'))
);
GO

-- =============================================
-- TABLA TURNOS CAJA
-- =============================================

CREATE TABLE TurnosCaja (
    IdTurno INT IDENTITY(1,1) PRIMARY KEY,
    IdEmpleado INT NOT NULL,
    FechaApertura DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaCierre DATETIME2 NULL,
    MontoInicial DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (MontoInicial >= 0),
    MontoCierre DECIMAL(10,2) NULL,
    Observaciones VARCHAR(200) NULL
);
GO

-- =============================================
-- TABLA PEDIDOS
-- =============================================

CREATE TABLE Pedidos (
    PedidoID INT IDENTITY(1,1) PRIMARY KEY,
    NumeroComanda AS ('C-' + RIGHT('0000' + CAST(PedidoID AS VARCHAR(4)), 4)) PERSISTED,
    TurnoID INT NOT NULL,
    ClienteID INT NULL,
    EmpleadoID INT NOT NULL,
    MesaID INT NULL,
    FechaHora DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    TipoServicio VARCHAR(20) NOT NULL CHECK (TipoServicio IN ('Mesa','ParaLlevar')),
    Estado VARCHAR(30) NOT NULL DEFAULT 'Pendiente' CHECK (Estado IN ('Pendiente','Pagado','Anulado')),
    Subtotal DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (Subtotal >= 0),
    IGV DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (IGV >= 0),
    Total DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (Total >= 0),
    NotasEspeciales VARCHAR(300) NULL
);
GO

-- =============================================
-- TABLA DETALLE PEDIDO
-- =============================================

CREATE TABLE DetallePedido (
    DetalleID INT IDENTITY(1,1) PRIMARY KEY,
    PedidoID INT NOT NULL,
    ProductoID INT NOT NULL,
    Cantidad INT NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario DECIMAL(10,2) NOT NULL CHECK (PrecioUnitario >= 0),
    Modificadores VARCHAR(120) NULL,
    Entregado BIT NOT NULL DEFAULT 0
);
GO

-- =============================================
-- TABLA PAGOS
-- =============================================

CREATE TABLE Pagos (
    PagoID INT IDENTITY(1,1) PRIMARY KEY,
    PedidoID INT NOT NULL,
    Metodo VARCHAR(30) NOT NULL CHECK (Metodo IN ('Efectivo','Yape')),
    Monto DECIMAL(10,2) NOT NULL CHECK (Monto > 0),
    Vuelto DECIMAL(10,2) DEFAULT 0 CHECK (Vuelto >= 0),
    Fecha DATETIME2 DEFAULT SYSDATETIME(),
    NroComprobante VARCHAR(50) NULL,
    Estado VARCHAR(20) DEFAULT 'Confirmado' CHECK (Estado IN ('Confirmado','Anulado')),
    QR_Ruta VARCHAR(200) NULL,
    QR_Bytes VARBINARY(MAX) NULL
);
GO

-- =============================================
-- TABLA AUDITORIA
-- =============================================

IF OBJECT_ID('AuditoriaSistema', 'U') IS NOT NULL DROP TABLE AuditoriaSistema;
GO

CREATE TABLE AuditoriaSistema (
    AuditoriaID INT IDENTITY(1,1) PRIMARY KEY,
    Tabla VARCHAR(50) NOT NULL,
    RegistroID INT NOT NULL,
    Accion VARCHAR(20) NOT NULL CHECK (Accion IN ('INSERT','UPDATE','DELETE','ANULAR','LOGIN')),
    EmpleadoID INT NULL,
    Fecha DATETIME2 DEFAULT SYSDATETIME(),
    Detalle VARCHAR(400) NULL
);
GO

-- =============================================
-- FOREIGN KEYS (Integridad Referencial)
-- =============================================

ALTER TABLE Empleado ADD CONSTRAINT FK_Empleado_Cargo
    FOREIGN KEY (IdCargo) REFERENCES Cargo(IdCargo);
GO

ALTER TABLE Producto ADD CONSTRAINT FK_Producto_Categoria
    FOREIGN KEY (IdCategoria) REFERENCES Categoria(IdCategoria);
GO

ALTER TABLE TurnosCaja ADD CONSTRAINT FK_TurnosCaja_Empleado
    FOREIGN KEY (IdEmpleado) REFERENCES Empleado(IdEmpleado);
GO

ALTER TABLE Pedidos ADD CONSTRAINT FK_Pedidos_TurnosCaja
    FOREIGN KEY (TurnoID) REFERENCES TurnosCaja(IdTurno);
GO

ALTER TABLE Pedidos ADD CONSTRAINT FK_Pedidos_Cliente
    FOREIGN KEY (ClienteID) REFERENCES Cliente(IdCliente) ON DELETE SET NULL;
GO

ALTER TABLE Pedidos ADD CONSTRAINT FK_Pedidos_Empleado
    FOREIGN KEY (EmpleadoID) REFERENCES Empleado(IdEmpleado);
GO

ALTER TABLE Pedidos ADD CONSTRAINT FK_Pedidos_Mesa
    FOREIGN KEY (MesaID) REFERENCES Mesa(IdMesa) ON DELETE SET NULL;
GO

ALTER TABLE DetallePedido ADD CONSTRAINT FK_DetallePedido_Pedido
    FOREIGN KEY (PedidoID) REFERENCES Pedidos(PedidoID) ON DELETE CASCADE;
GO

ALTER TABLE DetallePedido ADD CONSTRAINT FK_DetallePedido_Producto
    FOREIGN KEY (ProductoID) REFERENCES Producto(IdProducto);
GO

ALTER TABLE Pagos ADD CONSTRAINT FK_Pagos_Pedido
    FOREIGN KEY (PedidoID) REFERENCES Pedidos(PedidoID);
GO

-- =============================================
-- DATOS INICIALES
-- =============================================

INSERT INTO Cargo (Nombre, Sueldo, Activo)
VALUES ('Admin', 1500.00, 1),
       ('Mozo', 1100.00, 1),
       ('Cajero', 1000.00, 1);
GO

INSERT INTO Categoria (Nombre, Activo)
VALUES ('Entradas', 1),
       ('Sopas', 1),
       ('Tallarines', 1),
       ('Bebidas', 1),
       ('Bocadillos', 0);
GO

INSERT INTO ConfiguracionSistema (NombreNegocio, RUC, RazonSocial, IGV_Porcentaje, Moneda, NumeroYape, Correo)
SELECT 'Chifa Percy', '20601234567', 'Chifa Percy EIRL', 18.00, 'PEN', '987654321', 'ventas@chifapercy.com'
WHERE NOT EXISTS (SELECT 1 FROM ConfiguracionSistema);
GO

-- =============================================
-- DATOS DE PRUEBA
-- =============================================


-- 1. Empleados (6 registros)
INSERT INTO Empleado (IdCargo, NombreCompleto, Dni, Usuario, Contrasena, Telefono, Activo)
VALUES 
    (1, 'Carlos', '74125896', 'admin', 'admin', '987654001',  1),
    (3, 'Ana', '85236974', 'caja01', 'caja123', '987654002',  1),
    (2, 'Luis', '96347085', 'mozo01', 'mozo123', '987654003',  1),
    (2, 'Maria', '14725836', 'mozo02', 'mozo123', '987654004',  1),
    (2, 'Pedro', '25836914', 'mozo03', 'mozo123', '987654005',  1);
GO


-- 2. Clientes (6 registros)
INSERT INTO Cliente (Dni, NombreCompleto, Telefono, Activo)
VALUES 
    ('60945678', 'Juan Perez Gomez', '987654321', 1),
    ('87654321', 'Maria Lopez Rojas', '987654322', 1),
    ('45678912', 'Carlos Silva Torres', '987654323', 1),
    ('78912345', 'Ana Castillo Flores', '987654324', 1),
    ('32165498', 'Pedro Ramos Diaz', '987654325', 1),
    ('65498732', 'Luis Fernandez Castro', '987654326', 0);
GO

-- 3. Mesas (10 registros)
INSERT INTO Mesa (Numero, Estado, Activo)
VALUES 
    (1, 'Libre', 1), (2, 'Libre', 1), (3, 'Libre', 1), (4, 'Libre', 1), (5, 'Libre', 1),
    (6, 'Libre', 1), (7, 'Libre', 1), (8, 'Libre', 1), (9, 'Libre', 1), (10, 'Libre', 1);
GO

-- 4. Productos (12 registros)
INSERT INTO Producto (IdCategoria, Nombre, Precio, Activo)
VALUES
    -- Tallarines
    (3, 'Tallarin Saltado de Pollo', 30.00, 1),
    (3, 'Tallarin Saltado de Carne', 32.00, 1),

    -- Entradas
    (1, 'Wantan Frito', 12.00, 1),
    (1, 'Siu Mai', 14.00, 1),
    (1, 'Langostinos Fritos', 18.00, 1),

    -- Sopas
    (2, 'Sopa Wantan', 15.00, 1),
    (2, 'Sopa de Pollo', 14.00, 1),

    -- Bebidas
    (4, 'Chicha Morada 1L', 10.00, 1),
    (4, 'Inca Kola 500 ml', 6.00, 1),
    (4, 'Limonada 1L', 8.00, 1);


-- 5. TurnosCaja (1 registro)
INSERT INTO TurnosCaja (IdEmpleado, FechaApertura, MontoInicial)
VALUES (2, GETDATE(), 200.00);
GO

-- 6. Pedidos (20 registros)
INSERT INTO Pedidos
(TurnoID, ClienteID, EmpleadoID, MesaID, FechaHora, TipoServicio, Estado, Subtotal, IGV, Total)
VALUES

-- 26/06
(1,1,3,3,'2026-06-26 12:15:00','Mesa','Pagado',112.00,20.16,132.16),
(1,2,3,1,'2026-06-26 13:40:00','Mesa','Pagado',85.50,15.39,100.89),
(1,3,4,NULL,'2026-06-26 19:20:00','ParaLlevar','Pagado',58.00,10.44,68.44),

-- 27/06
(1,4,5,5,'2026-06-27 12:50:00','Mesa','Pagado',210.00,37.80,247.80),
(1,5,3,7,'2026-06-27 20:10:00','Mesa','Pagado',95.00,17.10,112.10),
(1,6,4,NULL,'2026-06-27 21:05:00','ParaLlevar','Pagado',45.00,8.10,53.10),

-- 28/06
(1,1,3,2,'2026-06-28 11:40:00','Mesa','Pagado',120.00,21.60,141.60),
(1,2,5,4,'2026-06-28 13:25:00','Mesa','Pagado',65.00,11.70,76.70),
(1,4,4,NULL,'2026-06-28 18:30:00','ParaLlevar','Pagado',80.00,14.40,94.40),

-- 29/06
(1,3,3,6,'2026-06-29 12:20:00','Mesa','Pagado',150.00,27.00,177.00),
(1,5,5,8,'2026-06-29 20:00:00','Mesa','Pagado',55.00,9.90,64.90),
(1,6,3,3,'2026-06-29 21:15:00','Mesa','Pendiente',132.00,23.76,155.76),

-- 30/06
(1,1,4,8,'2026-06-30 14:10:00','Mesa','Pendiente',45.00,8.10,53.10),
(1,2,5,NULL,'2026-06-30 19:30:00','ParaLlevar','Pendiente',68.44,12.32,80.76),
(1,4,3,6,'2026-06-30 20:20:00','Mesa','Pagado',120.00,21.60,141.60),

-- 01/07
(1,5,4,4,'2026-07-01 13:30:00','Mesa','Pagado',55.00,9.90,64.90),
(1,3,5,9,'2026-07-01 18:10:00','Mesa','Anulado',66.08,11.89,77.97),
(1,6,3,NULL,'2026-07-01 20:00:00','ParaLlevar','Pagado',42.00,7.56,49.56),

-- 02/07
(1,1,4,10,'2026-07-02 12:45:00','Mesa','Pagado',90.00,16.20,106.20),
(1,2,5,5,'2026-07-02 20:30:00','Mesa','Pagado',38.00,6.84,44.84);

-- 7. DetallePedido (52 registros)
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(1,1,2,30.00),(1,3,1,12.00),(1,9,3,6.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(2,1,1,30.00),(2,7,1,14.00),(2,8,1,10.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(3,4,2,14.00),(3,9,1,6.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(4,1,3,30.00),(4,3,2,12.00),(4,8,3,10.00),(4,9,5,6.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(5,5,2,18.00),(5,8,2,10.00),(5,10,2,8.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(6,2,1,32.00),(6,6,1,15.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(7,1,2,30.00),(7,8,1,10.00),(7,9,1,6.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(8,3,1,12.00),(8,7,1,14.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(9,1,2,30.00),(9,10,2,8.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(10,1,3,30.00),(10,3,2,12.00),(10,9,4,6.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(11,2,1,32.00),(11,5,2,18.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(12,1,2,30.00),(12,3,1,12.00),(12,9,3,6.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(13,9,1,6.00),(13,2,1,32.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(14,4,2,14.00),(14,9,1,6.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(15,1,3,30.00),(15,8,2,10.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(16,5,2,18.00),(16,6,1,15.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(17,5,2,18.00),(17,8,2,10.00),(17,10,2,8.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(18,2,1,32.00),(18,6,1,15.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(19,1,2,30.00),(19,8,1,10.00);
INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES
(20,9,1,6.00),(20,10,2,8.00);
GO

-- 8. Pagos (11 registros)
INSERT INTO Pagos (PedidoID, Metodo, Monto, Vuelto, NroComprobante, Estado)
VALUES 
    (1, 'Efectivo', 132.16, 0, 'EFEC-20260616-001', 'Confirmado'),
    (2, 'Yape', 100.89, 0, 'YAPE-20260616-002', 'Confirmado'),
    (3, 'Efectivo', 68.44, 0, 'EFEC-20260616-003', 'Confirmado'),
    (4, 'Yape', 247.80, 0, 'YAPE-20260616-004', 'Confirmado'),
    (5, 'Efectivo', 112.10, 0, 'EFEC-20260616-005', 'Confirmado'),
    (6, 'Yape', 53.10, 0, 'YAPE-20260616-006', 'Confirmado'),
    (7, 'Efectivo', 141.60, 0, 'EFEC-20260616-007', 'Confirmado'),
    (8, 'Efectivo', 76.70, 0, 'EFEC-20260616-008', 'Confirmado'),
    (9, 'Yape', 94.40, 0, 'YAPE-20260616-009', 'Confirmado'),
    (10, 'Efectivo', 177.00, 0, 'EFEC-20260616-010', 'Confirmado'),
    (11, 'Yape', 64.90, 0, 'YAPE-20260616-011', 'Confirmado');
GO

-- 9. Actualizar Mesas Ocupadas (con pedidos pendientes)
UPDATE Mesa SET Estado = 'Ocupada' WHERE IdMesa IN (3, 8, 6, 4);
GO

-- 10. Registrar Auditoría (20 registros)
INSERT INTO AuditoriaSistema (Tabla, RegistroID, Accion, EmpleadoID, Detalle, Fecha)
VALUES 
    -- INSERTS (creación de registros)
    ('Producto', 13, 'INSERT', 1, 'Se creó nuevo producto: "Sopa Wantan Especial"', DATEADD(hour, -8, GETDATE())),
    ('Producto', 14, 'INSERT', 1, 'Se creó nuevo producto: "Arroz Chaufa Mixto"', DATEADD(hour, -7.5, GETDATE())),
    ('Cliente', 7, 'INSERT', 2, 'Se registró nuevo cliente: "Roberto Sánchez"', DATEADD(hour, -6, GETDATE())),
    ('Empleado', 7, 'INSERT', 1, 'Se registró nuevo empleado: "Laura Torres"', DATEADD(hour, -5.5, GETDATE())),
    ('Categoria', 7, 'INSERT', 1, 'Se creó nueva categoría: "Postres"', DATEADD(hour, -5, GETDATE())),
    
    -- UPDATES (modificaciones de registros)
    ('Producto', 1, 'UPDATE', 1, 'Se actualizó precio de "Arroz Chaufa Especial" de 30.00 a 32.00', DATEADD(hour, -4, GETDATE())),
    ('Producto', 3, 'UPDATE', 1, 'Se actualizó precio de "Tallarin Saltado" de 28.00 a 30.00', DATEADD(hour, -3.5, GETDATE())),
    ('Cliente', 1, 'UPDATE', 2, 'Se actualizó teléfono de cliente Juan Pérez a 987654330', DATEADD(hour, -3, GETDATE())),
    ('Empleado', 3, 'UPDATE', 1, 'Se actualizó email de Luis Mozo a luis.mozo@chifa.com', DATEADD(hour, -2.5, GETDATE())),
    ('Producto', 11, 'UPDATE', 1, 'Se actualizó precio de "Inca Kola 500ml" de 5.00 a 6.00', DATEADD(hour, -2, GETDATE())),
    ('Mesa', 1, 'UPDATE', 3, 'Se cambió estado de Mesa #1 de "Libre" a "Ocupada"', DATEADD(hour, -1.5, GETDATE())),
    ('Producto', 7, 'UPDATE', 1, 'Se desactivó producto "Langosta Saltada" (Activo=0)', DATEADD(hour, -1, GETDATE())),
    
    -- DELETES (eliminaciones lógicas - desactivaciones)
    ('Producto', 5, 'DELETE', 1, 'Se eliminó producto "Wantan Frito" (desactivado)', DATEADD(hour, -0.5, GETDATE())),
    ('Cliente', 6, 'DELETE', 2, 'Se eliminó cliente Luis Fernández (desactivado)', DATEADD(hour, -0.25, GETDATE())),
    
    -- ANULACIONES (anulación de pedidos y pagos)
    ('Pedidos', 17, 'ANULAR', 2, 'Se anuló pedido #17 por error en la orden', DATEADD(hour, -8, GETDATE())),
    ('Pagos', 12, 'ANULAR', 2, 'Se anuló pago asociado al pedido #17', DATEADD(hour, -8, GETDATE())),
    ('Pedidos', 18, 'ANULAR', 3, 'Se anuló pedido #18 por cambio de decisión del cliente', DATEADD(hour, -7.5, GETDATE())),
    ('Pedidos', 19, 'ANULAR', 4, 'Se anuló pedido #19 por falta de insumos', DATEADD(hour, -6.5, GETDATE())),
    ('Pedidos', 20, 'ANULAR', 5, 'Se anuló pedido #20 por duplicidad', DATEADD(hour, -4, GETDATE()));
GO

-- =============================================
-- VERIFICAR DATOS
-- =============================================

SELECT * FROM ConfiguracionSistema;
GO

