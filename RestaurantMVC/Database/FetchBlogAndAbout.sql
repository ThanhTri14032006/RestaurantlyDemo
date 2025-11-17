-- FetchBlogAndAbout.sql
-- Truy vấn dữ liệu từ SQL Server cho trang Blog và About
-- Database hiện tại: RestaurantDBDev (sửa tên DB nếu khác)

USE [RestaurantDBDev];
GO
SET NOCOUNT ON;
GO

-- =============================================
-- Phần 1: Truy vấn BlogEntries
-- =============================================

-- 1. Lấy tất cả bài blog, mới nhất trước
SELECT
    [Id], [Title], [Author], [Excerpt], [ImageUrl], [PublishedAt]
FROM dbo.[BlogEntries]
ORDER BY [PublishedAt] DESC;
GO

-- 2. Lấy Top N bài blog mới nhất
DECLARE @TopN INT = 5;
SELECT TOP (@TopN)
    [Id], [Title], [Author], [Excerpt], [ImageUrl], [PublishedAt]
FROM dbo.[BlogEntries]
ORDER BY [PublishedAt] DESC;
GO

-- 3. Lấy chi tiết bài blog theo Id
DECLARE @BlogId INT = 1;
SELECT
    [Id], [Title], [Author], [Excerpt], [Content], [ImageUrl], [PublishedAt]
FROM dbo.[BlogEntries]
WHERE [Id] = @BlogId;
GO

-- 4. Tìm kiếm bài blog theo từ khóa trong tiêu đề hoặc nội dung
DECLARE @Keyword NVARCHAR(200) = N'phở';
SELECT
    [Id], [Title], [Author], [Excerpt], [ImageUrl], [PublishedAt]
FROM dbo.[BlogEntries]
WHERE [Title] LIKE N'%' + @Keyword + N'%' OR [Content] LIKE N'%' + @Keyword + N'%'
ORDER BY [PublishedAt] DESC;
GO

-- =============================================
-- Phần 2: Truy vấn AboutSettings
-- Ghi chú: Nội dung About ban đầu ở appsettings.json. 
-- Nếu muốn truy xuất từ SQL Server, dùng đoạn DDL bên dưới để tạo bảng/About seed.
-- =============================================

-- Tạo bảng AboutSettings nếu chưa tồn tại và seed giá trị mặc định
IF NOT EXISTS (
    SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.AboutSettings') AND [type] = N'U'
)
BEGIN
    CREATE TABLE dbo.[AboutSettings]
    (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Title] NVARCHAR(200) NOT NULL,
        [Lead] NVARCHAR(500) NOT NULL,
        [Story1] NVARCHAR(1000) NULL,
        [Story2] NVARCHAR(1000) NULL,
        [Story3] NVARCHAR(1000) NULL,
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    INSERT INTO dbo.[AboutSettings] ([Title], [Lead], [Story1], [Story2], [Story3])
    VALUES (
        N'Về Restaurantly',
        N'Câu chuyện mang đến trải nghiệm ẩm thực tuyệt vời',
        N'Restaurantly được thành lập năm 2020, mang trải nghiệm ẩm thực Việt trong không gian hiện đại.',
        N'Mỗi bữa ăn là cơ hội kết nối với gia đình, bạn bè và tạo kỷ niệm đẹp.',
        N'Đội ngũ đầu bếp giàu kinh nghiệm, cam kết món ăn chất lượng từ nguyên liệu tươi ngon.'
    );
END
GO

-- Lấy nội dung About hiện tại (bản cập nhật mới nhất)
SELECT TOP (1)
    [Id], [Title], [Lead], [Story1], [Story2], [Story3], [UpdatedAt]
FROM dbo.[AboutSettings]
ORDER BY [UpdatedAt] DESC;
GO

-- Ví dụ cập nhật nội dung About
-- UPDATE dbo.[AboutSettings]
-- SET [Title] = N'Về chúng tôi',
--     [Lead] = N'Thực đơn đa dạng và trải nghiệm tuyệt vời',
--     [UpdatedAt] = SYSUTCDATETIME()
-- WHERE [Id] = 1;
-- GO