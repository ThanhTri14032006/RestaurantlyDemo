-- =====================================================
-- SQL SERVER: BÁO CÁO TRANG ADMIN (Đơn hàng, Sản phẩm, Đặt bàn)
-- Hệ thống: RestaurantMVC (SQL Server)
-- Database: RestaurantDB
-- Hướng dẫn chạy nhanh:
--   docker run --rm -i mcr.microsoft.com/mssql-tools \
--     /opt/mssql-tools/bin/sqlcmd -S host.docker.internal,14333 \
--     -U sa -P 'PhamTri1403@' -d RestaurantDB -i \
--     "/Users/thanhtri/Downloads/Restaurantly/RestaurantMVC/Database/AdminReports.sql"
-- =====================================================

USE RestaurantDB;
GO

-- Biến lọc thời gian (mặc định: 30 ngày gần nhất)
DECLARE @StartDate DATETIME = DATEADD(DAY, -30, GETDATE());
DECLARE @EndDate   DATETIME = GETDATE();

-- =====================================================
-- 1) ĐẶT BÀN: Chi tiết mới nhất & Thống kê theo trạng thái
-- =====================================================

PRINT N'== ĐẶT BÀN: Chi tiết 50 bản ghi mới nhất ==';
SELECT TOP 50 
    b.Id,
    b.CustomerName AS N'Tên Khách Hàng',
    b.Email, 
    b.Phone AS N'Số Điện Thoại',
    FORMAT(b.BookingDate, 'dd/MM/yyyy') AS N'Ngày Đặt Bàn',
    FORMAT(b.BookingTime, 'HH:mm') AS N'Giờ Đặt Bàn',
    b.PartySize AS N'Số Người',
    CASE b.Status 
        WHEN 0 THEN N'Chờ xác nhận'
        WHEN 1 THEN N'Đã xác nhận'
        WHEN 2 THEN N'Đã hủy'
        WHEN 3 THEN N'Hoàn thành'
        ELSE N'Không rõ'
    END AS N'Trạng Thái',
    FORMAT(b.CreatedAt, 'dd/MM/yyyy HH:mm') AS N'Ngày Tạo',
    b.SpecialRequests AS N'Yêu Cầu Đặc Biệt'
FROM dbo.Bookings b
WHERE b.CreatedAt BETWEEN @StartDate AND @EndDate
ORDER BY b.CreatedAt DESC;

PRINT N'== ĐẶT BÀN: Thống kê theo trạng thái trong khoảng thời gian ==';
SELECT 
    CASE b.Status 
        WHEN 0 THEN N'Chờ xác nhận'
        WHEN 1 THEN N'Đã xác nhận'
        WHEN 2 THEN N'Đã hủy'
        WHEN 3 THEN N'Hoàn thành'
        ELSE N'Không rõ'
    END AS N'Trạng Thái',
    COUNT(*) AS N'Số Lượng'
FROM dbo.Bookings b
WHERE b.CreatedAt BETWEEN @StartDate AND @EndDate
GROUP BY b.Status
ORDER BY COUNT(*) DESC;

PRINT N'== ĐẶT BÀN: Số lượng mỗi ngày (14 ngày gần nhất) ==';
SELECT 
    FORMAT(CAST(b.CreatedAt AS DATE), 'dd/MM/yyyy') AS N'Ngày',
    COUNT(*) AS N'Số Lượng'
FROM dbo.Bookings b
WHERE b.CreatedAt >= DATEADD(DAY, -14, GETDATE())
GROUP BY CAST(b.CreatedAt AS DATE)
ORDER BY CAST(b.CreatedAt AS DATE) DESC;

-- =====================================================
-- 2) ĐƠN HÀNG: Chi tiết & Thống kê doanh thu, Top món bán chạy
-- =====================================================

