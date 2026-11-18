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

## 🥫 Pantalla "Mi despensa" (TP10)

Implementa **RF-11** en Angular: listar, editar, marcar como consumidos y eliminar los ítems propios, con **RF-09** aplicado a los datos faltantes. **RF-10** (agregar) queda disponible sólo en el BackEnd, sin pantalla.

| Funcionalidad | AppService y método | Endpoint | Proxy (`@proxy/pantries`) |
|---|---|---|---|
| RF-11 listar ítems propios | `PantryItemAppService.GetListAsync` | `GET /api/app/pantry-item` | `PantryItemService.getList` |
| RF-11 ver un ítem | `PantryItemAppService.GetAsync` | `GET /api/app/pantry-item/{id}` | `PantryItemService.get` |
| RF-11 editar cantidad, unidad y vencimiento | `PantryItemAppService.UpdateAsync` | `PUT /api/app/pantry-item/{id}` | `PantryItemService.update` |
| RF-11 marcar como consumido | `PantryItemAppService.ConsumeAsync` | `POST /api/app/pantry-item/{id}/consume` | `PantryItemService.consume` |
| RF-11 eliminar | `PantryItemAppService.DeleteAsync` | `DELETE /api/app/pantry-item/{id}` | `PantryItemService.delete` |
| RF-10 agregar (sólo BackEnd) | `PantryItemAppService.CreateAsync` | `POST /api/app/pantry-item` | `PantryItemService.create` (sin uso en la UI) |

**Acceso**
- `PantryItemAppService` tiene `[Authorize]`: sin sesión ni token responde **401**.
- El propietario sale siempre de `ICurrentUser`. Antes de leer, editar, consumir o eliminar, se busca el ítem **dentro de las despensas del usuario actual**.
- **Un ítem de otro usuario responde 404**, igual que uno inexistente, en todas las operaciones. Así la API no revela que el ítem existe.
- La UI oculta el menú a quien no tiene sesión, pero eso no reemplaza la autorización: la protección está en el servidor.

**Ítems consumidos (regla de negocio 6):** se **conservan como historial**. Por defecto el listado muestra sólo los disponibles; el interruptor "Mostrar consumidos" los agrega al final, atenuados y con la etiqueta "Consumido". Para quitar un ítem de la despensa hay que eliminarlo de forma explícita, con confirmación previa.

**ListService:** el listado es paginado (`PagedResultDto`), así que la pantalla usa `ListService` para la paginación y la recarga después de cada operación.

**Criterio de fechas:** las fechas de vencimiento son días calendario sin hora (`DateOnly` en el BackEnd, `"yyyy-MM-dd"` en el proxy). La UI las muestra con el formato del idioma de la aplicación y las convierte sin pasar por `Date`, para que la zona horaria no corra el día. **Coincide con el criterio del worker del TP08**, que compara contra el día calendario de Argentina.

**Manejo de errores**
- **Listado:** usa `skipHandleError: true`. La pantalla muestra su propio estado de error con un mensaje localizado y el botón "Reintentar", en lugar del modal global de ABP sobre una pantalla vacía.
- **Editar, consumir y eliminar:** usan el manejo predeterminado de ABP, que muestra el mensaje del servidor de forma comprensible. Si el servidor rechaza una edición, el modal sigue abierto con lo ingresado.
- No se muestran detalles técnicos ni códigos de estado como único mensaje.

**Validación:** el servidor valida cantidad no negativa, unidad de hasta 16 caracteres y fecha válida. El formulario repite esas reglas por usabilidad, con los mensajes localizados de ABP, y no llama al servidor si hay errores.

**Pruebas:** `yarn test --watch=false --browsers=ChromeHeadless` en `angular` (componente con el proxy mockeado) y la suite del BackEnd desde Test Explorer.

## 🔎 Catálogo de productos (entrega final)

Cubre **HU-02** y **HU-03**: buscar un producto por código o por texto, ver su detalle, guardarlo sin duplicados y agregarlo a la despensa. Completa la pantalla de **RF-10**, que en el TP10 quedó sólo en el BackEnd.

| Funcionalidad | AppService y método | Endpoint | Proxy | Pantalla |
|---|---|---|---|---|
| RF-05 búsqueda por código | `ExternalProductAppService.GetByBarcodeAsync` | `GET /api/app/external-product/by-barcode` | `ExternalProductService.getByBarcode` | Buscar productos → detalle |
| RF-06 búsqueda por texto | `ExternalProductAppService.GetListAsync` | `GET /api/app/external-product` | `ExternalProductService.getList` | Buscar productos |
| RF-07 detalle de producto | `ExternalProductAppService.GetByBarcodeAsync` | `GET /api/app/external-product/by-barcode` | `ExternalProductService.getByBarcode` | Detalle (`/products/:barcode`) |
| RF-08 guardar producto externo | `ProductAppService.SaveFromExternalAsync` | `POST /api/app/product/save-from-external` | `ProductService.saveFromExternal` | Detalle → "Agregar a mi despensa" |
| RF-10 agregar a la despensa | `PantryItemAppService.CreateAsync` | `POST /api/app/pantry-item` | `PantryItemService.create` | Detalle → "Agregar a mi despensa" |

