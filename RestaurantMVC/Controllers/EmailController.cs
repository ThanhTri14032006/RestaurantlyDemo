using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMVC.Services;

namespace RestaurantMVC.Controllers
{
    [Authorize]
    public class EmailController : Controller
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // GET /Email/Test?to=recipient@example.com
        [HttpGet]
        public async Task<IActionResult> Test(string to)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                return BadRequest("Thiếu tham số 'to'.");
            }

            var ok = await _emailService.SendAsync(
                to,
                "[Restaurantly] Email test",
                "<h3>Đây là email test từ hệ thống Restaurantly</h3><p>Nếu bạn nhận được thư này, cấu hình SMTP đã hoạt động.</p>",
                "Day la email test tu he thong Restaurantly."
            );

            if (ok)
            {
                return Ok($"Đã gửi email test tới {to}");
            }
            else
            {
                return StatusCode(500, "Không gửi được email test (SMTP có thể đang tắt hoặc cấu hình sai)");
            }
        }
    }
}