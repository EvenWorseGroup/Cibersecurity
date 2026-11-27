# Informe de integración y QA de seguridad

**Resultado del 27 de noviembre de 2026. PASS local. SKIP para las validaciones de infraestructura.** Entrega prevista para el lunes 30 de noviembre de 2026. La compilación Release y las 19 pruebas HTTP pasaron. El paquete se generó localmente y queda pendiente validarlo en IIS con SQL Server.

## Trazabilidad

- Repositorio `EvenWorseGroup/Cibersecurity`. Árbol de trabajo limpio al inicio en `feature/qa-security-integracion`.
- Rama de trabajo creada según el nombre solicitado, `feature/qa-security-integration`, desde `integration/api-sql-recovery`. HEAD de partida `3a40d99ac7d805e07c5dd20757e2cbccce8c37b2`, coincidente con la referencia remota local de integración.
- Cambios locales sin commit, push, merge ni revert. La VM permaneció fuera del alcance. Su comprobación HTTPS previa fue reportada por el solicitante.

## Inventario y contrato inspeccionado

| Componente | Alcance |
| --- | --- |
| Arranque y configuración | `Program.cs`, ambos `appsettings`, perfil de lanzamiento y configuración externa para JWT, usuarios demo, SQL y HTTPS. |
| API | Controladores Health, Auth, Pacientes y Admin. Modelos de entrada, respuesta, paciente, usuario y auditoría. |
| Servicios y persistencia | Emisión y validación JWT, Identity PasswordHasher, usuarios demo en memoria, lectura SQL parametrizada por entero, middleware y almacén de auditoría. |
| SQL | Los ocho archivos de `data/`. Esquema, semillas, guardia de instancia, respaldo con checksum, validación, restauración con reubicación y verificación de integridad. |
| Dependencias y pruebas | Proyectos `.csproj` de API y pruebas, referencias directas y transitivas, `ApiFactory` y suite HTTP. |
| Operación | README, informe previo, playbook, patrones de Git y nombres de archivos del historial alcanzable por referencias locales. |

| Ruta | Comportamiento verificado localmente |
| --- | --- |
| `GET /health` | Público. Devuelve 200, estado y nombre del servicio. Comprueba que el proceso responde. |
| `POST /auth/login` | Público. Devuelve 200 con token Bearer y vencimiento, 400 para campos obligatorios vacíos y 401 genérico para contraseña incorrecta. |
| `GET /api/pacientes/{id}` | 401 si falta JWT o es inválido. El clínico lee su asignación 1001. El expediente ajeno y el inexistente producen el mismo 404. El administrador puede leer expedientes existentes. El ID cero devuelve 400. |
| `GET /api/admin/auditoria` | 401 sin JWT o con JWT inválido, 403 para el clínico y 200 para el administrador. |

El controlador de pacientes verifica rol y asignación antes de llamar a SQL. El rol administrativo da acceso a cualquier expediente existente. Los SDK y pruebas locales usaron la cadena HTTP de ASP.NET Core en memoria, con URL HTTPS de prueba y sin negociación TLS real.

## Hallazgos por criticidad

| Prioridad | Evidencia y tratamiento |
| --- | --- |
| Alta · confirmada en código | La auditoría acumulaba registros y devolvía la colección entera sin un límite. El consumo de memoria aumentaba con cada solicitud auditada, incluso anónima. Se limita la vista administrativa a los últimos 1 000 eventos por proceso. |
| Media · confirmada | La configuración JWT conservaba el comportamiento predeterminado que permite detalles de rechazo en el desafío Bearer. Se desactivaron los detalles, se restringió el algoritmo al HS256 del emisor y se comprobó el 401 sin parámetros ni cuerpo. |
| Media · confirmada en código | La verificación de duración JWT y de contraseñas externas podía ocurrir en la primera solicitud de login. Ahora el arranque exige credenciales configuradas, duración positiva, emisor y audiencia no vacíos, y una clave válida. |
| Media · confirmada | La vista de auditoría empezaba con eventos ficticios e IDs de pacientes en rutas. El login correcto quedaba fuera del logger estructurado y una variante de la ruta podía generar un segundo evento. La vista empieza vacía y el evento real se registra una vez con identidad canónica y ruta normalizada. |
| Media · riesgo condicionado | Un fallo inesperado de lectura llegaba al manejo de errores del host, cuya salida depende del entorno. Se verificó con un fallo simulado que los errores capturados antes de iniciar la respuesta entregan un 500 genérico y registran tipo y correlación sin mensaje ni traza de la excepción. No se observó una filtración real en IIS. |
| Baja · confirmada | Las pruebas tenían contraseñas ficticias constantes y una impresión de diagnóstico JWT. Se sustituyeron por credenciales efímeras de prueba y aserciones sin imprimir tokens. |