PRINT N'== ĐƠN HÀNG: Chi tiết 100 bản ghi gần đây ==';
SELECT TOP 100 
    o.Id,
    o.CustomerName AS N'Khách Hàng',
    o.Email,
    o.Phone AS N'SĐT',
    o.DeliveryAddress AS N'Địa Chỉ',
    o.TotalAmount AS N'Tổng Tiền',
    CASE o.Status
        WHEN 0 THEN N'Chờ xác nhận'
        WHEN 1 THEN N'Đã xác nhận'
        WHEN 2 THEN N'Đang chuẩn bị'
        WHEN 3 THEN N'Sẵn sàng'
        WHEN 4 THEN N'Đã giao'
        WHEN 5 THEN N'Đã hủy'
        ELSE N'Không rõ'
    END AS N'Trạng Thái',
    CASE o.OrderType
        WHEN 0 THEN N'Giao hàng'
        WHEN 1 THEN N'Đến lấy'
        ELSE N'Không rõ'
    END AS N'Hình Thức',
    FORMAT(o.CreatedAt, 'dd/MM/yyyy HH:mm') AS N'Ngày Tạo',
    ISNULL(o.Notes, N'') AS N'Ghi Chú'
FROM dbo.Orders o
WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate
ORDER BY o.CreatedAt DESC;

PRINT N'== ĐƠN HÀNG: Doanh thu theo ngày trong khoảng thời gian ==';
SELECT 
    FORMAT(CAST(o.CreatedAt AS DATE), 'dd/MM/yyyy') AS N'Ngày',
    SUM(o.TotalAmount) AS N'Doanh Thu'
FROM dbo.Orders o
WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate
GROUP BY CAST(o.CreatedAt AS DATE)
ORDER BY CAST(o.CreatedAt AS DATE) DESC;

PRINT N'== ĐƠN HÀNG: Top 10 món bán chạy trong khoảng thời gian ==';
SELECT TOP 10 
    mi.Id AS MenuItemId,
    mi.Name AS N'Tên Món',
    mi.Category AS N'Danh Mục',
    SUM(oi.Quantity) AS N'Tổng Số Lượng',
    SUM(oi.TotalPrice) AS N'Tổng Doanh Thu'
FROM dbo.OrderItems oi
JOIN dbo.Orders o ON o.Id = oi.OrderId
JOIN dbo.MenuItems mi ON mi.Id = oi.MenuItemId
WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate
GROUP BY mi.Id, mi.Name, mi.Category
ORDER BY SUM(oi.Quantity) DESC, SUM(oi.TotalPrice) DESC;

-- =====================================================
-- 3) SẢN PHẨM (Menu): Danh sách & Thống kê danh mục
-- =====================================================

PRINT N'== SẢN PHẨM: Danh sách món ăn ==';
SELECT 
    mi.Id,
    mi.Name AS N'Tên Món',
    mi.Category AS N'Danh Mục',
    mi.Price AS N'Giá',
    CASE WHEN mi.IsAvailable = 1 THEN N'Có sẵn' ELSE N'Ngừng bán' END AS N'Trạng Thái',
    FORMAT(mi.CreatedAt, 'dd/MM/yyyy HH:mm') AS N'Ngày Tạo'
FROM dbo.MenuItems mi
ORDER BY mi.CreatedAt DESC;

PRINT N'== SẢN PHẨM: Thống kê danh mục ==';
SELECT 
    mi.Category AS N'Danh Mục',
    COUNT(*) AS N'Số Món',
    AVG(mi.Price) AS N'Giá Trung Bình',
    SUM(CASE WHEN mi.IsAvailable = 1 THEN 1 ELSE 0 END) AS N'Đang Bán'
FROM dbo.MenuItems mi
GROUP BY mi.Category
ORDER BY COUNT(*) DESC;

-- =====================================================
-- 4) NGƯỜI DÙNG & ĐÁNH GIÁ: tổng hợp nhanh
-- =====================================================

PRINT N'== NGƯỜI DÙNG: Thống kê nhanh ==';
SELECT 
    COUNT(*) AS N'Tổng Người Dùng',
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS N'Đang Hoạt Động'
FROM dbo.Users;

PRINT N'== ĐÁNH GIÁ: Số lượng & Điểm trung bình theo món ==';
SELECT 
    mi.Id AS MenuItemId,
    mi.Name AS N'Tên Món',
    COUNT(r.Id) AS N'Số Lượng Đánh Giá',
    AVG(CAST(r.Rating AS DECIMAL(10,2))) AS N'Điểm Trung Bình'
FROM dbo.MenuItems mi
LEFT JOIN dbo.Reviews r ON r.MenuItemId = mi.Id
GROUP BY mi.Id, mi.Name
ORDER BY COUNT(r.Id) DESC;

-- =====================================================
-- KẾT THÚC BÁO CÁO
-- =====================================================