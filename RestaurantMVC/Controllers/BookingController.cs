using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;
using RestaurantMVC.Services;

namespace RestaurantMVC.Controllers
{
    public class BookingController : Controller
    {
        private readonly RestaurantDbContext _context;
        private readonly IEmailService _emailService;

        public BookingController(RestaurantDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var menuItems = await _context.MenuItems
                .Where(m => m.IsAvailable)
                .OrderBy(m => m.Category)
                .ThenBy(m => m.Name)
                .ToListAsync();

            ViewBag.MenuItems = menuItems;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string bookingTime, string customerName, string email, string phone, DateTime bookingDate, int partySize, string specialRequests = "")
        {
            try
            {
                Console.WriteLine($"Received booking request - Time: {bookingTime}, Customer: {customerName}, Email: {email}, Phone: {phone}, Date: {bookingDate}, Party Size: {partySize}");
                
                // Parse the time string to DateTime (time only)
                if (!DateTime.TryParse(bookingTime, out DateTime parsedTime))
                {
                    Console.WriteLine($"Failed to parse booking time: {bookingTime}");
                    ModelState.AddModelError("BookingTime", "Giờ đặt bàn không hợp lệ.");
                    var errorBooking = new Booking 
                    { 
                        CustomerName = customerName, 
                        Email = email, 
                        Phone = phone, 
                        BookingDate = bookingDate, 
                        PartySize = partySize, 
                        SpecialRequests = specialRequests ?? "" 
                    };
                    return View("Index", errorBooking);
                }
                
                var booking = new Booking
                {
                    CustomerName = customerName,
                    Email = email,
                    Phone = phone,
                    BookingDate = bookingDate,
                    BookingTime = parsedTime, // Now using DateTime
                    PartySize = partySize,
                    SpecialRequests = specialRequests ?? ""
                };
                
                Console.WriteLine($"Created booking object - Time: {booking.BookingTime:HH:mm}");
                Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
                
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState is invalid:");
                    foreach (var error in ModelState)
                    {
                        Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                    return View("Index", booking);
                }
                else
                {
                    Console.WriteLine("ModelState is valid, proceeding with booking...");
                    
                    // Check if restaurant is open (example: 10:00 AM to 10:00 PM)
                    var openTime = new TimeSpan(10, 0, 0);
                    var closeTime = new TimeSpan(22, 0, 0);
                    var bookingTimeSpan = booking.BookingTime.TimeOfDay;
                    Console.WriteLine($"Restaurant hours: {openTime} - {closeTime}, Booking time: {bookingTimeSpan}");
                    
                    if (bookingTimeSpan < openTime || bookingTimeSpan > closeTime)
                    {
                        Console.WriteLine("Booking time is outside restaurant hours");
                        ModelState.AddModelError("BookingTime", "Nhà hàng mở cửa từ 10:00 đến 22:00.");
                        return View("Index", booking);
                    }
                    
                    Console.WriteLine("Checking availability...");
                    // Simplified availability check to avoid LINQ translation issues
                    var existingBookingsCount = await _context.Bookings
                        .Where(b => b.BookingDate.Date == booking.BookingDate.Date &&
                                   b.BookingTime.TimeOfDay == booking.BookingTime.TimeOfDay &&
                                   (int)b.Status != 2) // Not cancelled
                        .CountAsync();
                    
                    Console.WriteLine($"Existing bookings at this time: {existingBookingsCount}");
                    
                    if (existingBookingsCount >= 5) // Reduced limit for testing
                    {
                        Console.WriteLine("Time slot is full");
                        ModelState.AddModelError("", "Khung giờ này đã đầy. Vui lòng chọn thời gian khác.");
                        return View("Index", booking);
                    }
                    
                    booking.CreatedAt = DateTime.Now;
                    booking.Status = BookingStatus.Pending;
                    Console.WriteLine($"Setting booking status to: {booking.Status}, CreatedAt: {booking.CreatedAt}");
                    
                    Console.WriteLine("Adding booking to database...");
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"Booking saved successfully with ID: {booking.Id}");

                    // Send email acknowledgment to customer
                    if (!string.IsNullOrWhiteSpace(booking.Email))
                    {
                        var subject = $"[Restaurantly] Xác nhận yêu cầu đặt bàn #{booking.Id}";
                        var confirmationUrl = Url.Action("Confirmation", "Booking", new { id = booking.Id }, Request.Scheme);
                        var html = $@"<h3>Chào {booking.CustomerName},</h3>
                            <p>Chúng tôi đã nhận yêu cầu đặt bàn của bạn và sẽ sớm liên hệ xác nhận.</p>
                            <ul>
                              <li><strong>Mã đặt bàn:</strong> #{booking.Id}</li>
                              <li><strong>Ngày:</strong> {booking.BookingDate:dd/MM/yyyy}</li>
                              <li><strong>Giờ:</strong> {booking.BookingTime:HH:mm}</li>
                              <li><strong>Số khách:</strong> {booking.PartySize}</li>
                            </ul>
                            <p>Bạn có thể theo dõi trạng thái tại: <a href=""{confirmationUrl}"">Trang xác nhận</a>.</p>
                            <p>Trân trọng,<br/>Restaurantly</p>";
                        var plain = $"Chao {booking.CustomerName},\n\nChung toi da nhan yeu cau dat ban #{booking.Id} cho {booking.BookingDate:dd/MM/yyyy} {booking.BookingTime:HH:mm}, so khach {booking.PartySize}.\n\nTrang xac nhan: {confirmationUrl}\n\nRestaurantly";
                        _ = await _emailService.SendAsync(booking.Email, subject, html, plain);
                    }
                    
                    TempData["BookingSuccess"] = $"Đặt bàn thành công! Mã đặt bàn của bạn là: {booking.Id}. Chúng tôi sẽ liên hệ xác nhận sớm nhất.";
                    Console.WriteLine($"Redirecting to Confirmation with ID: {booking.Id}");
                    return RedirectToAction("Confirmation", new { id = booking.Id });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt bàn. Vui lòng thử lại.");
                
                var errorBooking = new Booking 
                { 
                    CustomerName = customerName, 
                    Email = email, 
                    Phone = phone, 
                    BookingDate = bookingDate, 
                    PartySize = partySize, 
                    SpecialRequests = specialRequests ?? "" 
                };
                return View("Index", errorBooking);
            }
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            
            if (booking == null)
            {
                return NotFound();
            }
            
            return View(booking);
        }

