using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Linq;
using RestaurantMVC.Services;

var builder = WebApplication.CreateBuilder(args);

// Force explicit URLs to avoid random/occupied dev ports
var urlsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(urlsEnv))
{
    builder.WebHost.UseUrls(urlsEnv);
}
else
{
    builder.WebHost.UseUrls("http://127.0.0.1:5298");
}

// Add services to the container.
builder.Services.AddControllersWithViews();

// Email service (SMTP đơn giản)
builder.Services.AddSingleton<IEmailService, EmailService>();

// Add Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Entity Framework
builder.Services.AddDbContext<RestaurantDbContext>(options =>
{
    // Allow forcing SQL Server via env var USE_SQLSERVER=true
    var useSqlServerEnv = Environment.GetEnvironmentVariable("USE_SQLSERVER");
    var useSqlServer = !string.IsNullOrEmpty(useSqlServerEnv) && useSqlServerEnv.Equals("true", StringComparison.OrdinalIgnoreCase);

    if (useSqlServer || !builder.Environment.IsDevelopment())
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection"));
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Initialize database per provider
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

    // Ensure chat schema once at startup (avoid per-request heavy SQL)
    try
    {
        ChatRepository.EnsureSchema(db);
    }
    catch { }

    // If using SQL Server, avoid applying provider-incompatible migrations at startup
    if (db.Database.IsSqlServer())
    {
        try
        {
            // Skip automatic migrations if there are any pending ones
            // This prevents failures when a migration targets SQLite column types
            var pending = db.Database.GetPendingMigrations();
            if (pending.Any())
            {
                // Intentionally skip applying migrations here. Use CLI to update DB.
                // dotnet ef database update
            }

            // Seed minimal data for SQL Server if empty
            if (!db.Users.Any(u => u.Username == "admin"))
            {
                db.Users.Add(new User
                {
                    Username = "admin",
                    Email = "admin@restaurant.com",
                    Password = "admin123",
                    FullName = "Administrator",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }

            if (!db.MenuItems.Any())
            {
                db.MenuItems.AddRange(new []
                {
                    new MenuItem { Name = "Phở Bò", Description = "Phở bò truyền thống với nước dùng đậm đà", Price = 65000, Category = "Món chính", ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=800", IsAvailable = true, CreatedAt = DateTime.UtcNow },
                    new MenuItem { Name = "Bún Chả", Description = "Thịt nướng ăn kèm bún và rau sống", Price = 55000, Category = "Món chính", ImageUrl = "https://images.unsplash.com/photo-1512058564366-18510be2f6c3?w=800", IsAvailable = true, CreatedAt = DateTime.UtcNow },
                    new MenuItem { Name = "Gỏi Cuốn", Description = "Cuốn tươi với tôm thịt và rau", Price = 45000, Category = "Món nhẹ", ImageUrl = "https://images.unsplash.com/photo-1544025162-4a0bdf1124a1?w=800", IsAvailable = true, CreatedAt = DateTime.UtcNow },
                    new MenuItem { Name = "Cà Phê Sữa", Description = "Cà phê đá pha sữa đặc", Price = 30000, Category = "Đồ uống", ImageUrl = "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=800", IsAvailable = true, CreatedAt = DateTime.UtcNow },
                    new MenuItem { Name = "Trà Đào", Description = "Trà đào mát lạnh với miếng đào", Price = 35000, Category = "Đồ uống", ImageUrl = "https://images.unsplash.com/photo-1541976076758-347226ffbfa2?w=800", IsAvailable = true, CreatedAt = DateTime.UtcNow },
                    new MenuItem { Name = "Chè Khúc Bạch", Description = "Chè thanh mát với sữa và hạnh nhân", Price = 40000, Category = "Món nhẹ", ImageUrl = "https://images.unsplash.com/photo-1495147466023-ac5c588e2cbb?w=800", IsAvailable = true, CreatedAt = DateTime.UtcNow }
                });
                db.SaveChanges();
            }
        }
        catch
        {
            // Ignore startup DB initialization errors in SQL Server
        }
    }
    else if (app.Environment.IsDevelopment() && db.Database.IsSqlite())
    {
        // For SQLite in Development, ensure schema exists without migrations
        var created = db.Database.EnsureCreated();

        // Seed minimal Dev data if empty
        try
        {
            if (!db.Users.Any())
            {
                db.Users.Add(new User
                {
                    Username = "admin",
                    Email = "admin@restaurant.com",
                    Password = "admin123", // Dev only; in prod use hashing
                    FullName = "Administrator",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (!db.MenuItems.Any())
            {
                db.MenuItems.AddRange(
                    new MenuItem { Name = "Phở Bò", Description = "Phở bò truyền thống với nước dùng đậm đà", Price = 65000m, Category = "Món chính", ImageUrl = "/images/pho-bo.jpg", IsAvailable = true, CreatedAt = DateTime.UtcNow },
                    new MenuItem { Name = "Bún Chả", Description = "Bún chả Hà Nội với thịt nướng thơm ngon", Price = 55000m, Category = "Món chính", ImageUrl = "/images/bun-cha.jpg", IsAvailable = true, CreatedAt = DateTime.UtcNow },
                    new MenuItem { Name = "Gỏi Cuốn", Description = "Gỏi cuốn tôm thịt tươi ngon", Price = 35000m, Category = "Khai vị", ImageUrl = "/images/goi-cuon.jpg", IsAvailable = true, CreatedAt = DateTime.UtcNow }
                );
            }

            if (!db.Reviews.Any() && db.MenuItems.Any())
            {
                var firstItemId = db.MenuItems.Select(m => m.Id).FirstOrDefault();
                db.Reviews.Add(new Review
                {
                    MenuItemId = firstItemId,
                    CustomerName = "Khách Ẩn Danh",
                    Email = "anon@example.com",
                    Rating = 5,
                    Comment = "Món ăn rất ngon!",
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = true
                });
            }

            db.SaveChanges();
        }
        catch
        {
            // Ignore seeding errors in dev
        }
    }
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
