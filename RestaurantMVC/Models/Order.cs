using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMVC.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Địa chỉ giao hàng không được vượt quá 500 ký tự")]
        public string? DeliveryAddress { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        public OrderType OrderType { get; set; } = OrderType.Delivery;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }

        // Navigation property
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public enum OrderStatus
    {
        [Display(Name = "Chờ xác nhận")]
        Pending = 0,
        
        [Display(Name = "Đã xác nhận")]
        Confirmed = 1,
        
        [Display(Name = "Đang chuẩn bị")]
        Preparing = 2,
        
        [Display(Name = "Sẵn sàng")]
        Ready = 3,
        
        [Display(Name = "Đã giao")]
        Delivered = 4,
        
        [Display(Name = "Đã hủy")]
        Cancelled = 5
    }

    public enum OrderType
    {
        [Display(Name = "Giao hàng")]
        Delivery = 0,
        
        [Display(Name = "Đến lấy")]
        Pickup = 1
    }
}