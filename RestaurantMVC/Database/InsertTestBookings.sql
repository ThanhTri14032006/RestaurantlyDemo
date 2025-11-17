-- =====================================================
-- SQL SERVER: Thêm 4 dữ liệu đặt bàn kiểm thử cho Admin
-- Hệ thống: RestaurantMVC (SQL Server)
-- Database: RestaurantDB
-- Cách chạy từ máy host (khớp app trên cổng 5096):
--   sqlcmd -S 127.0.0.1,14333 -U sa -P 'PhamTri1403@' \
--          -d RestaurantDB -i \
--          "/Users/thanhtri/Downloads/Restaurantly/RestaurantMVC/Database/InsertTestBookings.sql"
-- =====================================================

USE RestaurantDB;
GO

DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @BaseDateTime DATETIME2 = CAST(@Today AS DATETIME2);

-- Ghi chú mapping Status theo ứng dụng (Booking.cs):
--   0 = Pending, 1 = Confirmed, 2 = Cancelled, 3 = Completed

INSERT INTO dbo.Bookings (
  CustomerName, Email, Phone,
  BookingDate, BookingTime, PartySize,
  SpecialRequests, Status, CreatedAt, AdminNotes
)
VALUES
  (N'Đinh Văn H', 'h@example.com', '0911000001',
   DATEADD(DAY, 0, @BaseDateTime), DATEADD(HOUR, 19, @BaseDateTime), 2,
   N'Bàn gần cửa sổ', 0, SYSUTCDATETIME(), N''), -- Pending

  (N'Võ Thị I', 'i@example.com', '0911000002',
   DATEADD(DAY, 1, @BaseDateTime), DATEADD(HOUR, 18, @BaseDateTime), 4,
   N'', 1, SYSUTCDATETIME(), N''), -- Confirmed

  (N'Phạm Bá K', 'k@example.com', '0911000003',
   DATEADD(DAY, 2, @BaseDateTime), DATEADD(HOUR, 20, @BaseDateTime), 3,
   N'Ít cay', 2, SYSUTCDATETIME(), N''), -- Cancelled

  (N'Nguyễn Thị L', 'l@example.com', '0911000004',
   DATEADD(DAY, 3, @BaseDateTime), DATEADD(HOUR, 17, @BaseDateTime), 5,
   N'Có ghế trẻ em', 3, SYSUTCDATETIME(), N'Test dữ liệu'); -- Completed
GO

PRINT N'✅ Đã chèn 4 đặt bàn kiểm thử vào dbo.Bookings.';

-- Xem nhanh 10 đặt bàn mới nhất để đối chiếu trên Admin
SELECT TOP 10 
  b.Id,
  b.CustomerName AS N'Tên Khách Hàng',
  b.Email,
  b.Phone AS N'SĐT',
  FORMAT(b.BookingDate, 'dd/MM/yyyy') AS N'Ngày Đặt',
  FORMAT(b.BookingTime, 'HH:mm') AS N'Giờ Đặt',
  b.PartySize AS N'Số Người',
  CASE b.Status
    WHEN 0 THEN N'Chờ xác nhận'
    WHEN 1 THEN N'Đã xác nhận'
    WHEN 2 THEN N'Đã hủy'
    WHEN 3 THEN N'Hoàn thành'
    ELSE N'Không rõ'
  END AS N'Trạng Thái',
  FORMAT(b.CreatedAt, 'dd/MM/yyyy HH:mm') AS N'Ngày Tạo'
FROM dbo.Bookings b
ORDER BY b.CreatedAt DESC;
GO