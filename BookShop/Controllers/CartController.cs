using BookShop.Extensions;
using BookShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Web;

namespace BookShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCart()
        {
            List<CartItem> cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            return cart;
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        private decimal CalculateTotalAmount()
        {
            var cart = GetCart();
            return cart.Sum(item => item.Price * item.Quantity);
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        public IActionResult AddToCart(int bookId, int quantity)
        {
            var book = _context.Books.SingleOrDefault(b => b.BookId == bookId);
            if (book != null && book.Stock > 0)
            {
                var cart = GetCart();
                var cartItem = cart.Find(item => item.BookId == bookId);
                if (cartItem != null)
                {
                    var newQuantity = cartItem.Quantity + quantity;
                    cartItem.Quantity = Math.Min(newQuantity, book.Stock);
                }
                else
                {
                    cart.Add(new CartItem { BookId = bookId, Title = book.Title, Price = book.Price, Quantity = Math.Min(quantity, book.Stock) });
                }
                SaveCart(cart);
            }
            return RedirectToAction("Details", "Books", new { id = bookId });
        }

        public IActionResult RemoveFromCart(int bookId)
        {
            var cart = GetCart();
            var cartItem = cart.Find(item => item.BookId == bookId);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var cartItems = GetCart();
            var totalAmount = CalculateTotalAmount();

            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                TotalAmount = totalAmount,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            var cartItems = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart");
            if (cartItems == null || !cartItems.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            var order = new Order
            {
                OrderDate = DateTime.Now,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                PaymentMethod = model.PaymentMethod,
                Status = OrderStatus.Waiting,
                Address = model.Address,
                OrderDetails = cartItems.Select(ci => new OrderDetail
                {
                    BookId = ci.BookId,
                    Quantity = ci.Quantity,
                    Price = ci.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                var book = await _context.Books.FindAsync(item.BookId);
                if (book != null)
                {
                    book.Stock -= item.Quantity;
                    if (book.Stock < 0) book.Stock = 0;
                }
            }
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");

            if (model.PaymentMethod == "Card")
            {
                string businessEmail = "sb-6qjjs29128401@business.example.com";
                string returnUrl = Url.Action("PaymentSuccessful", "Cart", new { orderId = order.OrderId }, Request.Scheme);
                string cancelUrl = Url.Action("Index", "Cart", null, Request.Scheme);

                var builder = new UriBuilder("https://www.sandbox.paypal.com/cgi-bin/webscr");
                var query = HttpUtility.ParseQueryString(string.Empty);

                query["cmd"] = "_cart";
                query["upload"] = "1";
                query["business"] = businessEmail;
                query["currency_code"] = "EUR";
                query["return"] = returnUrl;
                query["cancel_return"] = cancelUrl;

                int counter = 1;
                foreach (var item in cartItems)
                {
                    query[$"item_name_{counter}"] = item.Title;
                    query[$"amount_{counter}"] = item.Price.ToString("F2", CultureInfo.InvariantCulture);
                    query[$"quantity_{counter}"] = item.Quantity.ToString();
                    counter++;
                }

                builder.Query = query.ToString();
                string paypalUrl = builder.ToString();

                return Redirect(paypalUrl);
            }
            else
            {
                return RedirectToAction("PaymentSuccessful", new { orderId = order.OrderId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccessful(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return RedirectToAction("Index", "Home");

            return View(order);
        }

    }
}
