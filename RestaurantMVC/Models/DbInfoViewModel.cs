using System.Collections.Generic;

namespace RestaurantMVC.Models
{
    public class DbInfoViewModel
    {
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }

        public string Provider { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public bool CanConnect { get; set; }

        public int UsersCount { get; set; }
        public int MenuItemsCount { get; set; }
        public int BookingsCount { get; set; }
        public int ReviewsCount { get; set; }
        public int OrdersCount { get; set; }
        public int OrderItemsCount { get; set; }

        public string UseSqlServerEnv { get; set; } = string.Empty;
        public string AspNetCoreEnv { get; set; } = string.Empty;

        public List<Booking> RecentBookings { get; set; } = new();
    }
}