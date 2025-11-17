using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;
using System.Text.Json;

namespace RestaurantMVC.Controllers
{
    public class OrderController : Controller
    {
        private readonly RestaurantDbContext _context;

        public OrderController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            var menuItems = await _context.MenuItems
                .Where(m => m.IsAvailable)
                .OrderBy(m => m.Category)
                .ThenBy(m => m.Name)
                .ToListAsync();

            return View(menuItems);
        }

        // POST: Order/AddToCart
        [HttpPost]
        public IActionResult AddToCart([FromBody] CartItem item)
        {
            try
            {
                var cart = GetCart();
                var existingItem = cart.FirstOrDefault(x => x.MenuItemId == item.MenuItemId);

                if (existingItem != null)
                {
                    existingItem.Quantity += item.Quantity;
                }
                else
                {
                    cart.Add(item);
                }

                SaveCart(cart);
                return Json(new { success = true, cartCount = cart.Sum(x => x.Quantity) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Order/Cart
        public async Task<IActionResult> Cart()
        {
            var cart = GetCart();
            var cartItems = new List<CartItemViewModel>();

            foreach (var item in cart)
            {
                var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                if (menuItem != null)
                {
                    cartItems.Add(new CartItemViewModel
                    {
                        MenuItemId = item.MenuItemId,
                        Name = menuItem.Name,
                        Price = menuItem.Price,
                        Quantity = item.Quantity,
                        TotalPrice = menuItem.Price * item.Quantity,
                        ImageUrl = menuItem.ImageUrl
                    });
                }
            }

            ViewBag.TotalAmount = cartItems.Sum(x => x.TotalPrice);
            return View(cartItems);
        }

        // POST: Order/UpdateCart
        [HttpPost]
        public IActionResult UpdateCart(int menuItemId, int quantity)
        {
            try
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(x => x.MenuItemId == menuItemId);

                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        cart.Remove(item);
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }
                    SaveCart(cart);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Order/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int menuItemId)
        {
            try
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(x => x.MenuItemId == menuItemId);

                if (item != null)
                {
                    cart.Remove(item);
                    SaveCart(cart);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Order/Checkout
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var cartItems = new List<CartItemViewModel>();
            foreach (var item in cart)
            {
                var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                if (menuItem != null)
                {
                    cartItems.Add(new CartItemViewModel
                    {
                        MenuItemId = item.MenuItemId,
                        Name = menuItem.Name,
                        Price = menuItem.Price,
                        Quantity = item.Quantity,
                        TotalPrice = menuItem.Price * item.Quantity
                    });
                }
            }

            ViewBag.CartItems = cartItems;
            ViewBag.TotalAmount = cartItems.Sum(x => x.TotalPrice);

            return View(new Order());
        }

        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Tính tổng tiền
                    decimal totalAmount = 0;
                    var orderItems = new List<OrderItem>();

                    foreach (var cartItem in cart)
                    {
                        var menuItem = await _context.MenuItems.FindAsync(cartItem.MenuItemId);
                        if (menuItem != null)
                        {
                            var orderItem = new OrderItem
                            {
                                MenuItemId = cartItem.MenuItemId,
                                Quantity = cartItem.Quantity,
                                UnitPrice = menuItem.Price,
                                TotalPrice = menuItem.Price * cartItem.Quantity
                            };
                            orderItems.Add(orderItem);
                            totalAmount += orderItem.TotalPrice;
                        }
                    }

                    order.TotalAmount = totalAmount;
                    order.CreatedAt = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Thêm order items
                    foreach (var item in orderItems)
                    {
                        item.OrderId = order.Id;
                        _context.OrderItems.Add(item);
                    }

                    await _context.SaveChangesAsync();

                    // Xóa giỏ hàng
                    ClearCart();

                    return RedirectToAction("Confirmation", new { id = order.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi đặt hàng: " + ex.Message);
                }
            }

            // Nếu có lỗi, hiển thị lại form
            var cartItems = new List<CartItemViewModel>();
            foreach (var item in cart)
            {
                var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                if (menuItem != null)
                {
                    cartItems.Add(new CartItemViewModel
                    {
                        MenuItemId = item.MenuItemId,
                        Name = menuItem.Name,
                        Price = menuItem.Price,
                        Quantity = item.Quantity,
                        TotalPrice = menuItem.Price * item.Quantity
                    });
                }
            }

            ViewBag.CartItems = cartItems;
            ViewBag.TotalAmount = cartItems.Sum(x => x.TotalPrice);

            return View(order);
        }

        // GET: Order/Confirmation
        public async Task<IActionResult> Confirmation(int id)
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

        // GET: Order/Track
        public IActionResult Track()
        {
            return View();
        }

        // POST: Order/Track
        [HttpPost]
        public async Task<IActionResult> Track(string email, int? orderId)
        {
            var query = _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem).AsQueryable();

            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(o => o.Email.Contains(email));
            }

            if (orderId.HasValue)
            {
                query = query.Where(o => o.Id == orderId.Value);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            ViewBag.Orders = orders;

            return View();
        }

        #region Helper Methods

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        private void ClearCart()
        {
            HttpContext.Session.Remove("Cart");
        }

        #endregion
    }

    // Helper classes
    public class CartItem
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class CartItemViewModel
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string? ImageUrl { get; set; }
    }
}