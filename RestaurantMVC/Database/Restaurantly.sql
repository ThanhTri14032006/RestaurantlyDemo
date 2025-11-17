-- Restaurantly SQL Server Schema & Seed
-- Khởi tạo toàn bộ schema và dữ liệu mẫu cho website chạy trên SQL Server

-- Tạo database nếu chưa có (đồng bộ với appsettings.json: Database=RestaurantDB)
IF DB_ID('RestaurantDB') IS NULL
BEGIN
  CREATE DATABASE RestaurantDB;
END
GO
USE RestaurantDB;
GO

/*
 Tables:
 - Users
 - MenuItems
 - Bookings
 - Reviews (FK -> MenuItems, ON DELETE CASCADE)
 - Orders
 - OrderItems (FK -> Orders ON DELETE CASCADE, FK -> MenuItems ON DELETE NO ACTION)
*/

-- Gỡ bảng theo thứ tự phụ thuộc để tránh lỗi FK
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Reviews', 'U') IS NOT NULL DROP TABLE dbo.Reviews;
IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL DROP TABLE dbo.Bookings;
IF OBJECT_ID('dbo.MenuItems', 'U') IS NOT NULL DROP TABLE dbo.MenuItems;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- USERS
CREATE TABLE dbo.Users (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  Username        NVARCHAR(100) NOT NULL,
  Email           NVARCHAR(200) NOT NULL,
  Password        NVARCHAR(500) NOT NULL,
  FullName        NVARCHAR(200) NULL,
  Phone           NVARCHAR(20) NULL,
  Role            INT NOT NULL DEFAULT(0),         -- Customer=0, Staff=1, Manager=2, Admin=3
  IsActive        BIT NOT NULL DEFAULT(1),
  CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  LastLoginAt     DATETIME2 NULL
);
CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(Username);
CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
GO

-- MENUITEMS
CREATE TABLE dbo.MenuItems (
  Id            INT IDENTITY(1,1) PRIMARY KEY,
  Name          NVARCHAR(200) NOT NULL,
  Description   NVARCHAR(1000) NULL,
  Price         DECIMAL(18,2) NOT NULL,
  Category      NVARCHAR(100) NULL,
  ImageUrl      NVARCHAR(500) NULL,
  IsAvailable   BIT NOT NULL DEFAULT(1),
  CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- BOOKINGS
CREATE TABLE dbo.Bookings (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  CustomerName    NVARCHAR(200) NOT NULL,
  Email           NVARCHAR(200) NOT NULL,
  Phone           NVARCHAR(20) NOT NULL,
  BookingDate     DATETIME2 NOT NULL,
  BookingTime     DATETIME2 NOT NULL,
  PartySize       INT NOT NULL,
  SpecialRequests NVARCHAR(1000) NULL,
  Status          INT NOT NULL DEFAULT(0), -- Pending=0, Confirmed=1, Completed=2, Cancelled=3 (theo EF: có thể khác thứ tự)
  CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  AdminNotes      NVARCHAR(1000) NULL
);
GO

-- REVIEWS
CREATE TABLE dbo.Reviews (
  Id            INT IDENTITY(1,1) PRIMARY KEY,
  MenuItemId    INT NOT NULL,
  CustomerName  NVARCHAR(100) NOT NULL,
  Email         NVARCHAR(200) NOT NULL,
  Rating        INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
  Comment       NVARCHAR(1000) NULL,
  CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  IsApproved    BIT NOT NULL DEFAULT(0)
);
ALTER TABLE dbo.Reviews
  ADD CONSTRAINT FK_Reviews_MenuItems
      FOREIGN KEY (MenuItemId) REFERENCES dbo.MenuItems(Id)
      ON DELETE CASCADE; -- phù hợp cấu hình EF
GO

-- ORDERS
CREATE TABLE dbo.Orders (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  CustomerName    NVARCHAR(100) NOT NULL,
  Email           NVARCHAR(100) NOT NULL,
  Phone           NVARCHAR(20) NOT NULL,
  DeliveryAddress NVARCHAR(500) NULL,
  TotalAmount     DECIMAL(10,2) NOT NULL,
  Status          INT NOT NULL DEFAULT(0),   -- Pending..Cancelled
  OrderType       INT NOT NULL DEFAULT(0),   -- Delivery=0, Pickup=1
  CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  Notes           NVARCHAR(500) NULL
);
GO

-- ORDERITEMS
CREATE TABLE dbo.OrderItems (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  OrderId     INT NOT NULL,
  MenuItemId  INT NOT NULL,
  Quantity    INT NOT NULL CHECK (Quantity BETWEEN 1 AND 100),
  UnitPrice   DECIMAL(10,2) NOT NULL,
  TotalPrice  DECIMAL(10,2) NOT NULL
);
ALTER TABLE dbo.OrderItems
  ADD CONSTRAINT FK_OrderItems_Orders
      FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id)
      ON DELETE CASCADE;
