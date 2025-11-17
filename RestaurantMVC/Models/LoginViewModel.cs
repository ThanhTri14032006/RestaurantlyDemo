using System.ComponentModel.DataAnnotations;

namespace RestaurantMVC.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
        
        public string? ReturnUrl { get; set; }
    }
}