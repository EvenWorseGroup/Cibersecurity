:r ./guardia_laboratorio.sql
IF ISNULL(CONVERT(nvarchar(60), DATABASEPROPERTYEX(N'ClinicaDB_Lab', 'Status')), N'') <> N'ONLINE'
    THROW 50006, 'ClinicaDB_Lab debe estar en línea.', 1;
IF NOT EXISTS
(
    SELECT 1 FROM ClinicaDB_Lab.sys.extended_properties
    WHERE class = 0 AND name = N'ClinicaLabContract' AND CONVERT(nvarchar(50), value) = N'Pacientes-v1'
)
    THROW 50007, 'El origen no tiene la marca del laboratorio.', 1;

:r ./ruta_respaldo.sql
IF EXISTS (SELECT 1 FROM @archivo WHERE Existe = 1)
    THROW 50008, 'El respaldo ya existe. Use un directorio descartable nuevo.', 1;

BACKUP DATABASE [ClinicaDB_Lab] TO DISK = @respaldo
    WITH COPY_ONLY, CHECKSUM, STOP_ON_ERROR, NOINIT, NAME = N'ClinicaDB_Lab completo';
