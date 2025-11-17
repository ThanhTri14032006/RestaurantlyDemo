-- =====================================================
-- GetCustomerBookings.sql
-- Mục đích: Lấy dữ liệu khách hàng đã đặt bàn trên website từ SQL Server
-- Bao gồm: View tổng hợp, Stored Procedure có lọc theo ngày, trạng thái, từ khóa
-- Cách dùng nhanh:
--   1) Mở SSMS, chọn database RestaurantDB
--   2) Chạy toàn bộ script này (F5)
--   3) Gọi: EXEC dbo.sp_GetCustomerBookings @FromDate='2024-01-01', @Status=1, @Keyword=N'nguyen'
-- =====================================================

USE [RestaurantDB];
GO

-- Trạng thái đặt bàn (tham khảo)
-- 0 = Pending (Chờ xử lý)
-- 1 = Confirmed (Đã xác nhận)
-- 2 = Completed (Hoàn tất)
-- 3 = Cancelled (Đã hủy)

-- View tổng hợp dữ liệu đặt bàn (dễ SELECT trực tiếp)
CREATE OR ALTER VIEW dbo.vw_CustomerBookings AS
SELECT
    b.Id                AS BookingId,
    b.CustomerName,
    b.Email,
    b.Phone,
    b.BookingDate,
    b.BookingTime,
    b.PartySize,
    b.Status,
    b.SpecialRequests,
    b.AdminNotes,
    b.CreatedAt
FROM dbo.Bookings AS b;
GO

-- Stored Procedure: Lọc theo khoảng ngày, trạng thái, từ khóa (tên/email/điện thoại)
CREATE OR ALTER PROCEDURE dbo.sp_GetCustomerBookings
    @FromDate DATE = NULL,
    @ToDate   DATE = NULL,
    @Status   INT  = NULL,
    @Keyword  NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        b.Id                AS BookingId,
        b.CustomerName,
        b.Email,
        b.Phone,
        b.BookingDate,
        b.BookingTime,
        b.PartySize,
        b.Status,
        b.SpecialRequests,
        b.AdminNotes,
        b.CreatedAt
    FROM dbo.Bookings AS b
    WHERE (@FromDate IS NULL OR b.BookingDate >= @FromDate)
      AND (@ToDate   IS NULL OR b.BookingDate <= @ToDate)
      AND (@Status   IS NULL OR b.Status = @Status)
      AND (
            @Keyword IS NULL OR
            b.CustomerName LIKE N'%' + @Keyword + N'%' OR
            b.Email        LIKE N'%' + @Keyword + N'%' OR
            b.Phone        LIKE N'%' + @Keyword + N'%'
          )
    ORDER BY b.BookingDate DESC, b.BookingTime DESC, b.Id DESC;
END
GO

-- Ví dụ chạy nhanh:
-- Lấy tất cả
-- SELECT * FROM dbo.vw_CustomerBookings;

-- Lấy theo khoảng ngày
-- EXEC dbo.sp_GetCustomerBookings @FromDate='2025-01-01', @ToDate='2025-12-31';

-- Chỉ bản ghi đã xác nhận
-- EXEC dbo.sp_GetCustomerBookings @Status=1;

-- Tìm theo từ khóa (tên/email/sđt)
-- EXEC dbo.sp_GetCustomerBookings @Keyword=N'nguyen';

-- Xuất ra JSON (tham khảo, xem trực tiếp trong SSMS):
-- SELECT * FROM dbo.vw_CustomerBookings FOR JSON PATH;
GO