**Integración externa**
- **Por código (RF-05, RF-07):** Product Opener API v3 (`https://world.openfoodfacts.org/api/v3/product/{código}`). Se piden sólo los campos que se usan: nombre, marca, imagen, categorías, ingredientes, alérgenos, Nutri-Score y NOVA.
- **Por texto (RF-06):** Search-a-licious (`https://search.openfoodfacts.org/search?q=…&langs=es`). Busca a la vez por nombre, marca y categoría. El texto del usuario se reduce a letras, números y espacios, para que nunca se interprete como sintaxis de consulta.
- Las dos consultas están detrás de `IExternalProductCatalogClient` (Application). La implementación (`OpenFoodFactsProductCatalogClient`, HttpApi.Host) es la única que conoce las URLs, los códigos HTTP y el JSON del proveedor.
- Cada llamada registra el tipo, el resultado y la duración. No se registran el contenido de la respuesta ni el texto buscado.
- **Paginación:** Search-a-licious pagina por número de página, así que `SkipCount` tiene que ser múltiplo de `MaxResultCount`. Se navega hasta **1.000 resultados**: el proveedor informa hasta 10.000, pero las páginas muy profundas fallan y nadie las recorre.
- **Estados:** las dos operaciones devuelven `Found`, `NotFound`, `RateLimited` (HTTP 429) o `ServiceUnavailable` (sin conexión, timeout o error del proveedor). La pantalla muestra un mensaje distinto para cada uno.

**Datos faltantes (RF-09)**
- Todos los datos de RF-07 son opcionales, **incluida la marca**, que dejó de ser obligatoria porque muchos productos no la traen.
- El Nutri-Score `"unknown"` o `"not-applicable"` y un grupo NOVA fuera de 1-4 se informan como "No informado", no como un valor.
- **Alérgenos:** se guardan las etiquetas canónicas de Open Food Facts (`en:milk`), porque son las que se compararán con las preferencias del usuario. El texto libre cargado por usuarios del proveedor, como `es:Contiene leche`, se descarta. Una lista vacía se muestra como "No informados (eso no significa que no tenga)".
- Los textos de terceros que superan el largo de la columna se recortan en lugar de rechazar el producto.

**Guardado sin duplicados (RF-08, regla de negocio 4)**
- `AppProducts.Barcode` tiene un **índice único filtrado**: los productos sin código no tienen restricción. El alta y la edición manual validan el código antes de guardar y responden un mensaje localizado si ya existe.
- `SaveFromExternalAsync` recibe **sólo el código**: el servidor vuelve a consultar Open Food Facts y guarda lo que obtiene, sin confiar en datos armados por el navegador.
- **Es repetible:** si el código ya está guardado, devuelve ese producto sin volver a consultar al proveedor. Agregar dos veces el mismo producto crea dos ítems de despensa (por ejemplo, dos unidades con distinto vencimiento), pero un solo producto.
- Si el proveedor no informa el nombre, la pantalla pide que el usuario lo escriba. El sistema nunca lo inventa.

**Pantallas**
- **Buscar productos (`/products`):** un solo campo. Con 8 a 14 dígitos va directo al detalle; con cualquier otro texto busca por nombre, marca o categoría, así que no hace falta conocer códigos. Usa `ListService` para paginar. El texto buscado queda en la URL (`?q=`), así que "Volver a la búsqueda" conserva los resultados.
- **Detalle (`/products/:barcode`):** muestra los datos de RF-07 y el botón "Agregar a mi despensa". El modal pide cantidad, unidad y vencimiento, todos opcionales, con las mismas validaciones que la edición del TP10.
- Las dos pantallas requieren sesión (`authGuard`, menú visible sólo con sesión) y muestran la leyenda de la fuente: *"Datos de Open Food Facts… SmartPantry no ofrece consejo médico"*.
- **Errores:** la búsqueda y el detalle usan `skipHandleError` y muestran su propio estado con "Reintentar". "Agregar" usa el manejo predeterminado de ABP, que muestra el mensaje del servidor, por ejemplo "Open Food Facts no está disponible", y deja el modal abierto con lo ingresado.

**Base de datos:** la migración `AddProductCatalogDetails` agrega las columnas de RF-07, hace opcional la marca y crea el índice único. Si una base local ya tenía dos productos con el mismo código, la migración falla al crear el índice: hay que borrar o corregir el duplicado y volver a correr `SmartPantry.DbMigrator`.
