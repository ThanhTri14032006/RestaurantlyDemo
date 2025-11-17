using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RestaurantMVC.Models
{
    public class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options)
        {
        }
        
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<BlogEntry> BlogEntries { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure MenuItem
            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
            });
            
            // Configure Booking
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SpecialRequests).HasMaxLength(1000);
                entity.Property(e => e.AdminNotes).HasMaxLength(1000);
            });
            
            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FullName).HasMaxLength(200);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });
            
            // Configure Review
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Comment).HasMaxLength(1000);
                entity.HasOne(e => e.MenuItem)
                      .WithMany()
                      .HasForeignKey(e => e.MenuItemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DeliveryAddress).HasMaxLength(500);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
            });
            
            // Configure OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)");
                
                entity.HasOne(e => e.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.MenuItem)
                      .WithMany()
                      .HasForeignKey(e => e.MenuItemId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure BlogEntry
            modelBuilder.Entity<BlogEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Excerpt).HasMaxLength(500);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
                entity.Property(e => e.PublishedAt).HasColumnType("datetime2");
                entity.Property(e => e.Author).HasMaxLength(100);
            });

            // Configure ChatMessage
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ConversationId).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Sender).IsRequired().HasMaxLength(16);
                entity.Property(e => e.DisplayName).HasMaxLength(100);
                entity.Property(e => e.Text).IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
                entity.HasIndex(e => e.ConversationId);
            });
            
            SeedData(modelBuilder);
        }
        
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Users with static DateTime
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@restaurant.com",
                    Password = "admin123", // In production, this should be hashed
                    FullName = "Administrator",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc)
                }
            );

            // Seed Menu Items
            modelBuilder.Entity<MenuItem>().HasData(
                new MenuItem { Id = 1, Name = "Phở Bò", Description = "Phở bò truyền thống với nước dùng đậm đà", Price = 65000, Category = "Món chính", ImageUrl = "/images/pho-bo.jpg", IsAvailable = true },
                new MenuItem { Id = 2, Name = "Bún Chả", Description = "Bún chả Hà Nội với thịt nướng thơm ngon", Price = 55000, Category = "Món chính", ImageUrl = "/images/bun-cha.jpg", IsAvailable = true },
                new MenuItem { Id = 3, Name = "Gỏi Cuốn", Description = "Gỏi cuốn tôm thịt tươi ngon", Price = 35000, Category = "Khai vị", ImageUrl = "/images/goi-cuon.jpg", IsAvailable = true },
                new MenuItem { Id = 4, Name = "Chả Cá Lã Vọng", Description = "Chả cá truyền thống với thì là và hành", Price = 85000, Category = "Món chính", ImageUrl = "/images/cha-ca.jpg", IsAvailable = true },
                new MenuItem { Id = 5, Name = "Bánh Mì", Description = "Bánh mì thịt nguội với rau củ tươi", Price = 25000, Category = "Món nhẹ", ImageUrl = "/images/banh-mi.jpg", IsAvailable = true },
                new MenuItem { Id = 6, Name = "Cà Phê Sữa Đá", Description = "Cà phê sữa đá truyền thống", Price = 20000, Category = "Đồ uống", ImageUrl = "/images/ca-phe.jpg", IsAvailable = true }
            );

            // Seed Blog Entries
            modelBuilder.Entity<BlogEntry>().HasData(
                new BlogEntry
                {
                    Id = 1,
                    Title = "Khai trương mùa lễ hội",
                    Excerpt = "Những món mới mùa lễ hội và ưu đãi đặc biệt trong tháng này.",
                    Content = "Mùa lễ hội đã đến! Restaurantly ra mắt bộ sưu tập món ăn theo mùa với nguyên liệu tươi ngon nhất. Đừng bỏ lỡ các ưu đãi đặc biệt dành cho khách hàng thân thiết.",
                    ImageUrl = "https://images.unsplash.com/photo-1551218808-94e220e084d2?w=1200",
                    PublishedAt = DateTime.SpecifyKind(new DateTime(2024, 10, 1), DateTimeKind.Utc),
                    Author = "Restaurantly"
                },
                new BlogEntry
                {
                    Id = 2,
                    Title = "Bí quyết phở ngon",
                    Excerpt = "Chia sẻ bí quyết nước dùng đậm đà và chọn nguyên liệu chuẩn.",
                    Content = "Phở ngon bắt đầu từ nước dùng. Chúng tôi hầm xương bò trong nhiều giờ với các loại gia vị truyền thống để đạt được vị ngọt tự nhiên.",
                    ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=1200",
                    PublishedAt = DateTime.SpecifyKind(new DateTime(2024, 10, 3), DateTimeKind.Utc),
                    Author = "Bếp trưởng"
                }
            );
        }
    }
}