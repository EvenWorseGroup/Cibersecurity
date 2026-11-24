-- 1. Respaldo oficial
BACKUP DATABASE ClinicaDB TO DISK = 'C:\LabClinica\ClinicaDB.bak' WITH FORMAT, INIT;
GO

-- 2. Simular borrado de datos por incidente
USE ClinicaDB;
DELETE FROM Pacientes;
SELECT COUNT(*) AS RegistrosTrasIncidente FROM Pacientes; -- Debe dar 0
GO

-- 3. Restaurar respaldo oficial
USE master;
ALTER DATABASE ClinicaDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE ClinicaDB FROM DISK = 'C:\LabClinica\ClinicaDB.bak' WITH REPLACE;
ALTER DATABASE ClinicaDB SET MULTI_USER;
GO

-- 4. Validar que regresaron los 5 registros
USE ClinicaDB;
SELECT COUNT(*) AS RegistrosRecuperados FROM Pacientes; -- Debe dar 5
