-- =====================================================
-- SQLITE: LẤY DỮ LIỆU KHÁCH ĐẶT BÀN TỪ restaurant.db
-- Tác giả: Restaurant Management System
-- Phiên bản: SQLite tương thích với ứng dụng hiện tại
-- Hướng dẫn chạy (trong terminal):
--   sqlite3 "/Users/thanhtri/Downloads/Restaurantly/RestaurantMVC/restaurant.db" < \
--     "/Users/thanhtri/Downloads/Restaurantly/RestaurantMVC/Database/LayDuLieuKhachDatBan.sql"
-- =====================================================

-- =====================================================
-- 1. DANH SÁCH CHI TIẾT KHÁCH ĐẶT BÀN (MỚI NHẤT TRƯỚC)
-- =====================================================
SELECT
  b.Id AS "Mã Đặt Bàn",
  b.CustomerName AS "Tên Khách Hàng",
  b.Email AS "Email",
  b.Phone AS "Số Điện Thoại",
  strftime('%d/%m/%Y', b.BookingDate) AS "Ngày Đặt Bàn",
  strftime('%H:%M', b.BookingTime)    AS "Giờ Đặt Bàn",
  CASE strftime('%w', b.BookingDate)
    WHEN '0' THEN 'Chủ nhật'
    WHEN '1' THEN 'Thứ 2'
    WHEN '2' THEN 'Thứ 3'
    WHEN '3' THEN 'Thứ 4'
    WHEN '4' THEN 'Thứ 5'
    WHEN '5' THEN 'Thứ 6'
    WHEN '6' THEN 'Thứ 7'
  END AS "Thứ Trong Tuần",
  b.PartySize AS "Số Người",
  b.SpecialRequests AS "Yêu Cầu Đặc Biệt",
  CASE b.Status
    WHEN 0 THEN 'Chờ xác nhận'
    WHEN 1 THEN 'Đã xác nhận'
    WHEN 2 THEN 'Đã hoàn thành'
    WHEN 3 THEN 'Đã hủy'
    ELSE 'Không xác định'
  END AS "Trạng Thái",
  strftime('%d/%m/%Y %H:%M:%S', b.CreatedAt) AS "Thời Gian Tạo",
  b.AdminNotes AS "Ghi Chú Admin",
  CAST(julianday('now') - julianday(b.CreatedAt) AS INTEGER) AS "Số Ngày Từ Khi Đặt",
  CASE
    WHEN date(b.BookingDate) < date('now') THEN 'Đã qua'
    WHEN date(b.BookingDate) = date('now') THEN 'Hôm nay'
    WHEN date(b.BookingDate) = date('now','+1 day') THEN 'Ngày mai'
    ELSE 'Sắp tới'
  END AS "Tình Trạng Thời Gian"
FROM Bookings b
ORDER BY b.CreatedAt DESC;

-- =====================================================
-- 2. THỐNG KÊ TỔNG QUAN KHÁCH ĐẶT BÀN
-- =====================================================
-- Tổng quan
SELECT
  'THỐNG KÊ TỔNG QUAN' AS "Loại Báo Cáo",
  COUNT(*) AS "Tổng Số Đặt Bàn",
  COUNT(DISTINCT b.Email) AS "Số Khách Hàng Duy Nhất",
  SUM(b.PartySize) AS "Tổng Số Người",
  ROUND(AVG(CAST(b.PartySize AS REAL)), 2) AS "Số Người Trung Bình/Bàn",
  MIN(b.CreatedAt) AS "Đặt Bàn Đầu Tiên",
  MAX(b.CreatedAt) AS "Đặt Bàn Gần Nhất"
FROM Bookings b;

-- Theo trạng thái
SELECT
  CASE b.Status
    WHEN 0 THEN 'Chờ xác nhận'
    WHEN 1 THEN 'Đã xác nhận'
    WHEN 2 THEN 'Đã hoàn thành'
    WHEN 3 THEN 'Đã hủy'
  END AS "Loại Báo Cáo",
  COUNT(*) AS "Số Lượng",
  COUNT(DISTINCT b.Email) AS "Số Khách Hàng",
  SUM(b.PartySize) AS "Tổng Số Người",
  ROUND(AVG(CAST(b.PartySize AS REAL)), 2) AS "Trung Bình Người/Bàn",
  MIN(b.CreatedAt) AS "Sớm Nhất",
  MAX(b.CreatedAt) AS "Muộn Nhất"
FROM Bookings b
GROUP BY b.Status;

-- =====================================================
-- 3. KHÁCH HÀNG THƯỜNG XUYÊN (ĐẶT >= 2 LẦN)
-- =====================================================
SELECT
  b.CustomerName AS "Tên Khách Hàng",
  b.Email AS "Email",
  b.Phone AS "Số Điện Thoại",
  COUNT(*) AS "Số Lần Đặt Bàn",
  SUM(b.PartySize) AS "Tổng Số Người Đã Đặt",
  ROUND(AVG(CAST(b.PartySize AS REAL)), 2) AS "Trung Bình Người/Lần",
  MIN(b.BookingDate) AS "Lần Đặt Đầu Tiên",
  MAX(b.BookingDate) AS "Lần Đặt Gần Nhất",
  SUM(CASE WHEN b.Status = 2 THEN 1 ELSE 0 END) AS "Số Lần Hoàn Thành",
  SUM(CASE WHEN b.Status = 3 THEN 1 ELSE 0 END) AS "Số Lần Hủy"
