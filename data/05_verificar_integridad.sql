:r ./guardia_laboratorio.sql
IF ISNULL(CONVERT(nvarchar(60), DATABASEPROPERTYEX(N'ClinicaDB_Lab_Ensayo', 'Status')), N'') <> N'ONLINE'
    THROW 50013, 'La base de ensayo debe estar en línea.', 1;
GO
USE [ClinicaDB_Lab_Ensayo];
IF NOT EXISTS
(
    SELECT 1 FROM sys.extended_properties
    WHERE class = 0 AND name = N'ClinicaLabContract' AND CONVERT(nvarchar(50), value) = N'Pacientes-v1'
)
    THROW 50014, 'La copia no tiene la marca del laboratorio.', 1;

-- DBCC genera error y sqlcmd -b devuelve un código distinto de cero si hay corrupción.
DBCC CHECKDB (N'ClinicaDB_Lab_Ensayo') WITH NO_INFOMSGS, ALL_ERRORMSGS;

DECLARE @esperados table (Id int, Nombre nvarchar(100), Edad int, MotivoConsulta nvarchar(255));
INSERT @esperados VALUES
    (1001, N'Paciente Demo Uno', 35, N'Consulta general ficticia'),
    (1002, N'Paciente Demo Dos', 42, N'Revisión médica ficticia');

IF (SELECT COUNT(*) FROM dbo.Pacientes) <> 2 OR EXISTS
(
    SELECT Id, Nombre, Edad, MotivoConsulta FROM @esperados
    EXCEPT
    SELECT Id, Nombre, Edad, MotivoConsulta FROM dbo.Pacientes
)
    THROW 50015, 'Los pacientes restaurados no coinciden con las semillas.', 1;

SELECT Id, Nombre, Edad, MotivoConsulta FROM dbo.Pacientes ORDER BY Id;
