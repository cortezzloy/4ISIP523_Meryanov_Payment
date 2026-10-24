-- =============================================
-- База данных: DB_Meryanov_Payment
-- Группа: 4ISIP-523 | Студент: Meryanov
-- Можно выполнять повторно
-- =============================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'DB_Meryanov_Payment')
BEGIN
    ALTER DATABASE DB_Meryanov_Payment SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DB_Meryanov_Payment;
END
GO

CREATE DATABASE DB_Meryanov_Payment;
GO

USE DB_Meryanov_Payment;
GO

CREATE TABLE [User] (
    ID INT IDENTITY(1,1) NOT NULL,
    Login NVARCHAR(50) NOT NULL,
    Password NVARCHAR(256) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT N'Пользователь',
    FIO NVARCHAR(150) NOT NULL,
    Photo VARBINARY(MAX) NULL,
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    IsBlocked BIT NOT NULL DEFAULT 0,
    BlockUntil DATETIME NULL,
    CONSTRAINT PK_User PRIMARY KEY (ID),
    CONSTRAINT UQ_User_Login UNIQUE (Login)
);
GO

CREATE TABLE Category (
    ID INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Category PRIMARY KEY (ID),
    CONSTRAINT UQ_Category_Name UNIQUE (Name)
);
GO

CREATE TABLE Payment (
    ID INT IDENTITY(1,1) NOT NULL,
    CategoryID INT NOT NULL,
    UserID INT NOT NULL,
    Date DATETIME NOT NULL DEFAULT GETDATE(),
    Name NVARCHAR(200) NOT NULL,
    Num INT NOT NULL CHECK (Num > 0),
    Price DECIMAL(18,2) NOT NULL CHECK (Price >= 0),
    CONSTRAINT PK_Payment PRIMARY KEY (ID),
    CONSTRAINT FK_Payment_Category FOREIGN KEY (CategoryID) 
        REFERENCES Category(ID) ON DELETE CASCADE,
    CONSTRAINT FK_Payment_User FOREIGN KEY (UserID) 
        REFERENCES [User](ID) ON DELETE CASCADE
);
GO

CREATE INDEX IX_Payment_UserID ON Payment(UserID);
CREATE INDEX IX_Payment_CategoryID ON Payment(CategoryID);
CREATE INDEX IX_Payment_Date ON Payment(Date);
CREATE INDEX IX_Payment_Name ON Payment(Name);
GO

INSERT INTO Category (Name) VALUES 
(N'Транспортные расходы'),
(N'Коммунальные услуги'),
(N'Развлечения'),
(N'Продукты питания'),
(N'Здоровье'),
(N'Образование'),
(N'Одежда'),
(N'Прочее');
GO

INSERT INTO [User] (Login, Password, Role, FIO, FailedLoginAttempts, IsBlocked)
VALUES 
(N'admin', N'8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', N'Администратор', N'Администратор Системы', 0, 0);

INSERT INTO [User] (Login, Password, Role, FIO, FailedLoginAttempts, IsBlocked)
VALUES 
(N'user', N'04f8996da763b7a969b1028ee3007569eaf3a635486ddab211d512c85b9df8fb', N'Пользователь', N'Мериянов Тестовый Пользователь', 0, 0);
GO

DECLARE @AdminId INT = (SELECT ID FROM [User] WHERE Login = N'admin');
DECLARE @UserId INT = (SELECT ID FROM [User] WHERE Login = N'user');
DECLARE @CatTransport INT = (SELECT ID FROM Category WHERE Name = N'Транспортные расходы');
DECLARE @CatCommunal INT = (SELECT ID FROM Category WHERE Name = N'Коммунальные услуги');
DECLARE @CatFun INT = (SELECT ID FROM Category WHERE Name = N'Развлечения');
DECLARE @CatFood INT = (SELECT ID FROM Category WHERE Name = N'Продукты питания');

INSERT INTO Payment (CategoryID, UserID, Date, Name, Num, Price) VALUES
(@CatTransport, @UserId, '2026-09-01', N'Метро', 1, 62.00),
(@CatTransport, @UserId, '2026-09-05', N'Такси', 2, 450.00),
(@CatCommunal, @UserId, '2026-09-10', N'Электричество', 1, 1850.50),
(@CatCommunal, @UserId, '2026-09-10', N'Вода', 1, 420.00),
(@CatFun, @UserId, '2026-09-15', N'Кино', 2, 800.00),
(@CatFood, @UserId, '2026-09-20', N'Продукты в магазине', 1, 3450.75),
(@CatTransport, @AdminId, '2026-09-02', N'Бензин', 1, 3200.00),
(@CatFun, @AdminId, '2026-09-12', N'Концерт', 1, 5500.00);
GO

PRINT N'База DB_Meryanov_Payment успешно создана.';
PRINT N'admin / admin | user / user';
GO
