using Microsoft.AspNetCore.Mvc;
using RestaurantMVC.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace RestaurantMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RestaurantDbContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, RestaurantDbContext context, IConfiguration config, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _config = config;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            // Get featured menu items for homepage
            var featuredItems = await _context.MenuItems
                .Where(m => m.IsAvailable)
                .OrderBy(m => m.Id)
                .Take(6)
                .ToListAsync();
            
            ViewBag.FeaturedItems = featuredItems;
            return View();
        }

        public IActionResult About()
        {
            // Try reading latest values from appsettings.json to reflect admin edits immediately
            try
            {
                var configPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    var jsonText = System.IO.File.ReadAllText(configPath);
                    var doc = System.Text.Json.JsonDocument.Parse(jsonText);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("About", out var about))
                    {
                        ViewBag.AboutTitle = about.TryGetProperty("Title", out var title) ? title.GetString() : null;
                        ViewBag.AboutLead = about.TryGetProperty("Lead", out var lead) ? lead.GetString() : null;
                        ViewBag.AboutStory1 = about.TryGetProperty("Story1", out var s1) ? s1.GetString() : null;
                        ViewBag.AboutStory2 = about.TryGetProperty("Story2", out var s2) ? s2.GetString() : null;
                        ViewBag.AboutStory3 = about.TryGetProperty("Story3", out var s3) ? s3.GetString() : null;
                    }
                }
            }
            catch
            {
                // ignore, use configuration fallback
            }

            ViewBag.AboutTitle ??= _config["About:Title"] ?? "Về Restaurantly";
            ViewBag.AboutLead ??= _config["About:Lead"] ?? "Câu chuyện về hành trình mang đến những trải nghiệm ẩm thực tuyệt vời";
            ViewBag.AboutStory1 ??= _config["About:Story1"] ?? "Restaurantly được thành lập vào năm 2020 với mong muốn mang đến cho thực khách những trải nghiệm ẩm thực đích thực của Việt Nam trong không gian hiện đại và ấm cúng.";
            ViewBag.AboutStory2 ??= _config["About:Story2"] ?? "Chúng tôi tin rằng mỗi bữa ăn không chỉ là việc thưởng thức món ăn mà còn là cơ hội để kết nối với gia đình, bạn bè và tạo nên những kỷ niệm đẹp.";
            ViewBag.AboutStory3 ??= _config["About:Story3"] ?? "Với đội ngũ đầu bếp giàu kinh nghiệm và đam mê, chúng tôi cam kết mang đến những món ăn chất lượng cao được chế biến từ nguyên liệu tươi ngon nhất.";
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Contact(string name, string email, string subject, string message)
        {
            if (ModelState.IsValid)
            {
                // In a real application, you would send an email or save to database
                TempData["ContactSuccess"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
                return RedirectToAction("Contact");
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
