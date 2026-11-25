:r ./guardia_laboratorio.sql
IF DB_ID(N'ClinicaDB_Lab') IS NOT NULL
    THROW 50003, 'ClinicaDB_Lab ya existe. Use una instancia descartable nueva.', 1;

GO
CREATE DATABASE [ClinicaDB_Lab];
GO
USE [ClinicaDB_Lab];
SET XACT_ABORT ON;
BEGIN TRANSACTION;
CREATE TABLE dbo.Pacientes
(
    Id int NOT NULL CONSTRAINT PK_Pacientes PRIMARY KEY CHECK (Id > 0),
    Nombre nvarchar(100) NOT NULL,
    Edad int NOT NULL CHECK (Edad BETWEEN 0 AND 130),
    MotivoConsulta nvarchar(255) NOT NULL
);
INSERT dbo.Pacientes (Id, Nombre, Edad, MotivoConsulta)
VALUES (1001, N'Paciente Demo Uno', 35, N'Consulta general ficticia'),
       (1002, N'Paciente Demo Dos', 42, N'Revisión médica ficticia');
EXEC sys.sp_addextendedproperty @name = N'ClinicaLabContract', @value = N'Pacientes-v1';
COMMIT;
SELECT Id, Nombre, Edad, MotivoConsulta FROM dbo.Pacientes ORDER BY Id;
