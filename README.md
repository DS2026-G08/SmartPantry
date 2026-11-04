# SmartPantry

Repositorio oficial del proyecto SmartPantry.
Aplicación integradora desarrollada para la asignatura Desarrollo de Software (UTN FRCU) - Ciclo lectivo 2026.

# SmartPantry (TP04) - Grupo 08

## 📌 Requisitos Previos
- **IDE**: Visual Studio 2022 o 2026 (con Desarrollo de ASP.NET y web)
- **.NET SDK**: 8.0 o superior
- **Node.js**: Versión 24.15.0 o superior (Instalación manual)
- **Gestor de paquetes**: Yarn 1.22.x
- **Base de Datos**: SQL Server Developer o Express
- **Herramientas adicionales**: SQL Server Management Studio (SSMS), ABP Studio y Git

## ⚙️ Configuración Local

1. **Clonar el repositorio:** 
   `git clone https://github.com/DS2026-G08/SmartPantry.git`

2. **Cambiar a la rama de trabajo:** 
   `git switch feature/4-base-tecnologica`

3. **Configurar la base de datos:** 
   Actualizar la cadena de conexión en los archivos `appsettings.json` de los proyectos **DbMigrator** y **HttpApi.Host** para apuntar al servidor local:
   `"Default": "Server=localhost;Database=SmartPantry;Trusted_Connection=True;TrustServerCertificate=true"`

## 🚀 Puesta en marcha

### 1. Base de Datos
- Establecer `SmartPantry.DbMigrator` como proyecto de inicio en Visual Studio.
- Ejecutar el proyecto para generar las tablas en SQL Server mediante Entity Framework Core.

### 2. Backend
- Abrir una terminal en `src/SmartPantry.HttpApi.Host` y ejecutar `abp install-libs` para restaurar dependencias.
- Establecer `SmartPantry.HttpApi.Host` como proyecto de inicio en Visual Studio y ejecutar.
- Verificar que se despliegue correctamente la interfaz de Swagger en el navegador (ej: `https://localhost:443XX/swagger`).

### 3. Frontend
- Navegar a la carpeta `angular` desde la terminal.
- Instalar las dependencias con el comando `yarn install`.
- Iniciar el servidor de desarrollo con `yarn start`.
- La interfaz estará disponible en `http://localhost:4200`.

## 🛠️ Comandos de verificación (CI Local)
Para verificar que el proyecto compila y pasa las pruebas antes de subir cambios:

**Backend (.NET):**
- Restaurar: `dotnet restore ./SmartPantry.slnx`
- Compilar: `dotnet build ./SmartPantry.slnx --configuration Release --no-restore`
- Pruebas: `dotnet test ./SmartPantry.slnx --configuration Release --no-build`

**Frontend (Angular):**
- Compilar: `yarn build`
- Pruebas: `yarn test --watch=false --browsers ChromeHeadless`

## 🌐 Integración externa: Open Food Facts (TP07)

Primer incremento de **RF-05 (búsqueda por código)**. El backend consulta un producto por código de barras en Open Food Facts (Product Opener API v3) y devuelve un resultado propio.

**Endpoint propio:** `GET /api/app/external-product/by-barcode?Barcode={código}` (8 a 14 dígitos; otro formato devuelve HTTP 400).

| `status` | Significado |
|---|---|
| `1` Found | El proveedor devolvió el producto |
| `2` NotFound | El proveedor no conoce ese código |
| `3` RateLimited | El proveedor limitó temporalmente las consultas (HTTP 429) |
| `4` ServiceUnavailable | El proveedor no respondió, tardó demasiado o devolvió un error |

`name`, `brand` e `imageUrl` son opcionales: si Open Food Facts no informa un dato, queda en `null` (RF-09).

**Diseño**
- `ExternalProductAppService` (Application) depende sólo de `IExternalProductCatalogClient`; no conoce `HttpClient` ni el JSON del proveedor.
- `OpenFoodFactsProductCatalogClient` (HttpApi.Host) arma la ruta v3, interpreta los códigos HTTP y traduce el JSON al DTO interno.
- El cliente se registra con `IHttpClientFactory` en `SmartPantryHttpApiHostModule` (URL base, tiempo máximo de espera de 15 s y `User-Agent` identificable).
- No se usan claves ni credenciales: la consulta es pública.
- Las pruebas del AppService reemplazan el cliente por un mock (NSubstitute) y no dependen de Internet.

