/*
  ChatMessages.sql — SQL Server script cho chatbox
  - Tạo bảng lưu tin nhắn
  - Index phục vụ truy vấn nhanh
  - Stored procedures: thêm tin, lấy theo conversation, lấy tin mới nhất theo cuộc trò chuyện
  Lưu ý: dùng NVARCHAR để hỗ trợ tiếng Việt; thời gian dùng UTC.
*/

-- Sử dụng đúng database (chỉnh lại nếu tên DB khác)
USE [RestaurantDB];
GO

-- Tạo bảng ChatMessages nếu chưa có
IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages (
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        ConversationId NVARCHAR(64) NOT NULL,
        Sender NVARCHAR(16) NOT NULL,              -- 'customer' hoặc 'admin'
        DisplayName NVARCHAR(100) NULL,
        Text NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ChatMessages PRIMARY KEY (Id)
    );
    CREATE INDEX IX_ChatMessages_ConversationId ON dbo.ChatMessages(ConversationId);
    CREATE INDEX IX_ChatMessages_CreatedAt ON dbo.ChatMessages(CreatedAt);
END
GO

-- Thêm tin nhắn
CREATE OR ALTER PROCEDURE dbo.usp_AddChatMessage
    @ConversationId NVARCHAR(64),
    @Sender NVARCHAR(16),
    @DisplayName NVARCHAR(100) = NULL,
    @Text NVARCHAR(MAX),
    @CreatedAt DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @CreatedAt IS NULL SET @CreatedAt = SYSUTCDATETIME();

    INSERT INTO dbo.ChatMessages (ConversationId, Sender, DisplayName, Text, CreatedAt)
    VALUES (@ConversationId, @Sender, @DisplayName, @Text, @CreatedAt);
END
GO

-- Lấy toàn bộ tin nhắn của một conversation (tăng dần theo thời gian)
CREATE OR ALTER PROCEDURE dbo.usp_GetConversationMessages
    @ConversationId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, ConversationId, Sender, DisplayName, Text, CreatedAt
    FROM dbo.ChatMessages
    WHERE ConversationId = @ConversationId
    ORDER BY CreatedAt ASC;
END
GO

-- Lấy tin mới nhất theo từng conversation
CREATE OR ALTER PROCEDURE dbo.usp_GetLatestByConversation
AS
BEGIN
    SET NOCOUNT ON;
    ;WITH Latest AS (
        SELECT ConversationId,
               Id,
               Sender,
               Text,
               CreatedAt,
               ROW_NUMBER() OVER (PARTITION BY ConversationId ORDER BY CreatedAt DESC) AS rn
        FROM dbo.ChatMessages
    )
    SELECT ConversationId,
           Id AS LatestId,
           Sender AS LatestSender,
           Text AS LatestText,
           CreatedAt AS LatestAt
    FROM Latest
    WHERE rn = 1
    ORDER BY LatestAt DESC;
END
GO

/*
-- Ví dụ sử dụng (bỏ comment để chạy thử):

-- Thêm tin nhắn mẫu
-- EXEC dbo.usp_AddChatMessage @ConversationId = N'abc123', @Sender = N'customer', @DisplayName = N'Bạn', @Text = N'Xin chào';

-- Lấy tin nhắn của conversation
-- EXEC dbo.usp_GetConversationMessages @ConversationId = N'abc123';

-- Lấy tin mới nhất theo conversation
-- EXEC dbo.usp_GetLatestByConversation;
*/