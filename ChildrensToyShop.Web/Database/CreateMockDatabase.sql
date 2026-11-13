/* Little Lantern Toy Shop - SQL Server Express mock database setup */
USE master;
GO
IF DB_ID(N'ChildrensToyShopDb') IS NULL CREATE DATABASE ChildrensToyShopDb;
GO
USE ChildrensToyShopDb;
GO

IF OBJECT_ID(N'dbo.Reviews', N'U') IS NOT NULL DROP TABLE dbo.Reviews;
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.CartItems', N'U') IS NOT NULL DROP TABLE dbo.CartItems;
IF OBJECT_ID(N'dbo.Carts', N'U') IS NOT NULL DROP TABLE dbo.Carts;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users (
    Id int IDENTITY(1,1) PRIMARY KEY,
    DisplayName nvarchar(80) NOT NULL,
    Email nvarchar(200) NOT NULL UNIQUE,
    PasswordHash nvarchar(64) NOT NULL,
    Role nvarchar(20) NOT NULL CHECK (Role IN ('Admin','Customer')),
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE dbo.Products (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Sku nvarchar(40) NOT NULL UNIQUE,
    Title nvarchar(140) NOT NULL,
    Description nvarchar(2000) NOT NULL,
    Price decimal(18,2) NOT NULL CHECK (Price >= 0),
    AgeRange nvarchar(40) NOT NULL,
    Category nvarchar(60) NOT NULL,
    Brand nvarchar(60) NOT NULL,
    SafetyNotes nvarchar(240) NOT NULL DEFAULT '',
    SafetyCertification nvarchar(100) NOT NULL DEFAULT '',
    StockQuantity int NOT NULL CHECK (StockQuantity >= 0),
    ReorderThreshold int NOT NULL DEFAULT 10,
    Status nvarchar(20) NOT NULL DEFAULT 'Active' CHECK (Status IN ('Active','Recalled','Discontinued')),
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE dbo.Carts (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NOT NULL UNIQUE FOREIGN KEY REFERENCES dbo.Users(Id),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE dbo.CartItems (
    Id int IDENTITY(1,1) PRIMARY KEY,
    CartId int NOT NULL FOREIGN KEY REFERENCES dbo.Carts(Id) ON DELETE CASCADE,
    ProductId int NOT NULL FOREIGN KEY REFERENCES dbo.Products(Id),
    Quantity int NOT NULL CHECK (Quantity BETWEEN 1 AND 99),
    GiftWrap bit NOT NULL DEFAULT 0,
    CONSTRAINT UQ_CartItems UNIQUE (CartId, ProductId)
);
CREATE TABLE dbo.Orders (
    Id int IDENTITY(1,1) PRIMARY KEY,
    OrderNumber nvarchar(30) NOT NULL UNIQUE,
    UserId int NOT NULL FOREIGN KEY REFERENCES dbo.Users(Id),
    Status nvarchar(20) NOT NULL CHECK (Status IN ('PAID','PROCESSING','SHIPPED','DELIVERED','CANCELLED')),
    ShippingAddress nvarchar(300) NOT NULL,
    TrackingNumber nvarchar(80) NULL,
    Subtotal decimal(18,2) NOT NULL,
    Tax decimal(18,2) NOT NULL,
    GiftWrapFee decimal(18,2) NOT NULL,
    Total decimal(18,2) NOT NULL,
    IsArchived bit NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE dbo.OrderItems (
    Id int IDENTITY(1,1) PRIMARY KEY,
    OrderId int NOT NULL FOREIGN KEY REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    ProductId int NOT NULL,
    ProductTitle nvarchar(140) NOT NULL,
    Sku nvarchar(40) NOT NULL,
    UnitPrice decimal(18,2) NOT NULL,
    Quantity int NOT NULL CHECK (Quantity > 0),
    GiftWrap bit NOT NULL DEFAULT 0
);
CREATE TABLE dbo.Reviews (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ProductId int NOT NULL FOREIGN KEY REFERENCES dbo.Products(Id),
    UserId int NOT NULL FOREIGN KEY REFERENCES dbo.Users(Id),
    Rating int NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment nvarchar(1600) NOT NULL,
    ActualAgeSuitability nvarchar(40) NOT NULL,
    VisibilityStatus nvarchar(20) NOT NULL DEFAULT 'PENDING' CHECK (VisibilityStatus IN ('PENDING','APPROVED','FLAGGED')),
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Reviews UNIQUE (ProductId, UserId)
);
GO

/* Both mock accounts use password: 123456. SHA2_256 is used only to avoid clear-text storage in this demo. */
INSERT dbo.Users (DisplayName, Email, PasswordHash, Role) VALUES
('Toy Shop Admin', 'admin@toyshop.local', CONVERT(varchar(64), HASHBYTES('SHA2_256', '123456'), 2), 'Admin'),
('Sample Customer', 'customer@toyshop.local', CONVERT(varchar(64), HASHBYTES('SHA2_256', '123456'), 2), 'Customer');

INSERT dbo.Products (Sku, Title, Description, Price, AgeRange, Category, Brand, SafetyNotes, SafetyCertification, StockQuantity, ReorderThreshold, Status, IsActive) VALUES
('EDU-1001', '90 Piece Magnetic Building Set', 'Colorful magnetic tiles for imaginative construction and early STEM play.', 49.99, '3-5 years', 'Educational', 'Bright Minds', 'Contains magnets. Adult supervision recommended.', 'ASTM F963, CE', 38, 30, 'Active', 1),
('ROB-2001', 'Robo Pup Interactive Friend', 'A friendly programmable puppy with sound, movement, and simple coding games.', 79.99, '6-8 years', 'Robotics', 'Spark Labs', 'Rechargeable battery included.', 'ASTM F963', 20, 30, 'Active', 1),
('ART-3001', 'Ocean Paint Lab', 'Water-based paint and craft set for creative, ocean-inspired projects.', 24.50, '6-8 years', 'Arts and Crafts', 'Little Palette', 'Non-toxic materials. Wear a smock.', 'CE', 56, 15, 'Active', 1),
('OUT-4001', 'Junior Explorer Kit', 'Binoculars, compass, specimen jar and activity booklet for outdoor discovery.', 32.00, '8-10 years', 'Outdoor', 'Trail Tots', 'Small parts; not for children under 3.', 'ASTM F963', 12, 10, 'Active', 1),
('BLK-9001', 'Toddler Activity Blocks', 'Legacy stock intentionally marked as recalled for the safety-recall demonstration.', 19.99, '1-3 years', 'Toddler', 'Playful Steps', 'Choking hazard recall. Do not sell; retain for manufacturer return.', 'RECALLED', 3, 5, 'Recalled', 1);
GO

SELECT 'Mock database ready' AS Result, Email, Role FROM dbo.Users;
SELECT Sku, Title, StockQuantity, ReorderThreshold, Status FROM dbo.Products ORDER BY Sku;
