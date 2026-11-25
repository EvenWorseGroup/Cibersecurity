# Informe de integración

**Dictamen: BLOCKED.** La API compila, supera las pruebas HTTP en memoria y genera el paquete IIS. Falta comprobar la consulta con SQL Server real, ejecutar el ensayo completo de recuperación y arrancar el paquete en el IIS de destino.

## Inspección y trazabilidad

La rama `integration/api-sql-recovery` parte de `main` en `2be1d56`. Los commits técnicos `913a7f2` (Marco) y `6b41422` (Pablo) se incorporaron como `0b2bee5` y `404b6b7` con la autoría que consta en Git. El identificador de committer local de integración se usó porque Git carecía de identidad configurada. Los dos commits posteriores de la rama de recuperación solo cambiaban el README. El estado inicial estaba limpio y no había un informe de auditoría separado disponible en las ramas inspeccionadas.

La revisión directa confirmó que la API usaba IDs 1001 y 1002 en memoria, mientras que el SQL asignaba IDs mediante IDENTITY y usaba columnas incompatibles. El flujo anterior borraba expedientes y forzaba la restauración sobre el origen. El `.gitignore` contenía un fragmento de PowerShell junto a sus patrones.

## Arquitectura final

| Archivos | Cambio y motivo |
| --- | --- |
| `src/api/Data/PacienteStore.cs`, `Controllers/PacientesController.cs`, `ClinicaSecurityApi.csproj` | Consulta asíncrona parametrizada con SqlClient 6.1.8. El controlador decide el acceso por rol y asignación antes de consultar. Las rutas y el contrato de respuesta se conservan. |
| `src/api/Program.cs` | Registra el repositorio SQL. Conserva validaciones de firma, emisor, audiencia y expiración del JWT. Las opciones se construyen con la configuración del entorno y se validan al arrancar. |
| `data/*.sql` | Crea un esquema acorde con la API y semillas ficticias estables. Separa inicialización, respaldo, validación, restauración en destino nuevo e integridad. La guardia comprueba confirmación e instancia y serializa los scripts del laboratorio. La restauración comprueba el archivo, checksum, nombres lógicos, rutas y disponibilidad del destino. |
| `tests/api/ApiFactory.cs`, `ApiIntegrationTests.cs` | Usa una clave JWT efímera y sustituye el repositorio de pacientes en el servidor de pruebas. Verifica también el rechazo de un JWT malformado. El cliente de pruebas usa HTTPS y el host de pruebas prescinde del registro de eventos de Windows. |
| `.gitignore`, `README.md`, `runbooks/playbook_recuperacion.md` | Excluye compilaciones, archivos SQL físicos y archivos locales. Documenta contrato, configuración, publicación y ensayo manual. |

Las cuentas demo conservan IDs 1 y 2 y roles `Clinico` y `Administrador`. La aplicación valida el hash de las contraseñas de configuración externa. SQL conserva pacientes, con `1001` asignado al clínico y `1002` disponible para el administrador. La auditoría sigue vinculada a la memoria del proceso.

## Pruebas realizadas el 25 de noviembre de 2026

- SDK detectado `10.0.403`. Restauración NuGet completada tras resolver una restricción de permisos local.
- Compilación Release de la API completada con **0 advertencias y 0 errores**.
- Pruebas HTTP Release **14 aprobadas, 0 fallidas, 0 omitidas**. Cubren login, JWT válido y malformado, 200, 401, 403, 404 para el expediente ajeno y 400 para entradas inválidas. La prueba se ejecutó con el repositorio de pacientes sustituido. Un intento anterior falló porque el host de pruebas no podía escribir en el registro de eventos de Windows.
- Publicación en `artifacts/iis` completada. Se verificó que el paquete contiene la API, SqlClient y `web.config` con `AspNetCoreModuleV2` en modo `inprocess`. Se usó un `PublishDir` relativo porque la coma en la ruta local hizo fallar la opción `-o` de la CLI.
- Se revisaron archivos de configuración y coincidencias de patrones comunes de credenciales en las ramas inspeccionadas. Los valores de contraseña del código de pruebas son ficticios y la clave de pruebas se genera en memoria. No se identificaron secretos reales.
- No se detectaron servicios SQL Server ni `sqlcmd` en el anfitrión. No se ejecutó ningún script SQL ni se modificó un servicio.

## Compilar y publicar

```powershell
dotnet restore tests/api/ClinicaSecurityApi.Tests.csproj
dotnet build src/api/ClinicaSecurityApi.csproj -c Release --no-restore
dotnet test tests/api/ClinicaSecurityApi.Tests.csproj -c Release --no-restore
dotnet publish src/api/ClinicaSecurityApi.csproj -c Release --no-build --no-restore -p:PublishDir=../../artifacts/iis/
```

Dependencias de destino. Windows Server 2019 x64 actualizado, IIS, Hosting Bundle de ASP.NET Core 10, enlace HTTPS, SQL Server 2019 o posterior con TLS y permiso de lectura para la identidad de la API. El ensayo requiere sqlcmd ODBC, una cuenta administrativa separada y espacio disponible en los directorios SQL de la VM descartable. La configuración externa figura en [README.md](README.md). [Compatibilidad .NET](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) · [IIS y Hosting Bundle](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0) · [Propiedades de rutas SQL Server](https://learn.microsoft.com/sql/t-sql/functions/serverproperty-transact-sql)

## Pendientes y riesgos

- Ejecutar los cinco pasos del [playbook](runbooks/playbook_recuperacion.md) en la VM descartable y verificar que SQL contiene exactamente las semillas después de restaurar. Probar también el rechazo de una instancia incorrecta, un respaldo ausente y un destino ocupado.
- Comprobar la API publicada bajo IIS con su identidad real de ejecución, TLS y SQL. Repetir las consultas sobre el origen y sobre la copia. El HTTP de infraestructura ya validado externamente no acredita la ejecución de esta compilación.
- Los scripts dependen de permisos y rutas SQL del laboratorio. Una ejecución interrumpida puede dejar un destino parcial y exige preparar otro entorno limpio. La comprobación del origen del archivo incluye una revisión manual de los metadatos del respaldo.
- La auditoría en memoria se pierde al reiniciar. La ruta de salud solo verifica la respuesta de la aplicación.
