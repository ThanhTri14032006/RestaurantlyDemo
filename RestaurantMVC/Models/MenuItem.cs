using System.ComponentModel.DataAnnotations;

namespace RestaurantMVC.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Tên món ăn là bắt buộc")]
        [Display(Name = "Tên món ăn")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Display(Name = "Giá")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }
        
        [Display(Name = "Danh mục")]
        public string Category { get; set; } = string.Empty;
        
        [Display(Name = "Hình ảnh")]
        public string ImageUrl { get; set; } = string.Empty;
        
        [Display(Name = "Có sẵn")]
        public bool IsAvailable { get; set; } = true;
        
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}