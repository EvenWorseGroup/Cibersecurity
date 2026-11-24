IF DB_ID('ClinicaDB') IS NULL CREATE DATABASE ClinicaDB;
GO
USE ClinicaDB;
GO

DROP TABLE IF EXISTS Pacientes;
GO

CREATE TABLE Pacientes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NombreFicticio NVARCHAR(100) NOT NULL,
    DatoClinicoFicticio NVARCHAR(255) NOT NULL,
    PerfilAutorizado NVARCHAR(50) NOT NULL
);
GO

INSERT INTO Pacientes (NombreFicticio, DatoClinicoFicticio, PerfilAutorizado)
VALUES 
    ('Carlos Mendoza (Ficticio)', 'Alergia leve a penicilina', 'clinico1'),
    ('Laura Gómez (Ficticio)', 'Hipertensión controlada', 'clinico1'),
    ('Roberto Silva (Ficticio)', 'Fractura de radio recuperada', 'clinico2'),
    ('Ana Torres (Ficticio)', 'Consulta general sin antecedentes', 'clinico1'),
    ('Miguel Ángel Ruiz (Ficticio)', 'Seguimiento nutricional', 'clinico2');
GO

SELECT COUNT(*) AS TotalRegistros FROM Pacientes;
