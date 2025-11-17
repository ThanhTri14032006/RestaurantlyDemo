using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;

namespace RestaurantMVC.Controllers
{
    // Không yêu cầu đăng nhập để tiện kiểm tra nhanh
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class DbDiagnosticsController : Controller
    {
        private readonly RestaurantDbContext _context;

        public DbDiagnosticsController(RestaurantDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("DbInfo")]
        public async Task<IActionResult> Index()
        {
            var viewModel = new DbInfoViewModel();
            try
            {
                viewModel.Provider = _context.Database.ProviderName ?? "(unknown)";
                viewModel.ConnectionString = _context.Database.GetDbConnection().ConnectionString ?? "(none)";
                viewModel.DatabaseName = _context.Database.GetDbConnection().Database;
                viewModel.CanConnect = await _context.Database.CanConnectAsync();

                viewModel.UsersCount = await _context.Users.CountAsync();
                viewModel.MenuItemsCount = await _context.MenuItems.CountAsync();
                viewModel.BookingsCount = await _context.Bookings.CountAsync();
                viewModel.ReviewsCount = await _context.Reviews.CountAsync();
                viewModel.OrdersCount = await _context.Orders.CountAsync();
                viewModel.OrderItemsCount = await _context.OrderItems.CountAsync();

                viewModel.UseSqlServerEnv = Environment.GetEnvironmentVariable("USE_SQLSERVER") ?? "(unset)";
                viewModel.AspNetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "(unset)";

                viewModel.RecentBookings = await _context.Bookings
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                viewModel.Status = "OK";
            }
            catch (Exception ex)
            {
                viewModel.Status = "ERROR";
                viewModel.ErrorMessage = ex.Message;
            }

            // Trỏ tới view hiện có trong thư mục Views/Diagnostics/Index.cshtml
            return View("~/Views/Diagnostics/Index.cshtml", viewModel);
        }
    }
}