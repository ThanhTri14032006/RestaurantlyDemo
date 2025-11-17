using Microsoft.AspNetCore.Mvc;
using RestaurantMVC.Models;
using RestaurantMVC.Services;

namespace RestaurantMVC.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class ChatController : Controller
    {
        private const string SessionKey = "ChatConversationId";
        private readonly RestaurantDbContext _db;

        public ChatController(RestaurantDbContext db)
        {
            _db = db;
        }

        private string GetOrCreateConversationId()
        {
            var id = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrWhiteSpace(id))
            {
                id = ChatStore.EnsureConversation();
                HttpContext.Session.SetString(SessionKey, id);
            }
            else
            {
                ChatStore.EnsureConversation(id);
            }
            return id!;
        }

        [HttpGet]
        public IActionResult Init()
        {
            var id = GetOrCreateConversationId();
            return Json(new { conversationId = id });
        }

        [HttpGet]
        public async Task<IActionResult> Fetch()
        {
            var id = GetOrCreateConversationId();
            var msgs = await ChatRepository.GetMessagesAsync(_db, id);
            return Json(new { conversationId = id, messages = msgs });
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromForm] string text, [FromForm] string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest(new { error = "Text required" });

            var id = GetOrCreateConversationId();
            try
            {
                await ChatRepository.AddMessageAsync(_db, new ChatMessage
                {
                    ConversationId = id,
                    Sender = "customer",
                    DisplayName = displayName,
                    Text = text.Trim()
                });
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message });
            }
        }

        // Admin endpoints
        [HttpGet]
        public async Task<IActionResult> All()
        {
            var latest = await ChatRepository.GetLatestByConversationAsync(_db);
            return Json(latest.Select(x => new
            {
                conversationId = x.ConversationId,
                latestText = x.Latest?.Text,
                latestAt = x.Latest?.CreatedAt,
                latestSender = x.Latest?.Sender
            }));
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();
            var msgs = await ChatRepository.GetMessagesAsync(_db, id);
            return Json(msgs);
        }

        [HttpPost]
        public async Task<IActionResult> Reply([FromForm] string id, [FromForm] string text)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(text))
                return BadRequest(new { error = "Missing id or text" });
            try
            {
                await ChatRepository.AddMessageAsync(_db, new ChatMessage
                {
                    ConversationId = id,
                    Sender = "admin",
                    Text = text.Trim()
                });
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message });
            }
        }
    }
}