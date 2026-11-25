# Clínica ficticia

API ASP.NET Core 10 con pacientes en SQL Server 2019 o posterior. El [informe de integración](INFORME_INTEGRACION.md) contiene el resultado de las validaciones. El [playbook](runbooks/playbook_recuperacion.md) describe la preparación de SQL y el ensayo de recuperación.

## Contrato

| Entidad | Campos y reglas |
| --- | --- |
| Paciente | `Id` entero positivo y clave primaria, `Nombre` Unicode hasta 100 caracteres, `Edad` entero entre 0 y 130, `MotivoConsulta` Unicode hasta 255 caracteres. SQL contiene las semillas `1001` y `1002`. |
| Usuario clínico | `clinico.demo`, ID `1`, rol `Clinico`, claim `paciente_id=1001`. |
| Usuario administrador | `admin.demo`, ID `2`, rol `Administrador`, sin claim `paciente_id`. |

Las cuentas demo conservan su configuración externa y se validan con el hasher de ASP.NET Core Identity. La auditoría sigue en memoria y se pierde al reiniciar el proceso. `GET /health` comprueba que la API responde. Para comprobar SQL, consultar `GET /api/pacientes/1001` con autorización.

`POST /auth/login` recibe `nombreUsuario` y `contrasena`. Las rutas protegidas reciben el encabezado `Authorization: Bearer <token>`. El clínico recibe 404 al solicitar otro expediente. El administrador puede consultar ambos. `GET /api/admin/auditoria` requiere el rol `Administrador` y devuelve 403 para el clínico. Un token ausente o inválido produce 401.

## Compilación y publicación

Desde la raíz del repositorio, con el SDK .NET 10:

```powershell
dotnet restore tests/api/ClinicaSecurityApi.Tests.csproj
dotnet build src/api/ClinicaSecurityApi.csproj -c Release --no-restore
dotnet test tests/api/ClinicaSecurityApi.Tests.csproj -c Release --no-restore
dotnet publish src/api/ClinicaSecurityApi.csproj -c Release --no-restore -p:PublishDir=../../artifacts/iis/
```

En Windows Server 2019 x64 actualizado se requiere IIS con el Hosting Bundle de ASP.NET Core 10. Usar un grupo de aplicaciones x64 con **Sin código administrado**, acceso de lectura a la carpeta publicada, el `web.config` generado y un enlace HTTPS con certificado confiable. El SDK solo se requiere donde se compila. [Sistemas compatibles con .NET 10](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) · [Alojamiento de ASP.NET Core en IIS](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0)

## Configuración externa

Entregar estas variables al proceso de la API mediante la configuración de despliegue de IIS, fuera del repositorio y de la carpeta publicada. En desarrollo pueden usarse secretos de usuario .NET.

| Variable | Valor requerido |
| --- | --- |
| `Jwt__Key` | Base64 de al menos 32 bytes aleatorios criptográficamente seguros. |
| `Jwt__Issuer`, `Jwt__Audience` | Identificadores previstos para esta API y sus clientes. |
| `Jwt__ExpirationMinutes` | Entero positivo, por ejemplo `30` en el laboratorio. |
| `DemoUsers__Clinico__Password`, `DemoUsers__Administrador__Password` | Contraseñas ficticias distintas, generadas para el laboratorio. |
| `ConnectionStrings__Clinica` | Conexión a `ClinicaDB_Lab`, por ejemplo `Server=SQL-LAB;Database=ClinicaDB_Lab;Integrated Security=true;Encrypt=true;TrustServerCertificate=false`. |

Crear un login y usuario SQL para la identidad de ejecución con permiso `SELECT` sobre `dbo.Pacientes`. En la misma máquina se puede usar la identidad virtual del grupo de IIS. Con SQL remoto, usar una identidad Windows reconocida por el servidor SQL. El certificado SQL debe corresponder al nombre del servidor en la conexión. Usar una cuenta administrativa separada para el ensayo de recuperación.