**Todavía no incluido:** búsqueda por texto (RF-06), guardado del producto externo (RF-08), Angular y seguridad de usuarios.

## ⏰ Worker de vencimientos (TP08)

Implementa **RF-15 (tarea periódica)**: un proceso en segundo plano identifica ítems de despensa próximos a vencer y crea o actualiza sus advertencias sin duplicarlas.

**Regla de vencimiento** (`ExpirationRule`)
- **Umbral: 3 días**, con el día límite incluido. Con hoy = 21/10, un ítem que vence el 24/10 tiene advertencia y uno del 25/10 no.
- **Vencidos:** mantienen la advertencia activa hasta que se consuman o se corrija la fecha.
- **Sin fecha o consumidos:** no tienen advertencia; si tenían una, se desactiva.
- **"Hoy" es la fecha calendario de Argentina** (`America/Argentina/Buenos_Aires`), calculada a partir de la hora UTC del servidor (`ArgentinaCalendar`). Las fechas de vencimiento se guardan como `date`, sin hora.

**Modelo mínimo**
- `Pantry` (agregado con `OwnerId`) contiene `PantryItem` (producto, estado y fecha de vencimiento opcional).
- `PantryItemWarning` guarda una advertencia por ítem. Su clave lógica es `PantryItemId + Type`, y la base la refuerza con un **índice único**. La misma fila se actualiza, se desactiva y se reactiva; nunca se crea una segunda.
- Tablas nuevas: `AppPantries`, `AppPantryItems`, `AppPantryItemWarnings`.

**Diseño**
- `ExpirationWarningWorker` (Application, `AsyncPeriodicBackgroundWorkerBase`) decide **cuándo** se ejecuta: corre una vez por hora y también al iniciar el Host. En cada ejecución resuelve el servicio desde el alcance propio de esa ejecución, abre una unidad de trabajo y registra los errores sin detener las ejecuciones siguientes.
- `ExpirationWarningManager` (Domain) decide **qué** advertencias crear, actualizar, reactivar o desactivar. Recibe "hoy" como parámetro, así que se prueba con una fecha controlada y sin esperar el temporizador.
- El propietario sale de los datos persistidos. El worker no usa `ICurrentUser`, porque en segundo plano no hay sesión HTTP.
- **Ejecuciones cercanas:** si dos ejecuciones intentan crear la misma advertencia, el índice único rechaza la segunda, esa ejecución se registra como error y la siguiente encuentra la advertencia ya creada.
- Las pruebas desactivan los background workers (`AbpBackgroundWorkerOptions.IsEnabled = false`), así que CI no espera al worker.

**Datos locales:** al correr `SmartPantry.DbMigrator` se crea una "Despensa de prueba" del usuario `admin`, con un ítem por cada caso de la regla.

**Todavía no incluido:** API para consultar o administrar la despensa y las advertencias. Espera a Seguridad, porque sin autorización cualquiera podría elegir o modificar el propietario de un ítem. Tampoco se incluyen la vista de vencimientos (RF-13), la administración de la despensa (RF-10, RF-11) ni el envío de correos.

## 🔐 Login y seguridad (TP09)

El backend usa el login de ABP (Account + Identity) y OpenIddict para emitir y validar los access tokens. No hay login propio ni Angular en esta etapa.

| Funcionalidad | AppService y método | Endpoint | Acceso |
|---|---|---|---|
| RF-05 consulta externa | `ExternalProductAppService.GetByBarcodeAsync` | `GET /api/app/external-product/by-barcode` | Usuario autenticado |
| RF-08 guardado interno | `ProductAppService.CreateAsync` (y el resto del CRUD) | `POST /api/app/product` | Usuario autenticado |
| RF-04 listado de usuarios | `IdentityUserAppService.GetListAsync` (ABP) | `GET /api/identity/users` | Sólo con permisos de administración (`AbpIdentity.Users`) |

- `ExternalProductAppService` y `ProductAppService` tienen `[Authorize]`; se quitó el `[AllowAnonymous]` transitorio de los TPs anteriores.
- El worker de vencimientos (TP08) sigue funcionando sin usuario: no pasa por la API ni usa `ICurrentUser`.
- **Swagger** ofrece dos formas de autorizar: `oidc` (inicia sesión en la pantalla de ABP con el cliente `SmartPantry_Swagger`) y `Bearer` (pegar un access token sin el prefijo `Bearer`).
- Nunca se suben tokens, cookies ni contraseñas al código, al README ni a los PRs.
