:r ./guardia_laboratorio.sql
IF DB_ID(N'ClinicaDB_Lab_Ensayo') IS NOT NULL
    THROW 50010, 'El destino de ensayo ya existe.', 1;

:r ./comprobar_respaldo.sql
-- Acotar la restauración a los dos archivos que tienen MOVE explícito.
DECLARE @archivos table
(
    LogicalName nvarchar(128), PhysicalName nvarchar(260), [Type] char(1),
    FileGroupName nvarchar(128), Size numeric(20,0), MaxSize numeric(20,0),
    FileID bigint, CreateLSN numeric(25,0), DropLSN numeric(25,0),
    UniqueID uniqueidentifier, ReadOnlyLSN numeric(25,0), ReadWriteLSN numeric(25,0),
    BackupSizeInBytes bigint, SourceBlockSize int, FileGroupID int,
    LogGroupGUID uniqueidentifier, DifferentialBaseLSN numeric(25,0),
    DifferentialBaseGUID uniqueidentifier, IsReadOnly bit, IsPresent bit,
    TDEThumbprint varbinary(32), SnapshotURL nvarchar(360)
);
INSERT @archivos EXEC sp_executesql
    N'RESTORE FILELISTONLY FROM DISK = @path WITH FILE = 1;',
    N'@path nvarchar(4000)', @path = @respaldo;

IF (SELECT COUNT(*) FROM @archivos) <> 2
   OR NOT EXISTS (SELECT 1 FROM @archivos WHERE LogicalName = N'ClinicaDB_Lab' AND [Type] = 'D' AND IsPresent = 1)
   OR NOT EXISTS (SELECT 1 FROM @archivos WHERE LogicalName = N'ClinicaDB_Lab_log' AND [Type] = 'L' AND IsPresent = 1)
    THROW 50016, 'El respaldo no contiene los dos archivos esperados del laboratorio.', 1;

DECLARE @datos nvarchar(4000) = CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultDataPath'));
DECLARE @logs nvarchar(4000) = CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultLogPath'));
IF NULLIF(@datos, N'') IS NULL OR NULLIF(@logs, N'') IS NULL
   OR SUBSTRING(@datos, 2, 2) <> N':\' OR SUBSTRING(@logs, 2, 2) <> N':\'
    THROW 50011, 'Configure rutas locales de datos y logs.', 1;

DECLARE @mdf nvarchar(4000) = CONCAT(@datos, CASE WHEN RIGHT(@datos, 1) = N'\' THEN N'' ELSE N'\' END, N'ClinicaDB_Lab_Ensayo.mdf');
DECLARE @ldf nvarchar(4000) = CONCAT(@logs, CASE WHEN RIGHT(@logs, 1) = N'\' THEN N'' ELSE N'\' END, N'ClinicaDB_Lab_Ensayo_log.ldf');
DECLARE @destinos table (Existe int, EsDirectorio int, DirectorioPadreExiste int);
INSERT @destinos EXEC master.dbo.xp_fileexist @mdf;
INSERT @destinos EXEC master.dbo.xp_fileexist @ldf;

IF (SELECT COUNT(*) FROM @destinos WHERE Existe = 0 AND EsDirectorio = 0 AND DirectorioPadreExiste = 1) <> 2
   OR EXISTS (SELECT 1 FROM sys.master_files WHERE physical_name IN (@mdf, @ldf))
    THROW 50012, 'Los archivos de ensayo ya existen o sus rutas no están disponibles.', 1;

RESTORE VERIFYONLY FROM DISK = @respaldo WITH FILE = 1, CHECKSUM, STOP_ON_ERROR,
    MOVE N'ClinicaDB_Lab' TO @mdf,
    MOVE N'ClinicaDB_Lab_log' TO @ldf;

RESTORE DATABASE [ClinicaDB_Lab_Ensayo] FROM DISK = @respaldo
    WITH FILE = 1, CHECKSUM, STOP_ON_ERROR, RECOVERY,
    MOVE N'ClinicaDB_Lab' TO @mdf,
    MOVE N'ClinicaDB_Lab_log' TO @ldf;
