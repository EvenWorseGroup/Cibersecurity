DECLARE @directorio nvarchar(4000) = CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'));
IF NULLIF(@directorio, N'') IS NULL OR SUBSTRING(@directorio, 2, 2) <> N':\'
    THROW 50004, 'Configure una ruta local de respaldos en la instancia Windows.', 1;
DECLARE @respaldo nvarchar(4000) =
    CONCAT(@directorio, CASE WHEN RIGHT(@directorio, 1) = N'\' THEN N'' ELSE N'\' END, N'ClinicaDB_Lab.bak');
DECLARE @archivo table (Existe int, EsDirectorio int, DirectorioPadreExiste int);
INSERT @archivo EXEC master.dbo.xp_fileexist @respaldo;
IF NOT EXISTS (SELECT 1 FROM @archivo WHERE DirectorioPadreExiste = 1 AND EsDirectorio = 0)
    THROW 50005, 'La ruta del respaldo no es accesible.', 1;
