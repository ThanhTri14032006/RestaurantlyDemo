using System.ComponentModel.DataAnnotations;

namespace RestaurantMVC.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;
        
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }
        
        [Display(Name = "Vai trò")]
        public UserRole Role { get; set; } = UserRole.Customer;
        
        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Display(Name = "Cập nhật lần cuối")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        [Display(Name = "Lần đăng nhập cuối")]
        public DateTime? LastLoginAt { get; set; }
    }
    
    public enum UserRole
    {
        [Display(Name = "Khách hàng")]
        Customer,
        [Display(Name = "Nhân viên")]
        Staff,
        [Display(Name = "Quản lý")]
        Manager,
        [Display(Name = "Admin")]
        Admin
    }
}