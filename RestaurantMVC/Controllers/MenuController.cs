using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;

namespace RestaurantMVC.Controllers
{
    public class MenuController : Controller
    {
        private readonly RestaurantDbContext _context;

        public MenuController(RestaurantDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string category = "")
        {
            var menuItems = _context.MenuItems.Where(m => m.IsAvailable);
            
            if (!string.IsNullOrEmpty(category))
            {
                menuItems = menuItems.Where(m => m.Category == category);
            }
            
            var items = await menuItems.OrderBy(m => m.Category).ThenBy(m => m.Name).ToListAsync();
            
            // Get all categories for filter
            var categories = await _context.MenuItems
                .Where(m => m.IsAvailable)
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            
            if (menuItem == null || !menuItem.IsAvailable)
            {
                return NotFound();
            }
            
            return View(menuItem);
        }

        // Compact detail page for menu item
        [HttpGet]
        public async Task<IActionResult> DetailsCompact(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);

            if (menuItem == null || !menuItem.IsAvailable)
            {
                return NotFound();
            }

            return View("DetailsCompact", menuItem);
        }

        // AJAX Search endpoint for real-time search functionality
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            var searchResults = await _context.MenuItems
                .Where(m => m.IsAvailable && 
                           (m.Name.Contains(query) || 
                            m.Description.Contains(query) || 
                            m.Category.Contains(query)))
                .OrderBy(m => m.Name)
                .Take(10)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    price = m.Price,
                    category = m.Category,
                    imageUrl = m.ImageUrl
                })
                .ToListAsync();

            return Json(searchResults);
        }
    }
}