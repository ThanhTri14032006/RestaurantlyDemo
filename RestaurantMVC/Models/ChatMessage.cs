using System;

namespace RestaurantMVC.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ConversationId { get; set; } = string.Empty;
        public string Sender { get; set; } = "customer"; // "customer" or "admin"
        public string? DisplayName { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}