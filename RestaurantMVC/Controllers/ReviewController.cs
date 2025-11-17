using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;

namespace RestaurantMVC.Controllers
{
    public class ReviewController : Controller
    {
        private readonly RestaurantDbContext _context;

        public ReviewController(RestaurantDbContext context)
        {
            _context = context;
        }

        // AJAX endpoint to submit a review
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] Review review)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                // Check if menu item exists
                var menuItem = await _context.MenuItems.FindAsync(review.MenuItemId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Món ăn không tồn tại" });
                }

                review.CreatedAt = DateTime.Now;
                review.IsApproved = false; // Reviews need approval by default

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Cảm ơn bạn đã đánh giá! Đánh giá của bạn sẽ được hiển thị sau khi được duyệt." 
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi đánh giá" });
            }
        }

        // Get reviews for a menu item
        [HttpGet]
        public async Task<IActionResult> GetReviews(int menuItemId, int page = 1, int pageSize = 5)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Where(r => r.MenuItemId == menuItemId && r.IsApproved)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        id = r.Id,
                        customerName = r.CustomerName,
                        rating = r.Rating,
                        comment = r.Comment,
                        createdAt = r.CreatedAt.ToString("dd/MM/yyyy")
                    })
                    .ToListAsync();

                var totalReviews = await _context.Reviews
                    .CountAsync(r => r.MenuItemId == menuItemId && r.IsApproved);

                var averageRating = await _context.Reviews
                    .Where(r => r.MenuItemId == menuItemId && r.IsApproved)
                    .AverageAsync(r => (double?)r.Rating) ?? 0;

                return Json(new
                {
                    success = true,
                    reviews = reviews,
                    totalReviews = totalReviews,
                    averageRating = Math.Round(averageRating, 1),
                    hasMore = (page * pageSize) < totalReviews
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải đánh giá" });
            }
        }

        // Get review statistics for a menu item
        [HttpGet]
        public async Task<IActionResult> GetStats(int menuItemId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Where(r => r.MenuItemId == menuItemId && r.IsApproved)
                    .ToListAsync();

                if (!reviews.Any())
                {
                    return Json(new
                    {
                        success = true,
                        totalReviews = 0,
                        averageRating = 0,
                        ratingDistribution = new int[5]
                    });
                }

                var totalReviews = reviews.Count;
                var averageRating = reviews.Average(r => r.Rating);
                var ratingDistribution = new int[5];

                for (int i = 1; i <= 5; i++)
                {
                    ratingDistribution[i - 1] = reviews.Count(r => r.Rating == i);
                }

                return Json(new
                {
                    success = true,
                    totalReviews = totalReviews,
                    averageRating = Math.Round(averageRating, 1),
                    ratingDistribution = ratingDistribution
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thống kê" });
            }
        }
    }
}