La revisión por patrones y nombres de archivo no identificó credenciales reales en el árbol auditado. Los `appsettings` versionados contienen configuración general de logging y hosts. Las coincidencias históricas de patrones se limitaron a commits de API e integración que contienen referencias de configuración y datos de prueba. La revisión heurística no certifica la ausencia de secretos en todo el historial. Los artefactos locales de compilación y publicación están cubiertos por `.gitignore`. No se inspeccionaron secretos de usuario externos ni configuraciones de la VM.

## Archivos modificados

| Archivo | Justificación |
| --- | --- |
| `src/api/Program.cs` | Configuración de JWT y validación temprana de credenciales externas. |
| `src/api/Services/AuthService.cs`, `src/api/Controllers/AuthController.cs` | Registrar la cuenta autenticada con su identidad canónica sin cambiar el JSON de login. |
| `src/api/Data/AuditoriaStore.cs` | Retirar semillas de auditoría, limitar la memoria y emitir eventos estructurados desde un solo lugar. |
| `src/api/Middleware/AuditoriaMiddleware.cs` | Mantener correlación en el 500, sanitizar errores capturados y deduplicar el login usando la plantilla de ruta. |
| `tests/api/ApiFactory.cs`, `tests/api/ApiIntegrationTests.cs` | Credenciales efímeras, simulación de lectura y fallo de SQL, captura de logs y aserciones de contrato. |
| `INFORME_INTEGRACION.md` | Registrar hallazgos, evidencia local y pendientes de despliegue. |

Se conservaron los métodos, rutas y campos de respuesta exitosos. Los cambios observables son el desafío Bearer sin diagnóstico, el 500 genérico para fallos capturados y la ventana de eventos recientes en la auditoría. No se añadieron dependencias.

## Validación

| Entorno | Verificación | Resultado |
| --- | --- | --- |
| Local, base sin cambios | Suite HTTP original | **PASS** · 14 aprobadas. |
| Local, SDK 10.0.403 | `dotnet restore tests/api/ClinicaSecurityApi.Tests.csproj` | **PASS** |
| Local | `dotnet build tests/api/ClinicaSecurityApi.Tests.csproj -c Release --no-restore` | **PASS** · 0 advertencias, 0 errores. |
| Local | `dotnet test tests/api/ClinicaSecurityApi.Tests.csproj -c Release --no-build --no-restore` | **PASS** · 19 aprobadas, 0 fallidas, 0 omitidas. |
| Local | `dotnet list <proyecto> package --vulnerable --include-transitive --no-restore` para API y pruebas | **PASS** · NuGet no reportó avisos para los proyectos en los orígenes consultados. |
| Local | `dotnet publish src/api/ClinicaSecurityApi.csproj -c Release --no-build --no-restore -p:PublishDir=../../artifacts/iis-qa/` | **PASS** · DLL de API, SqlClient y `web.config` con `AspNetCoreModuleV2` e `inprocess` verificados. |
| SQL Server local | Consulta real, permisos y recuperación | **SKIP** · Sin servicio SQL Server ni `sqlcmd` detectados. Las pruebas usan un repositorio ficticio. |
| VMware | Arranque bajo IIS, HTTPS real, SQL y recuperación | **SKIP** · VM apagada y excluida del encargo. |

