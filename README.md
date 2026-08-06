# SWAP - Sistema Web de Gestión de Pedidos para Restaurante

Sistema web desarrollado con ASP.NET Core MVC para la gestión de pedidos, productos, clientes, mesas y pagos de un restaurante.

> Proyecto académico desarrollado en equipo y publicado como parte de mi portafolio profesional.

> **Nota:** El proyecto se desarrolló a partir de una base académica denominada `SOL_Panaderia`. Por compatibilidad con la solución y la estructura del código, se conservaron algunos nombres internos. Sin embargo, la funcionalidad implementada corresponde al sistema **SWAP**, orientado a la gestión de un restaurante.

## Objetivo del proyecto

Este proyecto fue desarrollado con el propósito de aplicar conceptos de:

- Arquitectura MVC.
- Programación orientada a objetos en C#.
- Acceso a datos mediante ADO.NET.
- Diseño e implementación de bases de datos en SQL Server.
- Gestión de pedidos y pagos en un entorno de restaurante.
- Trabajo colaborativo en un proyecto académico.

## Tecnologías

- ASP.NET Core MVC
- C#
- SQL Server
- ADO.NET
- Bootstrap 5
- MailKit
- SweetAlert2
- Git y GitHub

## Funcionalidades

- Gestión de pedidos
- Gestión de productos
- Gestión de clientes
- Gestión de mesas
- Pagos en efectivo y Yape
- Generación de QR
- Dashboard administrativo
- Auditoría del sistema

## Tabla de Contenidos