ALTER TABLE dbo.OrderItems
  ADD CONSTRAINT FK_OrderItems_MenuItems
      FOREIGN KEY (MenuItemId) REFERENCES dbo.MenuItems(Id)
      ON DELETE NO ACTION; -- Restrict theo cấu hình EF
GO

-- SEED DATA
-- Admin mặc định
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username='admin')
BEGIN
  INSERT INTO dbo.Users (Username, Email, Password, FullName, Role, IsActive, CreatedAt, UpdatedAt)
  VALUES ('admin', 'admin@restaurant.com', 'admin123', 'Administrator', 3, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

-- Thực đơn mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.MenuItems)
BEGIN
  INSERT INTO dbo.MenuItems (Name, Description, Price, Category, ImageUrl, IsAvailable, CreatedAt) VALUES
  (N'Phở Bò', N'Phở bò truyền thống với nước dùng đậm đà', 65000, N'Món chính', '/images/pho-bo.jpg', 1, SYSUTCDATETIME()),
  (N'Bún Chả', N'Bún chả Hà Nội với thịt nướng thơm ngon', 55000, N'Món chính', '/images/bun-cha.jpg', 1, SYSUTCDATETIME()),
  (N'Gỏi Cuốn', N'Gỏi cuốn tôm thịt tươi ngon', 35000, N'Khai vị', '/images/goi-cuon.jpg', 1, SYSUTCDATETIME()),
  (N'Chả Cá Lã Vọng', N'Chả cá truyền thống với thì là và hành', 85000, N'Món chính', '/images/cha-ca.jpg', 1, SYSUTCDATETIME()),
  (N'Bánh Mì', N'Bánh mì thịt nguội với rau củ tươi', 25000, N'Món nhẹ', '/images/banh-mi.jpg', 1, SYSUTCDATETIME()),
  (N'Cà Phê Sữa Đá', N'Cà phê sữa đá truyền thống', 20000, N'Đồ uống', '/images/ca-phe.jpg', 1, SYSUTCDATETIME());
END
GO

-- Một vài đặt bàn mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.Bookings)
BEGIN
  INSERT INTO dbo.Bookings (CustomerName, Email, Phone, BookingDate, BookingTime, PartySize, SpecialRequests, Status, CreatedAt, AdminNotes) VALUES
  (N'Nguyễn Văn A', 'a@example.com', '0901234567', DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), DATEADD(HOUR, 19, CAST(GETDATE() AS DATE)), 2, N'Bàn gần cửa sổ', 1, SYSUTCDATETIME(), NULL),
  (N'Trần Thị B', 'b@example.com', '0907654321', DATEADD(DAY, 2, CAST(GETDATE() AS DATE)), DATEADD(HOUR, 18, CAST(GETDATE() AS DATE)), 4, N'Không cay', 0, SYSUTCDATETIME(), NULL),
  (N'Lê Văn C', 'c@example.com', '0912345678', DATEADD(DAY, -1, CAST(GETDATE() AS DATE)), DATEADD(HOUR, 20, CAST(GETDATE() AS DATE)), 3, NULL, 2, SYSUTCDATETIME(), N'Khách quen');
