:ON ERROR EXIT
USE master;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF N'$(Modo)' <> N'CONFIRMO_LAB_DESCARTABLE'
    THROW 50000, 'Se requiere confirmación explícita del laboratorio.', 1;

IF ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')), N'') <> N'$(Instancia)'
    THROW 50001, 'La instancia no coincide con la esperada.', 1;

EXEC sys.sp_getapplock @Resource = N'ClinicaDB_Lab_Recuperacion',
    @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0,
    @DbPrincipal = 'dbo';
IF APPLOCK_MODE('dbo', 'ClinicaDB_Lab_Recuperacion', 'Session') <> 'Exclusive'
    THROW 50002, 'Hay otra operación de laboratorio en curso.', 1;
