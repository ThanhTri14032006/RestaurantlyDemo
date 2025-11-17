using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using RestaurantMVC.Services;

namespace RestaurantMVC.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly RestaurantDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AdminController(RestaurantDbContext context, IWebHostEnvironment env, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _env = env;
            _config = config;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            
            var stats = new
            {
                TotalBookingsToday = await _context.Bookings
                    .CountAsync(b => b.BookingDate.Date == today),
                PendingBookings = await _context.Bookings
                    .CountAsync(b => b.Status == BookingStatus.Pending),
                TotalMenuItems = await _context.MenuItems.CountAsync(),
                AvailableMenuItems = await _context.MenuItems
                    .CountAsync(m => m.IsAvailable)
            };
            
            var recentBookings = await _context.Bookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToListAsync();
            
            ViewBag.Stats = stats;
            ViewBag.RecentBookings = recentBookings;
            
            return View();
        }

        // Booking Management
        public async Task<IActionResult> Bookings(string status = "", DateTime? fromDate = null, DateTime? toDate = null, string? search = null)
        {
            var bookings = _context.Bookings.AsQueryable();
            
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var bookingStatus))
            {
                bookings = bookings.Where(b => b.Status == bookingStatus);
            }
            
            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                bookings = bookings.Where(b => b.BookingDate.Date >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date;
                bookings = bookings.Where(b => b.BookingDate.Date <= to);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                bookings = bookings.Where(b =>
                    (b.CustomerName != null && b.CustomerName.Contains(term)) ||
                    (b.Email != null && b.Email.Contains(term)) ||
                    (b.Phone != null && b.Phone.Contains(term))
                );
            }
            
            // Get bookings and sort in memory to avoid SQLite TimeSpan ORDER BY issue
            var result = await bookings
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
            
            // Sort by BookingTime in memory (now using DateTime.TimeOfDay)
            result = result.OrderByDescending(b => b.BookingDate)
                          .ThenByDescending(b => b.BookingTime.TimeOfDay)
                          .ToList();
            
            ViewBag.CurrentStatus = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = search;
            
            return View(result);
        }

        // Booking details (Admin)
        [HttpGet]
        public async Task<IActionResult> BookingDetails(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt bàn.";
                return RedirectToAction("Bookings");
            }
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingNotes(int id, string? adminNotes)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt bàn để cập nhật ghi chú.";
                return RedirectToAction("Bookings");
            }

            booking.AdminNotes = adminNotes;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu ghi chú cho đặt bàn.";
            return RedirectToAction("BookingDetails", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(int id, BookingStatus status, string? adminNotes = null)
        {
            var booking = await _context.Bookings.FindAsync(id);
            
            if (booking != null)
            {
                booking.Status = status;
                if (!string.IsNullOrEmpty(adminNotes))
                {
                    booking.AdminNotes = adminNotes;
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật trạng thái đặt bàn thành công.";

                // Send status update email to customer when applicable
                if (!string.IsNullOrWhiteSpace(booking.Email))
                {
                    string subject;
                    string html;
                    string plain;

                    switch (status)
                    {
                        case BookingStatus.Confirmed:
                            subject = $"[Restaurantly] Đã xác nhận đặt bàn #{booking.Id}";
                            html = $@"<h3>Chào {booking.CustomerName},</h3>
                                      <p>Đặt bàn của bạn đã được xác nhận.</p>
                                      <ul>
                                        <li><strong>Mã đặt bàn:</strong> #{booking.Id}</li>
                                        <li><strong>Ngày:</strong> {booking.BookingDate:dd/MM/yyyy}</li>
                                        <li><strong>Giờ:</strong> {booking.BookingTime:HH\:mm}</li>
                                        <li><strong>Số khách:</strong> {booking.PartySize}</li>
                                      </ul>
                                      <p>Hẹn gặp bạn tại nhà hàng!</p>";
                            plain = $"Dat ban #{booking.Id} da duoc xac nhan cho {booking.BookingDate:dd/MM/yyyy} {booking.BookingTime:HH:mm}, so khach {booking.PartySize}.";
                            await _emailService.SendAsync(booking.Email, subject, html, plain);
                            break;
                        case BookingStatus.Cancelled:
                            subject = $"[Restaurantly] Huỷ đặt bàn #{booking.Id}";
                            html = $@"<h3>Chào {booking.CustomerName},</h3>
                                      <p>Rất tiếc, đặt bàn #{booking.Id} đã bị hủy.</p>
                                      <p>Nếu cần hỗ trợ, vui lòng liên hệ hotline.</p>";
                            plain = $"Dat ban #{booking.Id} da bi huy. Vui long lien he neu can ho tro.";
                            await _emailService.SendAsync(booking.Email, subject, html, plain);
                            break;
                        case BookingStatus.Completed:
                            subject = $"[Restaurantly] Cảm ơn bạn đã ghé thăm #{booking.Id}";
                            html = $@"<h3>Chào {booking.CustomerName},</h3>
                                      <p>Cảm ơn bạn đã dùng bữa tại Restaurantly. Rất mong sớm gặp lại!</p>";
                            plain = $"Cam on ban da ghe tham Restaurantly. Rat mong som gap lai!";
                            await _emailService.SendAsync(booking.Email, subject, html, plain);
                            break;
                    }
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt bàn để cập nhật.";
            }
            
            return RedirectToAction("Bookings");
        }

        // Menu Management
        public async Task<IActionResult> Menu()
        {
            var menuItems = await _context.MenuItems
                .OrderBy(m => m.Category)
                .ThenBy(m => m.Name)
                .ToListAsync();
            
            return View(menuItems);
        }

        public IActionResult CreateMenuItem()
        {
            // Đảm bảo model mặc định có IsAvailable = true để món mới hiển thị trên Menu
            var model = new MenuItem
            {
                IsAvailable = true,
                CreatedAt = DateTime.Now
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem(MenuItem menuItem, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var saved = await SaveImageFile(imageFile);
                    if (!string.IsNullOrEmpty(saved))
                    {
                        menuItem.ImageUrl = saved;
                    }
                }
                menuItem.CreatedAt = DateTime.Now;
                _context.MenuItems.Add(menuItem);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Thêm món ăn thành công.";
                return RedirectToAction("Menu");
            }
            
            return View(menuItem);
        }

        public async Task<IActionResult> EditMenuItem(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            
            if (menuItem == null)
            {
                return NotFound();
            }
            
            return View(menuItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMenuItem(MenuItem menuItem, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var saved = await SaveImageFile(imageFile);
                    if (!string.IsNullOrEmpty(saved))
                    {
                        menuItem.ImageUrl = saved;
                    }
                }
                _context.MenuItems.Update(menuItem);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Cập nhật món ăn thành công.";
                return RedirectToAction("Menu");
            }
            
            return View(menuItem);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleMenuItemAvailability(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            
            if (menuItem != null)
            {
                menuItem.IsAvailable = !menuItem.IsAvailable;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Đã {(menuItem.IsAvailable ? "kích hoạt" : "vô hiệu hóa")} món ăn.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy món ăn để cập nhật.";
            }
            
            return RedirectToAction("Menu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAllMenuItems()
        {
            var items = await _context.MenuItems.ToListAsync();
            foreach (var item in items)
            {
                item.IsAvailable = true;
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã kích hoạt tất cả món ăn.";
            return RedirectToAction("Menu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FixMissingImages()
        {
            var items = await _context.MenuItems.ToListAsync();
            int fixedCount = 0;
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ImageUrl))
                {
                    item.ImageUrl = $"https://placehold.co/600x400?text={Uri.EscapeDataString(item.Name)}";
                    fixedCount++;
                    continue;
                }

                // If points to /images/* but file does not exist in wwwroot/images, set placeholder
                if (item.ImageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = item.ImageUrl.TrimStart('/');
                    var fullPath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    if (!System.IO.File.Exists(fullPath))
                    {
                        item.ImageUrl = $"https://placehold.co/600x400?text={Uri.EscapeDataString(item.Name)}";
                        fixedCount++;
                    }
                }
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = fixedCount > 0 ? $"Đã thay ảnh placeholder cho {fixedCount} món." : "Không có ảnh lỗi cần sửa.";
            return RedirectToAction("Menu");
        }

        // Blog Management
        public async Task<IActionResult> Blog()
        {
            var posts = await _context.BlogEntries
                .OrderByDescending(b => b.PublishedAt)
                .ToListAsync();
            return View(posts);
        }

        public IActionResult CreateBlogEntry()
        {
            var model = new BlogEntry
            {
                PublishedAt = DateTime.UtcNow,
                Author = "Admin"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlogEntry(BlogEntry entry)
        {
            if (ModelState.IsValid)
            {
                _context.BlogEntries.Add(entry);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã tạo bài viết mới.";
                return RedirectToAction("Blog");
            }
            return View(entry);
        }

        public async Task<IActionResult> EditBlogEntry(int id)
        {
            var entry = await _context.BlogEntries.FindAsync(id);
            if (entry == null)
            {
                return NotFound();
            }
            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlogEntry(BlogEntry entry)
        {
            if (ModelState.IsValid)
            {
                _context.BlogEntries.Update(entry);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật bài viết.";
                return RedirectToAction("Blog");
            }
            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlogEntry(int id)
        {
            var entry = await _context.BlogEntries.FindAsync(id);
            if (entry != null)
            {
                _context.BlogEntries.Remove(entry);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa bài viết.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết.";
            }
            return RedirectToAction("Blog");
        }

        private async Task<string?> SaveImageFile(IFormFile file)
        {
            try
            {
                var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "menu");
                Directory.CreateDirectory(uploadRoot);
                var ext = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{(string.IsNullOrWhiteSpace(ext) ? ".jpg" : ext)}";
                var fullPath = Path.Combine(uploadRoot, fileName);
                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return $"/uploads/menu/{fileName}";
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            
            if (menuItem != null)
            {
                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Xóa món ăn thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy món ăn để xóa.";
            }
            
            return RedirectToAction("Menu");
        }

        // Order Management
        public async Task<IActionResult> Orders(string status = "", DateTime? date = null)
        {
            var orders = _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem).AsQueryable();
            
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
            {
                orders = orders.Where(o => o.Status == orderStatus);
            }
            
            if (date.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt.Date == date.Value.Date);
            }
            
            var result = await orders
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedDate = date;
            
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus status, string? notes = null)
        {
            var order = await _context.Orders.FindAsync(id);
            
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.Now;
                if (!string.IsNullOrEmpty(notes))
                {
                    order.Notes = notes;
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng để cập nhật.";
            }
            
            return RedirectToAction("Orders");
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
            {
                return NotFound();
            }
            
            return View(order);
        }

        // Review Management
        public async Task<IActionResult> Reviews(string status = "")
        {
            var reviews = _context.Reviews.Include(r => r.MenuItem).AsQueryable();
            
            if (!string.IsNullOrEmpty(status))
            {
                bool isApproved = status.ToLower() == "approved";
                reviews = reviews.Where(r => r.IsApproved == isApproved);
            }
            
            var result = await reviews
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            
            ViewBag.SelectedStatus = status;
            
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            
            if (review != null)
            {
                review.IsApproved = true;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Duyệt đánh giá thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá để duyệt.";
            }
            
            return RedirectToAction("Reviews");
        }

        [HttpPost]
        public async Task<IActionResult> RejectReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            
            if (review != null)
            {
                review.IsApproved = false;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Từ chối đánh giá thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá để từ chối.";
            }
            
            return RedirectToAction("Reviews");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Xóa đánh giá thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá để xóa.";
            }
            
            return RedirectToAction("Reviews");
        }

        // User Management
        public async Task<IActionResult> Users(string role = "")
        {
            var users = _context.Users.AsQueryable();
            
            if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, out var userRole))
            {
                users = users.Where(u => u.Role == userRole);
            }
            
            var result = await users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            
            ViewBag.SelectedRole = role;
            
            return View(result);
        }

        // Simple Chat dashboard page
        public IActionResult Chat()
        {
            return View();
        }

        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                // Check if username or email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == user.Username || u.Email == user.Email);
                
                if (existingUser != null)
                {
                    if (existingUser.Username == user.Username)
                        ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                    if (existingUser.Email == user.Email)
                        ModelState.AddModelError("Email", "Email đã tồn tại.");
                    
                    return View(user);
                }
                
                user.CreatedAt = DateTime.Now;
                user.IsActive = true;
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Tạo người dùng thành công.";
                return RedirectToAction("Users");
            }
            
            return View(user);
        }

        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(User user)
        {
            if (ModelState.IsValid)
            {
                // Check if username or email already exists (excluding current user)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id != user.Id && (u.Username == user.Username || u.Email == user.Email));
                
                if (existingUser != null)
                {
                    if (existingUser.Username == user.Username)
                        ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                    if (existingUser.Email == user.Email)
                        ModelState.AddModelError("Email", "Email đã tồn tại.");
                    
                    return View(user);
                }
                
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Cập nhật người dùng thành công.";
                return RedirectToAction("Users");
            }
            
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Đã {(user.IsActive ? "kích hoạt" : "vô hiệu hóa")} người dùng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng để cập nhật.";
            }
            
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Xóa người dùng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng để xóa.";
            }
            
            return RedirectToAction("Users");
        }

        // Database Connection Test
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                // Test database connection
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    // Get database info
                    var connectionString = _context.Database.GetConnectionString();
                    var providerName = _context.Database.ProviderName;
                    
                    // Get some basic stats
                    var bookingCount = await _context.Bookings.CountAsync();
                    var menuItemCount = await _context.MenuItems.CountAsync();
                    var userCount = await _context.Users.CountAsync();
                    
                    var result = new
                    {
                        Status = "Connected",
                        ConnectionString = connectionString,
                        Provider = providerName,
                        Statistics = new
                        {
                            Bookings = bookingCount,
                            MenuItems = menuItemCount,
                            Users = userCount
                        }
                    };
                    
                    return Json(result);
                }
                else
                {
                    return Json(new { Status = "Failed", Message = "Cannot connect to database" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        // Check Recent Bookings in SQL Server
        public async Task<IActionResult> CheckRecentBookings()
        {
            try
            {
                var recentBookings = await _context.Bookings
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .Select(b => new
                    {
                        b.Id,
                        b.CustomerName,
                        b.Email,
                        b.Phone,
                        b.BookingDate,
                        b.BookingTime,
                        b.PartySize,
                        b.Status,
                        b.CreatedAt,
                        b.SpecialRequests
                    })
                    .ToListAsync();

                var result = new
                {
                    Status = "Success",
                    TotalBookings = await _context.Bookings.CountAsync(),
                    RecentBookings = recentBookings,
                    DatabaseProvider = _context.Database.ProviderName,
                    ConnectionString = _context.Database.GetConnectionString()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        // Directly check data in SQLite source configured by DefaultConnection
        [HttpGet]
        public async Task<IActionResult> CheckSqliteBookings()
        {
            try
            {
                var sqliteConn = _config.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(sqliteConn))
                {
                    return Json(new { Status = "Error", Message = "DefaultConnection (SQLite) not found in configuration." });
                }

                var sqliteOptions = new DbContextOptionsBuilder<RestaurantDbContext>()
                    .UseSqlite(sqliteConn)
                    .Options;

                using var sqlite = new RestaurantDbContext(sqliteOptions);
                await sqlite.Database.EnsureCreatedAsync();

                var recent = await sqlite.Bookings
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .Select(b => new
                    {
                        b.Id,
                        b.CustomerName,
                        b.Email,
                        b.Phone,
                        b.BookingDate,
                        b.BookingTime,
                        b.PartySize,
                        b.Status,
                        b.CreatedAt,
                        b.SpecialRequests
                    })
                    .ToListAsync();

                var result = new
                {
                    Status = "Success",
                    Source = "SQLite",
                    TotalBookings = await sqlite.Bookings.CountAsync(),
                    RecentBookings = recent,
                    DatabaseProvider = sqlite.Database.ProviderName,
                    ConnectionString = sqliteConn
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        // Endpoint để xuất tất cả thông tin đặt bàn
        public async Task<IActionResult> ExportAllBookings()
        {
            try
            {
                var allBookings = await _context.Bookings
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new
                    {
                        Id = b.Id,
                        CustomerName = b.CustomerName,
                        Email = b.Email,
                        Phone = b.Phone,
                        BookingDate = b.BookingDate.ToString("dd/MM/yyyy"),
                        BookingTime = b.BookingTime,
                        PartySize = b.PartySize,
                        Status = b.Status,
                        SpecialRequests = b.SpecialRequests,
                        CreatedAt = b.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                    })
                    .ToListAsync();

                var result = new
                {
                    Status = "Success",
                    TotalBookings = allBookings.Count,
                    DatabaseProvider = _context.Database.ProviderName,
                    ConnectionString = _context.Database.GetConnectionString(),
                    AllBookings = allBookings
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        // Endpoint để xuất thông tin đặt bàn dạng CSV
        public async Task<IActionResult> ExportBookingsCSV()
        {
            try
            {
                var allBookings = await _context.Bookings
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("ID,Tên khách hàng,Email,Số điện thoại,Ngày đặt,Giờ đặt,Số người,Trạng thái,Yêu cầu đặc biệt,Ngày tạo");

                foreach (var booking in allBookings)
                {
                    csv.AppendLine($"{booking.Id},{booking.CustomerName},{booking.Email},{booking.Phone},{booking.BookingDate:dd/MM/yyyy},{booking.BookingTime},{booking.PartySize},{booking.Status},{booking.SpecialRequests?.Replace(",", ";")},{booking.CreatedAt:dd/MM/yyyy HH:mm:ss}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"DanhSachDatBan_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        // Endpoint truy xuất dữ liệu bằng SQL thuần
        public async Task<IActionResult> QueryBookingsSQL(string? sqlQuery = null)
        {
            try
            {
                // Các câu lệnh SQL mẫu an toàn
                var sampleQueries = new Dictionary<string, string>
                {
                    ["all"] = @"SELECT Id, CustomerName, Email, Phone, 
                               FORMAT(BookingDate, 'dd/MM/yyyy') AS NgayDat, 
                               FORMAT(BookingTime, 'HH:mm') AS GioDat, 
                               PartySize AS SoNguoi,
                               CASE Status 
                                   WHEN 0 THEN N'Chờ xác nhận' 
                                   WHEN 1 THEN N'Đã xác nhận' 
                                   WHEN 2 THEN N'Đã hoàn thành' 
                                   WHEN 3 THEN N'Đã hủy' 
                               END AS TrangThai,
                               SpecialRequests AS YeuCauDacBiet,
                               FORMAT(CreatedAt, 'dd/MM/yyyy HH:mm') AS NgayTao
                               FROM Bookings ORDER BY CreatedAt DESC",

                    ["stats"] = @"SELECT 
                                 CASE Status 
                                     WHEN 0 THEN N'Chờ xác nhận' 
                                     WHEN 1 THEN N'Đã xác nhận' 
                                     WHEN 2 THEN N'Đã hoàn thành' 
                                     WHEN 3 THEN N'Đã hủy' 
                                 END AS TrangThai,
                                 COUNT(*) AS SoLuong,
                                 SUM(PartySize) AS TongSoNguoi
                                 FROM Bookings GROUP BY Status ORDER BY Status",

                    ["today"] = @"SELECT Id, CustomerName, Email, Phone, 
                                 FORMAT(BookingDate, 'dd/MM/yyyy') AS NgayDat, 
                                 FORMAT(BookingTime, 'HH:mm') AS GioDat, 
                                 PartySize AS SoNguoi
                                 FROM Bookings 
                                 WHERE CAST(BookingDate AS DATE) = CAST(GETDATE() AS DATE)
                                 ORDER BY BookingTime",

                    ["thisweek"] = @"SELECT Id, CustomerName, Email, Phone, 
                                    FORMAT(BookingDate, 'dd/MM/yyyy') AS NgayDat, 
                                    FORMAT(BookingTime, 'HH:mm') AS GioDat, 
                                    PartySize AS SoNguoi
                                    FROM Bookings 
                                    WHERE BookingDate >= DATEADD(week, DATEDIFF(week, 0, GETDATE()), 0)
                                    AND BookingDate < DATEADD(week, DATEDIFF(week, 0, GETDATE()) + 1, 0)
                                    ORDER BY BookingDate, BookingTime"
                };

                // Nếu không có query, trả về danh sách các query mẫu
                if (string.IsNullOrEmpty(sqlQuery))
                {
                    return Json(new 
                    { 
                        Status = "Success",
                        Message = "Sử dụng tham số ?sqlQuery=<key> để thực thi câu lệnh",
                        AvailableQueries = sampleQueries.Keys.ToList(),
                        Examples = new 
                        {
                            All = "/Admin/QueryBookingsSQL?sqlQuery=all",
                            Stats = "/Admin/QueryBookingsSQL?sqlQuery=stats", 
                            Today = "/Admin/QueryBookingsSQL?sqlQuery=today",
                            ThisWeek = "/Admin/QueryBookingsSQL?sqlQuery=thisweek"
                        }
                    });
                }

                // Kiểm tra query có trong danh sách an toàn không
                if (!sampleQueries.ContainsKey(sqlQuery.ToLower()))
                {
                    return Json(new 
                    { 
                        Status = "Error", 
                        Message = "Query không hợp lệ. Chỉ chấp nhận: " + string.Join(", ", sampleQueries.Keys)
                    });
                }

                var sql = sampleQueries[sqlQuery.ToLower()];
                
                // Thực thi SQL bằng FromSqlRaw (an toàn hơn)
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                
                await _context.Database.OpenConnectionAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                var results = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }

                return Json(new 
                { 
                    Status = "Success",
                    Query = sqlQuery,
                    SQL = sql,
                    RowCount = results.Count,
                    Data = results
                });
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        // Endpoint tìm kiếm đặt bàn
        public async Task<IActionResult> SearchBookings(string? customerName = null, string? email = null, string? phone = null, DateTime? fromDate = null, DateTime? toDate = null, BookingStatus? status = null)
        {
            try
            {
                var query = _context.Bookings.AsQueryable();

                if (!string.IsNullOrEmpty(customerName))
                    query = query.Where(b => b.CustomerName.Contains(customerName!));

                if (!string.IsNullOrEmpty(email))
                    query = query.Where(b => b.Email.Contains(email!));

                if (!string.IsNullOrEmpty(phone))
                    query = query.Where(b => b.Phone.Contains(phone!));

                if (fromDate.HasValue)
                    query = query.Where(b => b.BookingDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(b => b.BookingDate <= toDate.Value);

                if (status.HasValue)
                    query = query.Where(b => b.Status == status.Value);

                var results = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new
                    {
                        Id = b.Id,
                        CustomerName = b.CustomerName,
                        Email = b.Email,
                        Phone = b.Phone,
                        BookingDate = b.BookingDate.ToString("dd/MM/yyyy"),
                        BookingTime = b.BookingTime.ToString("HH:mm"),
                        PartySize = b.PartySize,
                        Status = b.Status == BookingStatus.Pending ? "Chờ xác nhận" : 
                                b.Status == BookingStatus.Confirmed ? "Đã xác nhận" : 
                                b.Status == BookingStatus.Completed ? "Đã hoàn thành" : "Đã hủy",
                        SpecialRequests = b.SpecialRequests,
                        CreatedAt = b.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                    })
                    .ToListAsync();

                return Json(new 
                { 
                    Status = "Success",
                    SearchCriteria = new 
                    {
                        CustomerName = customerName,
                        Email = email,
                        Phone = phone,
                        FromDate = fromDate?.ToString("dd/MM/yyyy"),
                        ToDate = toDate?.ToString("dd/MM/yyyy"),
                        Status = status?.ToString()
                    },
                    RowCount = results.Count,
                    Data = results
                });
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        // Import all data from SQLite into current SQL Server database
        // Dev-only utility: copies Users, MenuItems, Bookings, Orders, OrderItems, Reviews
        [HttpPost]
        public async Task<IActionResult> ImportFromSqlite()
        {
            // Ensure current provider is SQL Server
            if (!_context.Database.IsSqlServer())
            {
                return Json(new { Status = "Error", Message = "Current provider is not SQL Server. Enable USE_SQLSERVER=true." });
            }

            var sqliteConn = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(sqliteConn))
            {
                return Json(new { Status = "Error", Message = "DefaultConnection (SQLite) not found in configuration." });
            }

            var sqliteOptions = new DbContextOptionsBuilder<RestaurantDbContext>()
                .UseSqlite(sqliteConn)
                .Options;

            using var sqlite = new RestaurantDbContext(sqliteOptions);

            try
            {
                // Quick connectivity check
                await sqlite.Database.EnsureCreatedAsync();

                // Begin transaction on SQL Server side
                using var tx = await _context.Database.BeginTransactionAsync();

                // Copy Users
                var users = await sqlite.Users.AsNoTracking().ToListAsync();
                if (users.Count > 0)
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Users ON");
                    foreach (var u in users)
                    {
                        if (!await _context.Users.AnyAsync(x => x.Id == u.Id))
                        {
                            _context.Users.Add(u);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Users OFF");
                }

                // Copy MenuItems
                var menuItems = await sqlite.MenuItems.AsNoTracking().ToListAsync();
                if (menuItems.Count > 0)
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.MenuItems ON");
                    foreach (var m in menuItems)
                    {
                        if (!await _context.MenuItems.AnyAsync(x => x.Id == m.Id))
                        {
                            _context.MenuItems.Add(m);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.MenuItems OFF");
                }

                // Copy Bookings
                var bookings = await sqlite.Bookings.AsNoTracking().ToListAsync();
                if (bookings.Count > 0)
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Bookings ON");
                    foreach (var b in bookings)
                    {
                        if (!await _context.Bookings.AnyAsync(x => x.Id == b.Id))
                        {
                            _context.Bookings.Add(b);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Bookings OFF");
                }

                // Copy Orders
                var orders = await sqlite.Orders.AsNoTracking().ToListAsync();
                if (orders.Count > 0)
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Orders ON");
                    foreach (var o in orders)
                    {
                        if (!await _context.Orders.AnyAsync(x => x.Id == o.Id))
                        {
                            _context.Orders.Add(o);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Orders OFF");
                }

                // Copy OrderItems
                var orderItems = await sqlite.OrderItems.AsNoTracking().ToListAsync();
                if (orderItems.Count > 0)
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.OrderItems ON");
                    foreach (var oi in orderItems)
                    {
                        if (!await _context.OrderItems.AnyAsync(x => x.Id == oi.Id))
                        {
                            _context.OrderItems.Add(oi);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.OrderItems OFF");
                }

                // Copy Reviews
                var reviews = await sqlite.Reviews.AsNoTracking().ToListAsync();
                if (reviews.Count > 0)
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Reviews ON");
                    foreach (var r in reviews)
                    {
                        if (!await _context.Reviews.AnyAsync(x => x.Id == r.Id))
                        {
                            _context.Reviews.Add(r);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Reviews OFF");
                }

                await tx.CommitAsync();

                var result = new
                {
                    Status = "Success",
                    Users = new { Source = users.Count, Target = await _context.Users.CountAsync() },
                    MenuItems = new { Source = menuItems.Count, Target = await _context.MenuItems.CountAsync() },
                    Bookings = new { Source = bookings.Count, Target = await _context.Bookings.CountAsync() },
                    Orders = new { Source = orders.Count, Target = await _context.Orders.CountAsync() },
                    OrderItems = new { Source = orderItems.Count, Target = await _context.OrderItems.CountAsync() },
                    Reviews = new { Source = reviews.Count, Target = await _context.Reviews.CountAsync() }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        // GET variant for dev convenience: /Admin/ImportFromSqliteConfirm?confirm=yes
        [HttpGet]
        public async Task<IActionResult> ImportFromSqliteConfirm(string confirm = "no")
        {
            if (!string.Equals(confirm, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { Status = "Info", Message = "Add ?confirm=yes to trigger import." });
            }

            return await ImportFromSqlite();
        }

        // Upsert-only sync for Bookings: insert missing, update existing differences
        [HttpPost]
        public async Task<IActionResult> SyncBookingsFromSqlite()
        {
            if (!_context.Database.IsSqlServer())
            {
                return Json(new { Status = "Error", Message = "Current provider is not SQL Server. Enable USE_SQLSERVER=true." });
            }

            var sqliteConn = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(sqliteConn))
            {
                return Json(new { Status = "Error", Message = "DefaultConnection (SQLite) not found in configuration." });
            }

            var sqliteOptions = new DbContextOptionsBuilder<RestaurantDbContext>()
                .UseSqlite(sqliteConn)
                .Options;

            using var sqlite = new RestaurantDbContext(sqliteOptions);

            try
            {
                await sqlite.Database.EnsureCreatedAsync();

                var srcBookings = await sqlite.Bookings.AsNoTracking().ToListAsync();
                int inserted = 0, updated = 0;

                foreach (var s in srcBookings)
                {
                    var t = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == s.Id);
                    if (t == null)
                    {
                        // Insert new
                        _context.Bookings.Add(s);
                        inserted++;
                    }
                    else
                    {
                        // Update fields if different
                        bool changed = false;
                        if (t.CustomerName != s.CustomerName) { t.CustomerName = s.CustomerName; changed = true; }
                        if (t.Email != s.Email) { t.Email = s.Email; changed = true; }
                        if (t.Phone != s.Phone) { t.Phone = s.Phone; changed = true; }
                        if (t.BookingDate != s.BookingDate) { t.BookingDate = s.BookingDate; changed = true; }
                        if (t.BookingTime != s.BookingTime) { t.BookingTime = s.BookingTime; changed = true; }
                        if (t.PartySize != s.PartySize) { t.PartySize = s.PartySize; changed = true; }
                        if (t.SpecialRequests != s.SpecialRequests) { t.SpecialRequests = s.SpecialRequests; changed = true; }
                        if (t.Status != s.Status) { t.Status = s.Status; changed = true; }
                        if (t.AdminNotes != s.AdminNotes) { t.AdminNotes = s.AdminNotes; changed = true; }
                        if (t.CreatedAt != s.CreatedAt) { t.CreatedAt = s.CreatedAt; changed = true; }

                        if (changed) { updated++; }
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { Status = "Success", Inserted = inserted, Updated = updated, SourceCount = srcBookings.Count, TargetCount = await _context.Bookings.CountAsync() });
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        // GET variant for convenience
        [HttpGet]
        public async Task<IActionResult> SyncBookingsFromSqliteConfirm(string confirm = "no")
        {
            if (!string.Equals(confirm, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { Status = "Info", Message = "Add ?confirm=yes to trigger sync." });
            }
            return await SyncBookingsFromSqlite();
        }
        // About Settings Management
        public IActionResult AboutSettings()
        {
            var model = new AboutSettingsViewModel();

            try
            {
                var configPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    var jsonText = System.IO.File.ReadAllText(configPath);
                    var root = JsonNode.Parse(jsonText) as JsonObject;
                    var about = root?["About"] as JsonObject;
                    model.Title = (string?)about?["Title"] ?? _config["About:Title"];
                    model.Lead = (string?)about?["Lead"] ?? _config["About:Lead"];
                    model.Story1 = (string?)about?["Story1"] ?? _config["About:Story1"];
                    model.Story2 = (string?)about?["Story2"] ?? _config["About:Story2"];
                    model.Story3 = (string?)about?["Story3"] ?? _config["About:Story3"];
                }
                else
                {
                    model.Title = _config["About:Title"];
                    model.Lead = _config["About:Lead"];
                    model.Story1 = _config["About:Story1"];
                    model.Story2 = _config["About:Story2"];
                    model.Story3 = _config["About:Story3"];
                }
            }
            catch
            {
                // Fallback to config on any error
                model.Title = _config["About:Title"];
                model.Lead = _config["About:Lead"];
                model.Story1 = _config["About:Story1"];
                model.Story2 = _config["About:Story2"];
                model.Story3 = _config["About:Story3"];
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AboutSettings(AboutSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var configPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                JsonObject root;
                if (System.IO.File.Exists(configPath))
                {
                    var jsonText = await System.IO.File.ReadAllTextAsync(configPath);
                    root = (JsonNode.Parse(jsonText) as JsonObject) ?? new JsonObject();
                }
                else
                {
                    root = new JsonObject();
                }

                var about = root["About"] as JsonObject ?? new JsonObject();
                about["Title"] = model.Title ?? string.Empty;
                about["Lead"] = model.Lead ?? string.Empty;
                about["Story1"] = model.Story1 ?? string.Empty;
                about["Story2"] = model.Story2 ?? string.Empty;
                about["Story3"] = model.Story3 ?? string.Empty;
                root["About"] = about;

                var jsonOut = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(configPath, jsonOut);

                TempData["Success"] = "Đã cập nhật nội dung trang About.";
                return RedirectToAction("AboutSettings");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Không thể lưu cấu hình: {ex.Message}";
                return View(model);
            }
        }
    }
}