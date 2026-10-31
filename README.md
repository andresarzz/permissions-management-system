# Sistema de Gestión de Permisos

Proyecto personal de **Andrés Araya**, desarrollado con **C#, ASP.NET Core MVC (.NET 8), Entity Framework Core y SQL Server**.

## ¿Qué hace?
Aplicación web para gestionar usuarios y roles (Admin, Supervisor y Docente), solicitudes de permisos y omisiones de marca, reportes de daños, registros de auditoría y exportaciones a Excel. Las funciones dependen de la autorización de cada rol.

## Tecnologías y arquitectura
- ASP.NET Core MVC, vistas Razor, C#
- SQL Server y Entity Framework Core (migraciones)
- Separación entre controladores, servicios, repositorios, modelos y ViewModels
- Autenticación mediante cookies, autorización por roles y políticas
- EPPlus para exportación de reportes a Excel

## Instalación de desarrollo
1. Instalar .NET SDK 8 y SQL Server (o LocalDB), y clonar el repositorio.
2. Abrir `AdminSeguridad.sln`? **No**: la solución de este proyecto es `SistemaPermisos.sln`. Abrirla desde Visual Studio o usar `dotnet restore SistemaPermisos/SistemaPermisos.csproj`.
3. Configurar `ConnectionStrings:DefaultConnection` para una base de datos local en configuración de desarrollo, preferiblemente mediante variables de entorno o Secret Manager. No subir secretos al repositorio.
4. Ejecutar `dotnet ef database update --project SistemaPermisos` (requiere herramienta dotnet-ef) o permitir que la aplicación aplique migraciones pendientes con una base accesible.
5. Ejecutar `dotnet run --project SistemaPermisos`.

### Datos de prueba (opcional, solo desarrollo)
No se generan cuentas de demostración automáticamente. Si se desean datos de prueba, configurar `SeedData__Enabled=true` y las variables `SEED_ADMIN_PASSWORD`, `SEED_SUPERVISOR_PASSWORD` y `SEED_DOCENTE_PASSWORD` (mínimo 12 caracteres) en un entorno de **Development**. No usar credenciales de ejemplo en producción.

## Estado y precauciones
Este repositorio está en proceso de revisión para portafolio. Se identificaron inconsistencias heredadas entre algunas vistas/controladores y servicios, por lo que se recomienda ejecutar una compilación, corregir los errores restantes y probar todos los flujos antes de desplegarlo. La revisión estática no equivale a pruebas funcionales ni a una auditoría de seguridad completa.

## Qué demostrar en una entrevista
- Diseño de una aplicación MVC conectada a SQL Server
- Modelado de roles y autorización
- Lógica de negocio y capas de servicios/repositorios
- Flujos de solicitudes, auditoría y exportación de datos

## Autoría
Desarrollado individualmente por Andrés Araya. Los commits pueden contener distintas identidades usadas por el mismo autor durante la realización del proyecto.