END
GO

-- Một đơn hàng mẫu + chi tiết
IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
  INSERT INTO dbo.Orders (CustomerName, Email, Phone, DeliveryAddress, TotalAmount, Status, OrderType, CreatedAt, UpdatedAt, Notes)
  VALUES (N'Phạm Minh D', 'd@example.com', '0930000000', N'123 Đường Lê Lợi, Q1', 150000, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), NULL);

  DECLARE @OrderId INT = SCOPE_IDENTITY();

  -- Giả sử đã có MenuItems từ seed ở trên
  INSERT INTO dbo.OrderItems (OrderId, MenuItemId, Quantity, UnitPrice, TotalPrice)
  SELECT @OrderId, Id, 1, Price, Price FROM dbo.MenuItems WHERE Name IN (N'Phở Bò', N'Bún Chả');
END
GO

-- Đánh giá mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.Reviews)
BEGIN
  INSERT INTO dbo.Reviews (MenuItemId, CustomerName, Email, Rating, Comment, CreatedAt, IsApproved)
  SELECT TOP 1 Id, N'Khách hàng X', 'x@example.com', 5, N'Rất ngon!', SYSUTCDATETIME(), 1 FROM dbo.MenuItems ORDER BY Id;

  INSERT INTO dbo.Reviews (MenuItemId, CustomerName, Email, Rating, Comment, CreatedAt, IsApproved)
  SELECT TOP 1 Id, N'Khách hàng Y', 'y@example.com', 4, N'Ổn, sẽ quay lại', SYSUTCDATETIME(), 1 FROM dbo.MenuItems ORDER BY Id DESC;
END
GO