La suite previa se concentraba en estados HTTP. La suite final añade comprobaciones de JSON de salud, plazo de vencimiento, campos de expediente, indistinguibilidad de los 404, JWT malformado en ambas rutas protegidas, JWT legítimo de otro host con clave independiente, 500 saneado y auditoría correlacionada. No quedaron fallos en la ejecución final. La vigencia y la validación de emisor y audiencia se revisaron en código y la suite no incluye casos negativos independientes para cada parámetro.

## Riesgos residuales

- La auditoría administrativa es volátil por proceso. La retención de los logs estructurados depende del proveedor configurado en IIS. Los fallos de login se auditan sin registrar el nombre presentado por el cliente. No hay limitación de intentos implementada por la aplicación. La exposición efectiva depende de los controles perimetrales.
- `/health` no verifica SQL. La disponibilidad, TLS y permisos de SQL requieren una lectura autorizada con la identidad real. La suite local no prueba la recuperación, los metadatos del respaldo ni las guardias del servidor.
- El ensayo SQL puede dejar un destino parcial si se interrumpe. El playbook requiere comprobar la procedencia del respaldo y usar un destino descartable. Reservar permisos elevados para el operador del ensayo.
- Los errores posteriores al inicio de la respuesta quedan a cargo del host. Verificar su configuración de diagnóstico y evitar trazas de petición con cabeceras, cuerpos o identificadores de pacientes.

## Checklist pendiente en IIS y VMware

- [ ] Confirmar Windows Server 2019 x64 actualizado, Hosting Bundle de .NET 10, grupo x64 sin código administrado y permisos de lectura al paquete.
- [ ] Cargar configuración externa de JWT, cuentas demo y SQL según el README. Usar contraseñas distintas y proteger su acceso. Confirmar que el sitio arranca en `Production` y que los diagnósticos detallados están deshabilitados.
- [ ] Verificar nombre, vigencia, confianza y enlace del certificado HTTPS. Repetir `GET /health` y registrar estado y correlación.
- [ ] Verificar conexión SQL cifrada y permiso `SELECT` sobre `dbo.Pacientes` con la identidad de IIS. Confirmar que esa identidad carece de permisos de escritura y restauración.
- [ ] Repetir la matriz de login y roles, 401, 403, 404 y la lectura real de ambos expedientes ficticios. Verificar la recepción de eventos de nivel Información y su correlación. Guardar evidencia sin cuerpos médicos ni credenciales.
- [ ] Ejecutar el playbook solo en una instancia descartable autorizada. Verificar checksum, guardias, destino nuevo, `DBCC CHECKDB` y semillas. Repetir las lecturas con una instancia separada de la API apuntando a la copia restaurada.

Referencias operativas. [Configuración y contrato](README.md) · [Playbook SQL](runbooks/playbook_recuperacion.md)

## Correlación defensiva con MITRE ATT&CK

Correlacionar por hora UTC, ruta normalizada, estado, resultado e ID de correlación. El GUID puede provenir del cliente. Verificar su unicidad en el conjunto analizado y combinarlo con la hora y evidencia independiente de IIS o del proxy autorizado. Preservar los eventos fuera del proceso, con acceso restringido y sin cabeceras de autorización, cuerpos de login, datos clínicos ni rutas de expedientes con IDs.

- **T1110, Brute Force**. Considerar la técnica cuando una serie de fallos de login y la telemetría de origen sostengan un patrón de intentos sistemáticos. Un 401 aislado o un JWT malformado es señal insuficiente. Reservar las subtécnicas para evidencia del método. [MITRE ATT&CK T1110](https://attack.mitre.org/techniques/T1110/)
- **T1078, Valid Accounts**. Requiere evidencia independiente de uso indebido de una cuenta válida, por ejemplo una sesión exitosa corroborada por el origen y por el titular o la investigación del incidente. Un login exitoso, un 403 o un 404 por sí solos no acreditan esta técnica. [MITRE ATT&CK DET0560](https://attack.mitre.org/detectionstrategies/DET0560/)

Las pruebas de QA generaron eventos sintéticos. No se atribuyó ninguna técnica a un incidente real.