FROM Bookings b
GROUP BY b.CustomerName, b.Email, b.Phone
HAVING COUNT(*) >= 2
ORDER BY COUNT(*) DESC, MAX(b.BookingDate) DESC;

-- =====================================================
-- 4. LỊCH ĐẶT BÀN THEO NGÀY (7 NGÀY TỚI)
-- =====================================================
SELECT
  strftime('%d/%m/%Y', b.BookingDate) AS "Ngày",
  CASE strftime('%w', b.BookingDate)
    WHEN '0' THEN 'Chủ nhật'
    WHEN '1' THEN 'Thứ 2'
    WHEN '2' THEN 'Thứ 3'
    WHEN '3' THEN 'Thứ 4'
    WHEN '4' THEN 'Thứ 5'
    WHEN '5' THEN 'Thứ 6'
    WHEN '6' THEN 'Thứ 7'
  END AS "Thứ",
  COUNT(*) AS "Số Đặt Bàn",
  SUM(b.PartySize) AS "Tổng Số Người",
  GROUP_CONCAT(
    (strftime('%H:%M', b.BookingTime) || ' - ' || b.CustomerName || ' (' || b.PartySize || ' người)'),
    '; '
  ) AS "Chi Tiết Đặt Bàn"
FROM Bookings b
WHERE date(b.BookingDate) BETWEEN date('now') AND date('now','+7 day')
  AND b.Status IN (0,1)
GROUP BY date(b.BookingDate)
ORDER BY date(b.BookingDate);

-- =====================================================
-- 5. XUẤT DỮ LIỆU ĐẦY ĐỦ DẠNG CSV (DÙNG VỚI sqlite3)
--   Mẹo: sqlite3 -header -csv restaurant.db "SELECT * FROM Bookings;" > bookings.csv
--   Hoặc dùng truy vấn dưới để kiểm soát cột và format
-- =====================================================
SELECT 'Mã Đặt Bàn,Tên Khách Hàng,Email,Số Điện Thoại,Ngày Đặt,Giờ Đặt,Số Người,Yêu Cầu Đặc Biệt,Trạng Thái,Thời Gian Tạo,Ghi Chú Admin'
UNION ALL
SELECT
  CAST(b.Id AS TEXT) || ',' ||
  '"' || REPLACE(b.CustomerName, '"', '""') || '",' ||
  '"' || IFNULL(b.Email, '') || '",' ||
  '"' || IFNULL(b.Phone, '') || '",' ||
  '"' || strftime('%d/%m/%Y', b.BookingDate) || '",' ||
  '"' || strftime('%H:%M', b.BookingTime) || '",' ||
  CAST(b.PartySize AS TEXT) || ',' ||
  '"' || REPLACE(IFNULL(b.SpecialRequests, ''), '"', '""') || '",' ||
  '"' || (CASE b.Status WHEN 0 THEN 'Chờ xác nhận' WHEN 1 THEN 'Đã xác nhận' WHEN 2 THEN 'Đã hoàn thành' WHEN 3 THEN 'Đã hủy' END) || '",' ||
  '"' || strftime('%d/%m/%Y %H:%M:%S', b.CreatedAt) || '",' ||
  '"' || REPLACE(IFNULL(b.AdminNotes, ''), '"', '""') || '"'
FROM Bookings b;

-- =====================================================
-- 6. TÌM KIẾM NÂNG CAO (DÀNH CHO SQLITE)
-- =====================================================
-- Tìm theo tên khách hàng
-- SELECT * FROM Bookings WHERE CustomerName LIKE '%TEN_KHACH%' ORDER BY CreatedAt DESC;

-- Tìm theo email
-- SELECT * FROM Bookings WHERE Email LIKE '%EMAIL_KHACH%' ORDER BY CreatedAt DESC;

-- Tìm theo số điện thoại
-- SELECT * FROM Bookings WHERE Phone LIKE '%SO_DIEN_THOAI%' ORDER BY CreatedAt DESC;

-- Tìm theo khoảng thời gian
-- SELECT * FROM Bookings WHERE date(BookingDate) BETWEEN '2025-01-01' AND '2025-12-31' ORDER BY BookingDate, BookingTime;

-- Tìm theo trạng thái cụ thể (0: Chờ, 1: Xác nhận, 2: Hoàn thành, 3: Hủy)
-- SELECT * FROM Bookings WHERE Status = 1 ORDER BY BookingDate, BookingTime;

-- Gợi ý: dùng chế độ CSV sẵn có của sqlite3 thay cho PRINT
-- Ví dụ: sqlite3 -header -csv restaurant.db "SELECT ..." > output.csv