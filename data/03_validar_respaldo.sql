:r ./guardia_laboratorio.sql
:r ./comprobar_respaldo.sql
RESTORE HEADERONLY FROM DISK = @respaldo WITH FILE = 1;
RESTORE FILELISTONLY FROM DISK = @respaldo WITH FILE = 1;