-- Khuyến nghị: tạo index phục vụ truy vấn phổ biến
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bookings_BookingDate')
BEGIN
  CREATE INDEX IX_Bookings_BookingDate ON dbo.Bookings(BookingDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId')
BEGIN
  CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
END
GO

-- Hoàn tất
PRINT 'RestaurantDB schema + seed đã khởi tạo thành công.';
GO

-- Gợi ý: nếu muốn dữ liệu đơn hàng/mục đơn hàng mẫu, có thể thêm sau khi MenuItems đã có.














                                      --///////////--













-- =====================================================
-- Restaurantly SQL Server Schema & Seed (Fixed Version)
-- Khởi tạo toàn bộ schema và dữ liệu mẫu cho website Restaurantly
-- =====================================================

-- Tạo database nếu chưa có
IF DB_ID('RestaurantDB') IS NULL
BEGIN
  CREATE DATABASE RestaurantDB;
END
GO
USE RestaurantDB;
GO

-- =====================================================
-- XÓA BẢNG NẾU CÓ (theo thứ tự phụ thuộc)
-- =====================================================
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Reviews', 'U') IS NOT NULL DROP TABLE dbo.Reviews;
IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL DROP TABLE dbo.Bookings;
IF OBJECT_ID('dbo.MenuItems', 'U') IS NOT NULL DROP TABLE dbo.MenuItems;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- =====================================================
-- USERS
-- =====================================================
CREATE TABLE dbo.Users (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  Username        NVARCHAR(100) NOT NULL,
  Email           NVARCHAR(200) NOT NULL,
  Password        NVARCHAR(500) NOT NULL,
  FullName        NVARCHAR(200) NULL,
  Phone           NVARCHAR(20) NULL,
  Role            INT NOT NULL DEFAULT(0),         -- 0=Customer, 1=Staff, 2=Manager, 3=Admin
  IsActive        BIT NOT NULL DEFAULT(1),
  CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  LastLoginAt     DATETIME2 NULL
);
CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(Username);
CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
GO

-- =====================================================
-- MENU ITEMS
-- =====================================================
CREATE TABLE dbo.MenuItems (
  Id            INT IDENTITY(1,1) PRIMARY KEY,
  Name          NVARCHAR(200) NOT NULL,
  Description   NVARCHAR(1000) NULL,
  Price         DECIMAL(18,2) NOT NULL,
  Category      NVARCHAR(100) NULL,
  ImageUrl      NVARCHAR(500) NULL,
  IsAvailable   BIT NOT NULL DEFAULT(1),
  CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- =====================================================
-- BOOKINGS
-- =====================================================
CREATE TABLE dbo.Bookings (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  CustomerName    NVARCHAR(200) NOT NULL,
  Email           NVARCHAR(200) NOT NULL,
  Phone           NVARCHAR(20) NOT NULL,
  BookingDate     DATETIME2 NOT NULL,
  BookingTime     DATETIME2 NOT NULL,
  PartySize       INT NOT NULL,
  SpecialRequests NVARCHAR(1000) NULL,
  Status          INT NOT NULL DEFAULT(0), -- 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled
  CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  AdminNotes      NVARCHAR(1000) NULL
);
GO

-- =====================================================
-- REVIEWS
-- =====================================================
CREATE TABLE dbo.Reviews (
  Id            INT IDENTITY(1,1) PRIMARY KEY,
  MenuItemId    INT NOT NULL,
  CustomerName  NVARCHAR(100) NOT NULL,
  Email         NVARCHAR(200) NOT NULL,
  Rating        INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
  Comment       NVARCHAR(1000) NULL,
  CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  IsApproved    BIT NOT NULL DEFAULT(0)
);
ALTER TABLE dbo.Reviews
  ADD CONSTRAINT FK_Reviews_MenuItems
  FOREIGN KEY (MenuItemId) REFERENCES dbo.MenuItems(Id)
  ON DELETE CASCADE;
GO

-- =====================================================
-- ORDERS
-- =====================================================
CREATE TABLE dbo.Orders (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  CustomerName    NVARCHAR(100) NOT NULL,
  Email           NVARCHAR(100) NOT NULL,
  Phone           NVARCHAR(20) NOT NULL,
  DeliveryAddress NVARCHAR(500) NULL,
  TotalAmount     DECIMAL(10,2) NOT NULL,
  Status          INT NOT NULL DEFAULT(0),   -- 0=Pending
  OrderType       INT NOT NULL DEFAULT(0),   -- 0=Delivery, 1=Pickup
  CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  Notes           NVARCHAR(500) NULL
);
GO

-- =====================================================
-- ORDER ITEMS
-- =====================================================
CREATE TABLE dbo.OrderItems (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  OrderId     INT NOT NULL,
  MenuItemId  INT NOT NULL,
  Quantity    INT NOT NULL CHECK (Quantity BETWEEN 1 AND 100),
  UnitPrice   DECIMAL(10,2) NOT NULL,
  TotalPrice  DECIMAL(10,2) NOT NULL
);
ALTER TABLE dbo.OrderItems
  ADD CONSTRAINT FK_OrderItems_Orders
  FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id)
  ON DELETE CASCADE;

ALTER TABLE dbo.OrderItems
  ADD CONSTRAINT FK_OrderItems_MenuItems
  FOREIGN KEY (MenuItemId) REFERENCES dbo.MenuItems(Id)
  ON DELETE NO ACTION;
GO

-- =====================================================
-- SEED DATA
-- =====================================================
-- Admin mặc định
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username='admin')
BEGIN
  INSERT INTO dbo.Users (Username, Email, Password, FullName, Role)
  VALUES ('admin', 'admin@restaurant.com', 'admin123', N'Administrator', 3);
END
GO