1. [Tecnologías](#tecnologías)
2. [Estructura del Proyecto](#estructura-del-proyecto)
3. [Base de Datos](#base-de-datos)
4. [Arquitectura y Patrón de Diseño](#arquitectura-y-patrón-de-diseño)
5. [Configuración y Ejecución](#configuración-y-ejecución)
6. [Módulos del Sistema](#módulos-del-sistema)
7. [Validaciones](#validaciones)
8. [Frontend](#frontend)
9. [Consideraciones Técnicas](#consideraciones-técnicas)

---

## Tecnologías

| Categoría                  | Tecnología                              | Versión   |
| -------------------------- | --------------------------------------- | --------- |
| **Framework**              | ASP.NET Core MVC                        | .NET 10.0 |
| **Lenguaje**               | C#                                      | 12+       |
| **Base de datos**          | SQL Server Express                      | -         |
| **Acceso a datos**         | Microsoft.Data.SqlClient (ADO.NET puro) | 7.0.1     |
| **Frontend**               | HTML5, CSS3, JavaScript                 | -         |
| **CSS Framework**          | Bootstrap                               | 5.x       |
| **Iconos**                 | Bootstrap Icons                         | 1.11.3    |
| **Alertas**                | SweetAlert2                             | 11.x      |
| **Validación client-side** | jQuery Validation + Unobtrusive         | -         |
| **Generación de QR**       | QRCoder                                 | 1.4.3     |
| **Envío de correos**       | MailKit                                 | 4.17.0    |

> **Nota:** No se utiliza Entity Framework Core ni ningún ORM. Todo el acceso a datos es ADO.NET puro con `SqlConnection`, `SqlCommand` y `SqlDataReader`.

---

## Estructura del Proyecto

```
SOL_Panaderia/
├── SOL_Panaderia.slnx                        # Archivo de solución
├── README.md                                 # Documentación
└── PRJ_Panaderia/
    ├── Program.cs                            # Punto de entrada y configuración
    ├── appsettings.json                      # Cadena de conexión y configuración
    ├── PRJ_Panaderia.csproj                  # Archivo de proyecto (.NET 10)
    │
    ├── Models/                               # Modelos de dominio
    │   ├── Cargo.cs
    │   ├── Empleado.cs
    │   ├── Categoria.cs
    │   ├── Cliente.cs
    │   ├── Mesa.cs
    │   ├── Producto.cs
    │   ├── Pedido.cs
    │   ├── DetallePedido.cs
    │   ├── Pago.cs
    │   ├── TurnoCaja.cs
    │   ├── ConfiguracionSistema.cs
    │   ├── Auditoria.cs
    │   ├── ErrorViewModel.cs
    │   └── ViewModels/
    │       ├── DashboardViewModel.cs
    │       └── PedidoViewModel.cs
    │
    ├── Data/                                 # Repositorios (capa de acceso a datos)
    │   ├── CargoRepository.cs
    │   ├── EmpleadoRepository.cs
    │   ├── CategoriaRepository.cs
    │   ├── ClienteRepository.cs
    │   ├── MesaRepository.cs
    │   ├── ProductoRepository.cs
    │   ├── PedidoRepository.cs
    │   ├── DetallePedidoRepository.cs
    │   ├── PagoRepository.cs
    │   ├── TurnoCajaRepository.cs
    │   ├── ConfiguracionSistemaRepository.cs
    │   ├── AuditoriaRepository.cs
    │   └── DashboardRepository.cs
    │
    ├── Controllers/                          # Controladores MVC
    │   ├── HomeController.cs                  # Dashboard principal
    │   ├── LoginController.cs                 # Autenticación
    │   ├── CargoController.cs                 # Sueldo editable (sin CRUD completo)
    │   ├── EmpleadoController.cs
    │   ├── CategoriaController.cs
    │   ├── ClienteController.cs
    │   ├── MesaController.cs
    │   ├── ProductoController.cs
    │   ├── PedidoController.cs               # POS / Pedidos
    │   ├── DetallePedidoController.cs         # Detalle de pedidos
    │   ├── PagoController.cs                  # Pagos + envío de email Yape
    │   ├── TurnoCajaController.cs
    │   ├── ConfiguracionController.cs
    │   └── AuditoriaController.cs            # Auditoría del sistema
    │
    ├── Services/                              # Servicios externos
    │   ├── SmtpSettings.cs                    # Configuración SMTP (POCO)
    │   └── EmailService.cs                    # Envío de correos vía MailKit
    │
    ├── Views/                                # Vistas Razor
    │   ├── Shared/
    │   │   ├── _Layout.cshtml                # Layout principal con sidebar
    │   │   ├── _LayoutLogin.cshtml           # Layout para login
    │   │   ├── _ValidationScriptsPartial.cshtml
    │   │   └── Error.cshtml
    │   ├── Home/
    │   │   └── Index.cshtml                  # Dashboard con métricas
    │   ├── Login/
    │   │   └── Index.cshtml                  # Formulario de login
    │   ├── Cargo/        (Index solo lectura)
    │   ├── Empleado/     (Index, Create, Edit)
    │   ├── Categoria/    (Index, Create, Edit)
    │   ├── Cliente/      (Index, Create, Edit)
    │   ├── Mesa/         (Index, Create, Edit)
    │   ├── Producto/     (Index, Create, Edit)
    │   ├── Pedido/
    │   │   └── Index.cshtml                  # Interfaz POS completa
    │   ├── DetallePedido/
    │   │   ├── Index.cshtml
    │   │   ├── Detalle.cshtml
    │   │   └── Impresion.cshtml
    │   ├── Pago/
    │   │   ├── Listado.cshtml
    │   │   ├── Registrar.cshtml
    │   │   ├── Comprobante.cshtml
    │   │   └── Historial.cshtml
    │   ├── TurnoCaja/    (Index, Create, Edit)
    │   ├── Configuracion/ (Index, Edit)
    │   ├── Auditoria/
    │   │   ├── Index.cshtml
    │   │   └── Reporte.cshtml
    │
    └── wwwroot/
        ├── css/
        │   ├── site.css
        │   ├── producto.css
        │   ├── pedido.css
        │   ├── pago.css
        │   ├── auditoria.css
        │   ├── login.css
        │   ├── impresion.css
        │   └── SideBarAdministrativo.css
        ├── js/
        │   └── site.js
        ├── images/
        │   ├── Productos/
        │   │   └── default.svg
        │   ├── mesas/
        │   └── QRs/
        └── lib/
```

---

## Base de Datos

**Nombre:** `EmpresaBD`
**Servidor:** `127.0.0.1,1433` (SQL Server con autenticación SQL)

### Diagrama de Tablas

```
┌──────────────┐       ┌──────────────────┐       ┌──────────────┐
│    Cargo      │       │    Empleado       │       │   TurnoCaja  │
├──────────────┤       ├──────────────────┤       ├──────────────┤
│ IdCargo (PK) │◄──FK──│ IdEmpleado (PK)  │◄──FK──│ IdTurno (PK) │
│ Nombre       │       │ IdCargo (FK)     │       │ IdEmpleado   │
│ Sueldo       │       │ NombreCompleto   │       │ FechaApertura│
│ Activo       │       │ Dni (UNIQUE)     │       │ FechaCierre  │
└──────────────┘       │ Usuario (UNIQUE) │       │ MontoInicial │
                       │ Contrasena       │       │ MontoCierre  │
                       │ Telefono         │       │ Observaciones│
                       │ Activo           │       └──────────────┘
                       │ FechaCreacion    │
                       └──────────────────┘

┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│  Categoria   │       │    Mesa       │       │   Cliente    │
├──────────────┤       ├──────────────┤       ├──────────────┤
│ IdCategoria  │       │ IdMesa (PK)  │       │ IdCliente(PK)│
│ Nombre       │       │ Numero       │       │ Dni          │
│ Activo       │       │ Estado       │       │ NombreCompleto│
└──────┬───────┘       │ Activo       │       │ Telefono     │
       │               └──────────────┘       │ FechaRegistro│
       │                                      │ Activo       │
       │         ┌──────────────┐             └──────────────┘
       └───FK───►│   Producto    │
                 ├──────────────┤
                 │ IdProducto(PK)│
                 │ IdCategoria   │
                 │ Nombre        │
                 │ Precio        │
                 │ RutaImagen    │
                 │ Activo        │
                 │ FechaCreacion │
                 └──────────────┘

┌──────────────┐       ┌──────────────────┐       ┌──────────────┐
│   Pedido     │       │  DetallePedido   │       │    Pago      │
├──────────────┤       ├──────────────────┤       ├──────────────┤
│ PedidoID (PK)│◄──FK──│ DetalleID (PK)   │       │ PagoID (PK)  │
│ TurnoID (FK) │       │ PedidoID (FK)    │       │ PedidoID (FK)│
│ ClienteID    │       │ ProductoID (FK)  │       │ Metodo       │
│ EmpleadoID   │       │ Cantidad         │       │ Monto        │
│ MesaID       │       │ PrecioUnitario   │       │ Vuelto       │
│ FechaHora    │       │ Modificadores    │       │ QR_Ruta      │
│ TipoServicio │       │ Entregado        │       │ QR_Bytes     │
│ Estado       │       └──────────────────┘       │ Fecha        │
│ Subtotal     │                                  │ Estado       │
│ IGV          │                                  └──────────────┘
│ Total        │
│ NotasEspec.  │       ┌──────────────────┐
└──────────────┘       │ AuditoriaSistema  │
                       ├──────────────────┤
                       │ AuditoriaID (PK) │
                       │ Tabla            │
                       │ RegistroID       │
                       │ Accion           │
                       │ EmpleadoID       │
                       │ Fecha            │
                       │ Detalle          │
                       └──────────────────┘
```

### Script SQL

El script `Script_EmpresaBD.sql` crea las tablas base (Cargo, Empleado, Categoria, Cliente, Mesa) y datos semilla. Las tablas adicionales (Producto, Pedido, DetallePedido, Pago, TurnoCaja, ConfiguracionSistema, AuditoriaSistema) deben crearse manualmente o se crean al ejecutar la aplicación.

---

## Arquitectura y Patrón de Diseño

### Patrón Repository con ADO.NET

```
┌─────────────┐     ┌─────────────────┐     ┌──────────────────┐
│   View       │────►│   Controller     │────►│   Repository     │
│  (Razor)     │◄────│  (ASP.NET MVC)   │◄────│  (ADO.NET puro)  │
└─────────────┘     └─────────────────┘     └────────┬─────────┘
                                                      │
                                              ┌───────▼────────┐
                                              │  SQL Server     │
                                              │  (EmpresaBD)    │
                                              └────────────────┘
```

### Flujo de datos

1. **Vista** envía formulario al **Controller** vía POST
2. **Controller** valida con `ModelState.IsValid` + validaciones manuales
3. **Controller** llama al **Repository** (inyectado vía DI)
4. **Repository** ejecuta SQL con `SqlCommand` y mapea resultados con `Mapear()`
5. **Controller** retorna Vista con datos o redirige a Index

### Inyección de Dependencias (Program.cs)

```csharp
builder.Services.AddScoped<CargoRepository>();
builder.Services.AddScoped<EmpleadoRepository>();
builder.Services.AddScoped<CategoriaRepository>();
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<MesaRepository>();
builder.Services.AddScoped<ProductoRepository>();
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<DetallePedidoRepository>();
builder.Services.AddScoped<PagoRepository>();
builder.Services.AddScoped<TurnoCajaRepository>();
builder.Services.AddScoped<ConfiguracionSistemaRepository>();
builder.Services.AddScoped<AuditoriaRepository>();
builder.Services.AddScoped<DashboardRepository>();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<EmailService>();
```

---

## Configuración y Ejecución

### Requisitos

- .NET 10.0 SDK
- SQL Server Express (o compatible)
- Visual Studio 2022 / VS Code

### Pasos

1. **Clonar el repositorio**

2. **Crear la base de datos:**
   - Abrir SQL Server Management Studio
   - Ejecutar `Script_EmpresaBD.sql`
   - Crear tablas adicionales (Producto, Pedido, DetallePedido, Pago, TurnoCaja, ConfiguracionSistema, AuditoriaSistema)

3. **Verificar cadena de conexión** en `appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=127.0.0.1,1433;Database=EmpresaBD;User Id=sa;Password=SQLadmin123/;TrustServerCertificate=True;"
     }
   }
   ```

4. **Ejecutar:**

   ```bash
   dotnet run
   ```

5. **Abrir en el navegador:**
   ```
   https://localhost:7239
   ```

> La ruta por defecto es `Home/Index` (Dashboard) después del login.

### Autenticación

El sistema utiliza autenticación basada en cookies con roles:

- **Admin**: Acceso completo a todos los módulos
- **Cajero**: Pedidos, DetallePedido, Pagos, Turnos Caja, Dashboard
- **Mozo**: Pedidos, DetallePedido, Productos, Mesas, Clientes, Dashboard

Las contraseñas se almacenan en texto plano en la base de datos.

#### Permisos por módulo

| Módulo        | Admin             | Cajero | Mozo |
| ------------- | ----------------- | ------ | ---- |
| Dashboard     | ✅                | ✅     | ✅   |
| Cargos        | ✅ (solo lectura) | 🔒     | 🔒   |
| Empleados     | ✅                | 🔒     | 🔒   |
| Categorías    | ✅                | 🔒     | 🔒   |
| Clientes      | ✅                | ✅     | ✅   |
| Mesas         | ✅                | ✅     | ✅   |
| Productos     | ✅                | ✅     | ✅   |
| Pedidos       | ✅                | ✅     | ✅   |
| DetallePedido | ✅                | ✅     | ✅   |
| Pagos         | ✅                | ✅     | 🔒   |
| Turnos Caja   | ✅                | ✅     | 🔒   |
| Configuración | ✅                | 🔒     | 🔒   |
| Auditoría     | ✅                | 🔒     | 🔒   |

---

## Módulos del Sistema

### 1. Dashboard (Home)

- **Métricas**: Ventas del día, pedidos, clientes, ingresos
- **Gráficos**: Ventas de los últimos 7 días, platos más vendidos
- **Mapa de mesas**: Estado actual de todas las mesas
- **Últimos pedidos**: Lista de pedidos recientes
- **Acceso Rápido**: Tarjetas role-aware
  - **Admin**: Pedido, Listar, Reporte (Auditoría), Cliente
  - **Mozo/Cajero**: Pedido, Listar, Productos, Mesas

### 2. Login/Autenticación

- **Login**: Formulario con usuario y contraseña
- **Roles**: Admin, Cajero, Mozo
- **Sesiones**: Cookie-based con expiración de 8 horas
- **Logout**: Cierre de sesión seguro
- **Redirección**: Todos los roles ingresan al Dashboard

### 3. Cargos (Solo lectura)

| Campo  | Tipo          | Descripción      |
| ------ | ------------- | ---------------- |
| Nombre | VARCHAR(100)  | Nombre del cargo |
| Sueldo | DECIMAL(10,2) | Sueldo base      |
| Activo | BIT           | Estado del cargo |

**Vista:** Tabla de consulta únicamente. Los cargos son predefinidos por el sistema (Admin, Cajero, Mozo) y no pueden crearse, editarse ni eliminarse porque los permisos de acceso están vinculados internamente a cada rol. El sueldo es editable desde la vista de Index.

### 4. Empleados

| Campo          | Tipo         | Validación                             |
| -------------- | ------------ | -------------------------------------- |
| Cargo          | FK (select)  | Obligatorio                            |
| NombreCompleto | VARCHAR(70)  | Obligatorio, solo letras (sin números) |
| Dni            | CHAR(8)      | Regex: solo 8 dígitos, único           |
| Usuario        | VARCHAR(20)  | Obligatorio, único                     |
| Contrasena     | NVARCHAR(64) | Obligatorio                            |
| Telefono       | VARCHAR(9)   | Regex: empieza con 9 + 8 dígitos       |
| Activo         | BIT          | Toggle switch                          |

**Vista:** Tabla con badge de cargo, campos deshabilitados cuando Activo = false.

**Validaciones adicionales (Controller):**

- DNI único (excluyendo el registro actual al editar)
- Usuario único (excluyendo el registro actual al editar)
- Edición permite cambiar solo el estado sin revalidar otros campos (carga datos existentes)
- No se permite eliminar al único administrador

### 5. Categorías

| Campo  | Tipo        | Validación         |
| ------ | ----------- | ------------------ |
| Nombre | VARCHAR(50) | Obligatorio, único |
| Activo | BIT         | Toggle switch      |

**Vista:** Tabla simple con acciones.

### 6. Clientes

| Campo          | Tipo        | Validación                             |
| -------------- | ----------- | -------------------------------------- |
| Dni            | CHAR(8)     | Regex: 8 dígitos                       |
| NombreCompleto | VARCHAR(50) | Obligatorio, solo letras (sin números) |
| Telefono       | VARCHAR(9)  | Regex: empieza con 9 + 8 dígitos       |
| Activo         | BIT         | Toggle switch                          |

**Vista:** Tabla con formato de teléfono.

### 7. Mesas

| Campo  | Tipo        | Validación              |
| ------ | ----------- | ----------------------- |
| Numero | INT         | Obligatorio, único, > 0 |
| Estado | VARCHAR(20) | Libre/Ocupada           |
| Activo | BIT         | Toggle switch           |

**Vista:** Tarjetas de estado con íconos, filtro por estado, botón para cambiar estado.

### 8. Productos

| Campo       | Tipo          | Validación                                             |
| ----------- | ------------- | ------------------------------------------------------ |
| Categoria   | FK (select)   | Obligatorio                                            |
| Nombre      | VARCHAR(100)  | Obligatorio, no solo espacios                          |
| Descripcion | VARCHAR(200)  | Opcional                                               |
| Precio      | DECIMAL(10,2) | Obligatorio, > 0                                       |
| RutaImagen  | VARCHAR(500)  | Imagen obligatoria en creación (JPG/JPEG/PNG, max 5MB) |
| Activo      | BIT           | Toggle switch                                          |

**Vista:** Grid de tarjetas (cards) con imagen, nombre, categoría, precio y acciones. Búsqueda por nombre y filtro por categoría.

**Características especiales:**

- Validación JavaScript de extensión (.jpg/.jpeg/.png) y tamaño (5MB) antes del envío
- Carga de imágenes con preview en tiempo real
- Imágenes almacenadas en `wwwroot/images/Productos/` con nombres GUID
- Edición permite actualizar sin cambiar imagen (campo hidden mantiene la ruta existente)
- Validación pre-eliminación: no permite eliminar si tiene registros en pedidos

### 9. Turnos de Caja

| Campo         | Tipo          | Validación          |
| ------------- | ------------- | ------------------- |
| Empleado      | FK (select)   | Obligatorio         |
| MontoInicial  | DECIMAL(10,2) | ≥ 0                 |
| FechaCierre   | DATETIME2     | NULL = Abierto      |
| MontoCierre   | DECIMAL(10,2) | Requerido al cerrar |
| Observaciones | VARCHAR(200)  | Opcional            |

**Vista:** Tabla con filtros por estado y fechas, alerta de turno abierto, modal para cerrar turno con SweetAlert2.

### 10. Configuración del Sistema

| Campo          | Tipo          | Validación                |
| -------------- | ------------- | ------------------------- |
| NombreNegocio  | NVARCHAR(80)  | Obligatorio               |
| RUC            | CHAR(11)      | 11 dígitos numéricos      |
| RazonSocial    | NVARCHAR(120) | Obligatorio               |
| IGV_Porcentaje | DECIMAL(5,2)  | 0-100                     |
| Moneda         | CHAR(3)       | PEN o USD                 |
| NumeroYape     | VARCHAR(15)   | Opcional                  |
| Correo         | VARCHAR(100)  | Obligatorio, email válido |

**Vista:** Formulario singleton (una sola fila en BD). El campo Correo se utiliza para recibir notificaciones de pagos Yape.

### 11. Pedidos (POS)

| Campo           | Tipo    | Validación                                           |
| --------------- | ------- | ---------------------------------------------------- |
| TurnoID         | FK      | Obligatorio                                          |
| EmpleadoID      | FK      | Obligatorio (solo el empleado logueado)              |
| MesaID          | FK      | Opcional (si es null = Para llevar)                  |
| TipoServicio    | VARCHAR | Mesa/ParaLlevar (automático según selección de mesa) |
| Estado          | VARCHAR | Pendiente/Completado/Anulado                         |
| NotasEspeciales | VARCHAR | Opcional                                             |

**Vista:** Interfaz POS completa con:

- Grid de productos con búsqueda y filtro por categoría
- Panel de carrito de compras con iconos de categoría
- Selección de mesa con indicador visual (● al lado del número)
- Solo muestra el empleado que está logueado (no selection dropdown)
- Cálculo automático de IGV y total
- Creación de pedidos vía AJAX
- Validación AJAX de mesa con pedido activo antes de agregar productos
- Mínimo 1 producto, turno abierto para crear pedido

### 12. Detalle de Pedidos

| Campo          | Tipo    | Validación        |
| -------------- | ------- | ----------------- |
| PedidoID       | FK      | Obligatorio       |
| ProductoID     | FK      | Obligatorio       |
| Cantidad       | INT     | > 0               |
| PrecioUnitario | DECIMAL | > 0               |
| Modificadores  | VARCHAR | Opcional          |
| Entregado      | BIT     | Estado de entrega |

**Vista:**

- Lista paginada con filtros duales: Estado Entrega y Estado Pago
- Marcar como servido con actualización instantánea de UI
- Vista de impresión para comanda

### 13. Pagos

| Campo    | Tipo      | Validación           |
| -------- | --------- | -------------------- |
| PedidoID | FK        | Obligatorio          |
| Metodo   | VARCHAR   | Efectivo/Yape        |
| Monto    | DECIMAL   | > 0                  |
| Vuelto   | DECIMAL   | Calculado (Efectivo) |
| QR_Ruta  | VARCHAR   | Generado (Yape)      |
| QR_Bytes | VARBINARY | Generado (Yape)      |
| Fecha    | DATETIME2 | Automático           |
| Estado   | VARCHAR   | Confirmado/Anulado   |

**Vista:**

- Listado de pedidos pendientes de pago con paginación y filtros
- Formulario de registro de pago (Efectivo/Yape) con flujo AJAX
- Generación de código QR para Yape
- Validación de Base64 antes de procesar QR
- Comprobante de pago con tabla de productos, subtotal/IGV/total
- Historial de pagos con búsqueda case-insensitive y modal VER con detalle completo
- Envío automático de email de notificación al registrar pago Yape

### 14. Auditoría del Sistema

| Campo      | Tipo      | Descripción                    |
| ---------- | --------- | ------------------------------ |
| Tabla      | VARCHAR   | Tabla afectada                 |
| RegistroID | INT       | ID del registro                |
| Accion     | VARCHAR   | INSERT/UPDATE/DELETE           |
| EmpleadoID | FK        | Empleado que realizó la acción |
| Fecha      | DATETIME2 | Fecha y hora                   |
| Detalle    | VARCHAR   | Detalle de la acción           |

**Vista:**

- Lista paginada con filtros (fecha, tabla, acción, empleado)
- Reporte de auditoría
- Solo accesible por Administradores

---

## Validaciones

### Nivel de Modelo (DataAnnotations)

```csharp
[Required(ErrorMessage = "...")]          // Campo obligatorio
[StringLength(100)]                       // Longitud máxima
[Range(0.01, double.MaxValue)]            // Valor mínimo
[RegularExpression(@"^(?!\s*$).+")]       // Rechazar solo espacios
```

### Nivel de Controller

- **ModelState** con `Clear()` para edición flexible de empleados
- Validación de unicidad (DNI, Usuario, Nombre de categoría)
- Validación de archivos (extensión y tamaño en JavaScript)
- Try-catch en operaciones críticas para evitar crashes
- Validación de FK constraints (SqlException 547)

### Nivel de Vista (jQuery Validation)

```html
<span asp-validation-for="Nombre" class="text-danger"></span>
```

Mensajes en español vía `DataAnnotations` en los modelos.

### Validación de imagen (JavaScript)

```javascript
// Extensión y tamaño se validan antes del envío
var extensionesPermitidas = [".jpg", ".jpeg", ".png"];
var tamanoMaximo = 5 * 1024 * 1024; // 5MB
```

### Patrón para campos deshabilitados (Empleado)

Cuando los campos se deshabilitan (Activo = false), el controller carga los datos existentes antes de validar:

```csharp
if (string.IsNullOrWhiteSpace(empleado.NombreCompleto))
    empleado.NombreCompleto = existente.NombreCompleto;
ModelState.Clear(); // Limpiar errores previos
```

---

## Frontend

### Layout Principal

El layout (`_Layout.cshtml`) contiene:

- **Sidebar fijo** a la izquierda con navegación por módulos
- **Topbar móvil** con hamburger menu
- **Contenido principal** con `@RenderBody()`
- **Footer** con copyright

### Sidebar Administrativo

```
┌──────────────────┐
│  Chifa Percy     │
│  Sabor que conquista │
├──────────────────┤
│   MENÚ           │
│ > Dashboard      │
│   Cargos         │  ← Solo Admin (solo lectura)
│   Empleados      │  ← Solo Admin
│   Categorías     │  ← Solo Admin
│   Clientes       │
│   Mesas          │
│   Productos      │
│   Pedidos (POS)  │
│   Detalle Pedidos│
│   Pagos          │  ← Admin/Cajero
│   Turnos Caja    │  ← Admin/Cajero
│   Configuración  │  ← Solo Admin
│   Auditoría      │  ← Solo Admin
├──────────────────┤
│   Restaurante    │
│   Desde 2018     │
└──────────────────┘
```

### Hojas de Estilo

| Archivo                     | Responsabilidad                                                         |
| --------------------------- | ----------------------------------------------------------------------- |
| `site.css`                  | Tipografía, reset, cards, tables, badges, buttons, switches, validación |
| `producto.css`              | Tarjetas de producto, filtros, badges de estado, botones de acción      |
| `SideBarAdministrativo.css` | Sidebar responsive con overlay, scrollbar personalizada                 |
| `pedido.css`                | Estilos del POS, iconos de categoría del carrito                        |
| `pago.css`                  | Comprobante de pago, total Yape, secciones de pago                      |
| `auditoria.css`             | Estilos del módulo de auditoría                                         |
| `login.css`                 | Formulario de login                                                     |
| `impresion.css`             | Estilos de impresión de comprobantes                                    |

### Interacciones JavaScript

- **SweetAlert2**: Confirmaciones de eliminación, mensajes de éxito/error
- **AJAX fetch**: Eliminación, creación de pedidos, cambio de estado, validación de mesa
- **Validación de imagen**: Extensión y tamaño antes del envío
- **Preview de imagen**: FileReader para mostrar imagen antes de subir
- **Toggle de campos**: Campos deshabilitados/habilitados según switch Activo
- **Carrito de compras**: Gestión de items en el POS con iconos de categoría
- **Generación de QR**: Códigos QR para pagos Yape

---

## Consideraciones Técnicas

1. **Sin Entity Framework**: Todo el acceso a datos es ADO.NET puro con `Microsoft.Data.SqlClient`.

2. **Autenticación basada en cookies**: Sistema de roles con Claims, expiración de 8 horas.

3. **Passwords en texto plano**: Las contraseñas se almacenan directamente en la base de datos sin cifrado.

4. **Generación de QR**: Utiliza QRCoder para generar códigos QR de pagos Yape.

5. **Auditoría automática**: Todas las operaciones CRUD se registran en la tabla AuditoriaSistema.

6. **.NET 10.0**: Framework de última generación.

7. **Redirección post-login**: Todos los roles ingresan al Dashboard (`Home/Index`).

8. **Manejo de errores**: Try-catch en operaciones críticas para evitar crashes de la aplicación.

9. **Transacciones**: Las operaciones complejas (crear pedido, anular pago) usan transacciones SQL.

10. **Imágenes**: Almacenadas en disco con nombres GUID, eliminación física al eliminar registros.

11. **Búsqueda case-insensitive**: Historial de pagos utiliza `COLLATE SQL_Latin1_General_CP1_CI_AS`.

12. **Dashboard role-aware**: Tarjetas de acceso rápido personalizadas según el rol del usuario.

13. **Cargos predefinidos**: No se permiten crear, editar ni eliminar cargos (permisos vinculados internamente).

14. **Validación de mesa**: Verificación AJAX de pedido activo al seleccionar mesa en POS.

15. **Notificación por email**: Al registrar un pago con Yape se envía un correo de notificación al propietario del sistema (correo configurado en ConfiguracionSistema) con detalles del pago, número de operación y nombre del yapero. Utiliza MailKit con SMTP de Gmail.

---

## Configuración Adicional

### Archivos de Configuración

- `appsettings.json`: Cadena de conexión, configuración general y SmtpSettings
- `appsettings.Development.json`: Configuración de desarrollo (logging)
- `Properties/launchSettings.json`: Puertos de desarrollo (HTTP: 5099, HTTPS: 7239)

### Configuración SMTP (Gmail)

```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "User": "correo@gmail.com",
    "Password": "app-password",
    "SenderEmail": "correo@gmail.com",
    "SenderName": "Nombre del Negocio"
  }
}
```

### Variables de Entorno

```json
{
  "AuditDefaultEmpleadoId": 1
}
```

### Estructura de Imágenes

```
wwwroot/images/
├── Productos/
│   └── default.svg
├── mesas/
│   ├── mesa-libre.png
│   ├── mesa-ocupada.png
│   ├── mesa-reservada.png
│   └── mesa-inactiva.png
└── QRs/
```

## Mi participación

Durante el desarrollo del proyecto participé en:

- Desarrollo del módulo de Pedidos (POS).
- Desarrollo del módulo de Detalle de Pedidos.
- Desarrollo del módulo de Pagos.
- Propuesta de integración de MailKit para el envío de notificaciones por correo electrónico en la simulación de pagos.
- Pruebas funcionales, detección y corrección de errores.
