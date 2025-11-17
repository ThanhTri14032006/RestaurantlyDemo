using Microsoft.AspNetCore.Mvc;
using RestaurantMVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace RestaurantMVC.Controllers
{
    // Đổi tên controller để tránh xung đột tên với định nghĩa khác
    // Áp dụng attribute routing để vẫn phục vụ đường dẫn /Blog
    [Route("Blog")]
    public class BlogController : Controller
    {
        private readonly RestaurantDbContext _context;

        public BlogController(RestaurantDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var posts = await _context.BlogEntries
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();
            return View("~/Views/Blog/Index.cshtml", posts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.BlogEntries.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }
            return View("~/Views/Blog/Details.cshtml", post);
        }
    }
}