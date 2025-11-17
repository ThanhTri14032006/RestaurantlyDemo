using System;

namespace RestaurantMVC.Models
{
    // Đổi tên thành BlogEntry để tránh trùng định nghĩa với kiểu khác trong project
    public class BlogEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public string Author { get; set; } = "Admin";
    }
}