-- Menu mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.MenuItems)
BEGIN
  INSERT INTO dbo.MenuItems (Name, Description, Price, Category, ImageUrl) VALUES
  (N'Phở Bò', N'Phở bò truyền thống với nước dùng đậm đà', 65000, N'Món chính', '/images/pho-bo.jpg'),
  (N'Bún Chả', N'Bún chả Hà Nội với thịt nướng thơm ngon', 55000, N'Món chính', '/images/bun-cha.jpg'),
  (N'Gỏi Cuốn', N'Gỏi cuốn tôm thịt tươi ngon', 35000, N'Khai vị', '/images/goi-cuon.jpg'),
  (N'Chả Cá Lã Vọng', N'Chả cá truyền thống với thì là và hành', 85000, N'Món chính', '/images/cha-ca.jpg'),
  (N'Bánh Mì', N'Bánh mì thịt nguội với rau củ tươi', 25000, N'Món nhẹ', '/images/banh-mi.jpg'),
  (N'Cà Phê Sữa Đá', N'Cà phê sữa đá truyền thống', 20000, N'Đồ uống', '/images/ca-phe.jpg');
END
GO

-- Đặt bàn mẫu (đã fix lỗi DATEADD)
IF NOT EXISTS (SELECT 1 FROM dbo.Bookings)
BEGIN
  INSERT INTO dbo.Bookings (CustomerName, Email, Phone, BookingDate, BookingTime, PartySize, SpecialRequests, Status, AdminNotes)
  VALUES
  (N'Nguyễn Văn A', 'a@example.com', '0901234567',
      DATEADD(DAY, 1, GETDATE()), DATEADD(HOUR, 19, GETDATE()), 2, N'Bàn gần cửa sổ', 1, NULL),
  (N'Trần Thị B', 'b@example.com', '0907654321',
      DATEADD(DAY, 2, GETDATE()), DATEADD(HOUR, 18, GETDATE()), 4, N'Không cay', 0, NULL),
  (N'Lê Văn C', 'c@example.com', '0912345678',
      DATEADD(DAY, -1, GETDATE()), DATEADD(HOUR, 20, GETDATE()), 3, NULL, 2, N'Khách quen');
END
GO

-- Đơn hàng mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
  INSERT INTO dbo.Orders (CustomerName, Email, Phone, DeliveryAddress, TotalAmount)
  VALUES (N'Phạm Minh D', 'd@example.com', '0930000000', N'123 Đường Lê Lợi, Q1', 150000);

  DECLARE @OrderId INT = SCOPE_IDENTITY();
  INSERT INTO dbo.OrderItems (OrderId, MenuItemId, Quantity, UnitPrice, TotalPrice)
  SELECT @OrderId, Id, 1, Price, Price
  FROM dbo.MenuItems WHERE Name IN (N'Phở Bò', N'Bún Chả');
END
GO

-- Đánh giá mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.Reviews)
BEGIN
  INSERT INTO dbo.Reviews (MenuItemId, CustomerName, Email, Rating, Comment, IsApproved)
  SELECT TOP 1 Id, N'Khách hàng X', 'x@example.com', 5, N'Rất ngon!', 1 FROM dbo.MenuItems ORDER BY Id;

  INSERT INTO dbo.Reviews (MenuItemId, CustomerName, Email, Rating, Comment, IsApproved)
  SELECT TOP 1 Id, N'Khách hàng Y', 'y@example.com', 4, N'Ổn, sẽ quay lại', 1 FROM dbo.MenuItems ORDER BY Id DESC;
END
GO

-- =====================================================
-- INDEXES
-- =====================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bookings_BookingDate')
BEGIN
  CREATE INDEX IX_Bookings_BookingDate ON dbo.Bookings(BookingDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId')
BEGIN
  CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
END
GO

-- =====================================================
-- HOÀN TẤT
-- =====================================================
PRINT N'✅ RestaurantDB schema + seed đã khởi tạo thành công!';
GO
