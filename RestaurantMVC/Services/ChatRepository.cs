using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;

namespace RestaurantMVC.Services
{
    public static class ChatRepository
    {
        public static void EnsureSchema(RestaurantDbContext db)
        {
            if (db.Database.IsSqlite())
            {
                db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ChatMessages (
                    Id TEXT NOT NULL,
                    ConversationId TEXT NOT NULL,
                    Sender TEXT NOT NULL,
                    DisplayName TEXT NULL,
                    Text TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    PRIMARY KEY (Id)
                );");
                db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS IX_ChatMessages_ConversationId ON ChatMessages(ConversationId);");
            }
            else if (db.Database.IsSqlServer())
            {
                // If table exists but columns are wrong, rebuild to correct schema
                db.Database.ExecuteSqlRaw(@"
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages')
                    BEGIN
                        DECLARE @missing INT = (
                            SELECT COUNT(*) FROM (
                                SELECT 'Id' AS Col UNION ALL
                                SELECT 'ConversationId' UNION ALL
                                SELECT 'Sender' UNION ALL
                                SELECT 'DisplayName' UNION ALL
                                SELECT 'Text' UNION ALL
                                SELECT 'CreatedAt'
                            ) AS Req
                            WHERE NOT EXISTS (
                                SELECT 1 FROM sys.columns c
                                JOIN sys.objects o ON o.object_id = c.object_id
                                WHERE o.name = 'ChatMessages' AND c.name = Req.Col
                            )
                        );
                        IF (@missing > 0)
                        BEGIN
                            DROP TABLE ChatMessages;
                        END

                        -- Kiểm tra sai kiểu dữ liệu hoặc độ dài cột, nếu sai thì rebuild
                        IF (@missing = 0)
                        BEGIN
                            DECLARE @badSchema BIT = 0;
                            ;WITH cols AS (
                                SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
                                FROM INFORMATION_SCHEMA.COLUMNS
                                WHERE TABLE_NAME = 'ChatMessages'
                            )
                            SELECT @badSchema = CASE WHEN EXISTS (
                                SELECT 1 FROM (
                                    SELECT 'Id' AS COLUMN_NAME, 'uniqueidentifier' AS DATA_TYPE, NULL AS CHARACTER_MAXIMUM_LENGTH UNION ALL
                                    SELECT 'ConversationId','nvarchar',64 UNION ALL
                                    SELECT 'Sender','nvarchar',16 UNION ALL
                                    SELECT 'DisplayName','nvarchar',100 UNION ALL
                                    SELECT 'Text','nvarchar',-1 UNION ALL -- NVARCHAR(MAX) = -1
                                    SELECT 'CreatedAt','datetime2',NULL
                                ) AS Expect
                                LEFT JOIN cols ON cols.COLUMN_NAME = Expect.COLUMN_NAME
                                WHERE cols.COLUMN_NAME IS NULL
                                      OR cols.DATA_TYPE <> Expect.DATA_TYPE
                                      OR (Expect.CHARACTER_MAXIMUM_LENGTH IS NOT NULL AND cols.CHARACTER_MAXIMUM_LENGTH <> Expect.CHARACTER_MAXIMUM_LENGTH)
                            ) THEN 1 ELSE 0 END;

                            IF (@badSchema = 1)
                                DROP TABLE ChatMessages;
                        END
                    END");

                // Ensure table exists with proper defaults and indexes
                db.Database.ExecuteSqlRaw(@"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages') BEGIN
                    CREATE TABLE ChatMessages (
                        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                        ConversationId NVARCHAR(64) NOT NULL,
                        Sender NVARCHAR(16) NOT NULL,
                        DisplayName NVARCHAR(100) NULL,
                        Text NVARCHAR(MAX) NOT NULL,
                        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT PK_ChatMessages PRIMARY KEY (Id)
                    );
                    CREATE INDEX IX_ChatMessages_ConversationId ON ChatMessages(ConversationId);
                    CREATE INDEX IX_ChatMessages_CreatedAt ON ChatMessages(CreatedAt);
                END");

                // If table already existed, ensure CreatedAt has a DEFAULT constraint and indexes exist
                db.Database.ExecuteSqlRaw(@"IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages') BEGIN
                    DECLARE @hasDefault BIT = (
                        SELECT CASE WHEN EXISTS (
                            SELECT 1
                            FROM sys.default_constraints dc
                            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                            JOIN sys.objects o ON o.object_id = c.object_id
                            WHERE o.name = 'ChatMessages' AND c.name = 'CreatedAt'
                        ) THEN 1 ELSE 0 END
                    );
                    IF (@hasDefault = 0)
                        ALTER TABLE ChatMessages ADD CONSTRAINT DF_ChatMessages_CreatedAt DEFAULT SYSUTCDATETIME() FOR CreatedAt;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatMessages_ConversationId' AND object_id = OBJECT_ID('ChatMessages'))
                        CREATE INDEX IX_ChatMessages_ConversationId ON ChatMessages(ConversationId);
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatMessages_CreatedAt' AND object_id = OBJECT_ID('ChatMessages'))
                        CREATE INDEX IX_ChatMessages_CreatedAt ON ChatMessages(CreatedAt);
                END");
            }
        }

        public static async Task<List<ChatMessage>> GetMessagesAsync(RestaurantDbContext db, string conversationId)
        {
            try
            {
                var list = await db.ChatMessages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();
                if (list.Count > 0)
                    return list;
            }
            catch { }
            return ChatStore.GetMessages(conversationId).ToList();
        }

        public static async Task AddMessageAsync(RestaurantDbContext db, ChatMessage msg)
        {
            // In SQL Server, use explicit INSERT to avoid provider quirks if any
            try
            {
                if (db.Database.IsSqlServer())
                {
                    var id = msg.Id == default ? Guid.NewGuid() : msg.Id;
                    var created = msg.CreatedAt == default ? DateTime.UtcNow : msg.CreatedAt;
                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO ChatMessages (Id, ConversationId, Sender, DisplayName, Text, CreatedAt) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
                        id,
                        msg.ConversationId,
                        msg.Sender,
                        (object?)msg.DisplayName ?? DBNull.Value,
                        msg.Text,
                        created
                    );
                }
                else
                {
                    db.ChatMessages.Add(msg);
                    await db.SaveChangesAsync();
                }
            }
            catch { }
            // Always mirror to in-memory store to ensure connectivity even if DB is empty
            ChatStore.AddMessage(msg);
        }

        public static async Task<List<(string ConversationId, ChatMessage? Latest)>> GetLatestByConversationAsync(RestaurantDbContext db)
        {
            List<(string ConversationId, ChatMessage? Latest)> result = new();
            try
            {
                var grouped = await db.ChatMessages
                    .GroupBy(m => m.ConversationId)
                    .Select(g => new {
                        ConversationId = g.Key,
                        Latest = g.OrderByDescending(x => x.CreatedAt).FirstOrDefault()
                    })
                    .ToListAsync();
                result.AddRange(grouped.Select(x => (x.ConversationId, x.Latest)));
            }
            catch { }

            var memLatest = ChatStore.GetLatestByConversation();
            foreach (var kvp in memLatest)
            {
                if (!result.Any(r => r.ConversationId == kvp.Key) || result.First(r => r.ConversationId == kvp.Key).Latest == null)
                {
                    result.Add((kvp.Key, kvp.Value));
                }
            }
            return result;
        }
    }
}