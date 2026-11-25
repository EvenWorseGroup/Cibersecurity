:r ./ruta_respaldo.sql
IF NOT EXISTS (SELECT 1 FROM @archivo WHERE Existe = 1)
    THROW 50009, 'No se encontró el archivo de respaldo.', 1;

RESTORE VERIFYONLY FROM DISK = @respaldo WITH FILE = 1, CHECKSUM, STOP_ON_ERROR;
