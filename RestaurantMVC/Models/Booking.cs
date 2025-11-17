using System.ComponentModel.DataAnnotations;

namespace RestaurantMVC.Models
{
    public class Booking
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Ngày đặt bàn là bắt buộc")]
        [Display(Name = "Ngày đặt bàn")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }
        
        [Required(ErrorMessage = "Giờ đặt bàn là bắt buộc")]
        [Display(Name = "Giờ đặt bàn")]
        [DataType(DataType.Time)]
        public DateTime BookingTime { get; set; }
        
        [Required(ErrorMessage = "Số lượng khách là bắt buộc")]
        [Display(Name = "Số lượng khách")]
        [Range(1, 20, ErrorMessage = "Số lượng khách phải từ 1 đến 20")]
        public int PartySize { get; set; }
        
        [Display(Name = "Yêu cầu đặc biệt")]
        public string? SpecialRequests { get; set; }
        
        [Display(Name = "Trạng thái")]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Display(Name = "Ghi chú admin")]
        public string? AdminNotes { get; set; }
    }
    
    public enum BookingStatus
    {
        [Display(Name = "Chờ xác nhận")]
        Pending,
        [Display(Name = "Đã xác nhận")]
        Confirmed,
        [Display(Name = "Đã hủy")]
        Cancelled,
        [Display(Name = "Hoàn thành")]
        Completed
    }
}