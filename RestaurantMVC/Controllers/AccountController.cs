using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;
using System.Security.Claims;

namespace RestaurantMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly RestaurantDbContext _context;

        public AccountController(RestaurantDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // In production, use proper password hashing
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && 
                                            u.Password == model.Password && 
                                            u.IsActive);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Role, user.Role.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    // Update last login
                    user.LastLoginAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return RedirectToAction("Index", "Admin");
                }

                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            // Check if admin already exists
            var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            if (existingAdmin != null)
            {
                TempData["Info"] = "Admin user already exists. Username: admin, Password: admin123";
                return RedirectToAction("Login");
            }

            // Create admin user
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@restaurant.com",
                Password = "admin1234", // In production, this should be hashed
                FullName = "Administrator",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            // Also seed some menu items if they don't exist
            if (!await _context.MenuItems.AnyAsync())
            {
                var menuItems = new List<MenuItem>
                {
                    new MenuItem { Name = "Phở Bò", Description = "Phở bò truyền thống với nước dùng đậm đà", Price = 65000, Category = "Món chính", ImageUrl = "/images/pho-bo.jpg", IsAvailable = true },
                    new MenuItem { Name = "Bún Chả", Description = "Bún chả Hà Nội với thịt nướng thơm ngon", Price = 55000, Category = "Món chính", ImageUrl = "/images/bun-cha.jpg", IsAvailable = true },
                    new MenuItem { Name = "Gỏi Cuốn", Description = "Gỏi cuốn tôm thịt tươi ngon", Price = 35000, Category = "Khai vị", ImageUrl = "/images/goi-cuon.jpg", IsAvailable = true },
                    new MenuItem { Name = "Chả Cá Lã Vọng", Description = "Chả cá truyền thống với thì là và hành", Price = 85000, Category = "Món chính", ImageUrl = "/images/cha-ca.jpg", IsAvailable = true },
                    new MenuItem { Name = "Bánh Mì", Description = "Bánh mì thịt nguội với rau củ tươi", Price = 25000, Category = "Món nhẹ", ImageUrl = "/images/banh-mi.jpg", IsAvailable = true },
                    new MenuItem { Name = "Cà Phê Sữa Đá", Description = "Cà phê sữa đá truyền thống", Price = 20000, Category = "Đồ uống", ImageUrl = "/images/ca-phe.jpg", IsAvailable = true }
                };

                _context.MenuItems.AddRange(menuItems);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Admin user created successfully! Username: admin, Password: admin123";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}