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