        public IActionResult Check()
        {
            return View();
        }

        public IActionResult Test()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> Check(string? email, string? phone, string? customerName, int? bookingId, string searchType = "email")
        {
            email = email?.Trim();
            phone = phone?.Trim();
            customerName = customerName?.Trim();

            void SetFormState()
            {
                ViewBag.SearchType = searchType;
                ViewBag.SearchEmail = email;
                ViewBag.SearchPhone = phone;
                ViewBag.SearchName = customerName;
                ViewBag.SearchId = bookingId;
            }

            // Ensure the form retains user input
            SetFormState();

            var bookings = _context.Bookings.AsQueryable();

            if (string.Equals(searchType, "email", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    ModelState.AddModelError("email", "Vui lòng nhập email");
                    ViewBag.HasError = true;
                    ViewBag.Status = "form";
                    return View();
                }

                var normalizedEmail = email.ToLowerInvariant();
                bookings = bookings.Where(b => b.Email != null && b.Email.ToLower() == normalizedEmail);
            }
            else if (string.Equals(searchType, "phone", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(customerName))
                {
                    ModelState.AddModelError("phone", "Vui lòng nhập số điện thoại hoặc tên khách hàng");
                    ViewBag.HasError = true;
                    ViewBag.Status = "form";
                    return View();
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    bookings = bookings.Where(b => b.Phone != null && EF.Functions.Like(b.Phone, $"%{phone}%"));
                }

                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    var normalizedName = customerName.ToLowerInvariant();
                    bookings = bookings.Where(b => b.CustomerName != null && EF.Functions.Like(b.CustomerName.ToLower(), $"%{normalizedName}%"));
                }
            }
            else if (string.Equals(searchType, "code", StringComparison.OrdinalIgnoreCase))
            {
                if (!bookingId.HasValue)
                {
                    ModelState.AddModelError("bookingId", "Vui lòng nhập mã đặt bàn");
                    ViewBag.HasError = true;
                    ViewBag.Status = "form";
                    return View();
                }

                bookings = bookings.Where(b => b.Id == bookingId.Value);
            }
            else
            {
                // Fallback: no recognized search type
                ViewBag.HasError = true;
                ViewBag.Status = "form";
                return View();
            }

            var results = await bookings.OrderByDescending(b => b.CreatedAt).ToListAsync();

            ViewBag.Bookings = results;

            if (results.Any())
            {
                ViewBag.Status = "found";
            }
            else
            {
                ViewBag.Status = "notfound";
            }
            
            return View();
        }
    }
}