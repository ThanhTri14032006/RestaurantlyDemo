using System.ComponentModel.DataAnnotations;

namespace RestaurantMVC.Models
{
    public class Review
    {
        public int Id { get; set; }
        
        [Required]
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;
        
        [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
        [Display(Name = "Tên khách hàng")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Đánh giá là bắt buộc")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        [Display(Name = "Đánh giá")]
        public int Rating { get; set; }
        
        [Display(Name = "Nhận xét")]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
        
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Display(Name = "Đã duyệt")]
        public bool IsApproved { get; set; } = false;
    }
}