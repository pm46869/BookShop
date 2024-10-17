using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BookShop.Models;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> ManageBooks()
    {
        var books = await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Category)
            .ToListAsync();
        return View(books);
    }

    public IActionResult AddBook()
    {
        ViewBag.Authors = new SelectList(_context.Authors, "AuthorId", "Name");
        ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddBook(Book book)
    {
        book.Stock = book.Stock > 0 ? book.Stock : 0;
        _context.Add(book);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(ManageBooks));
    }

    public async Task<IActionResult> EditBook(int? id)
    {
        if (id == null) return NotFound();

        var book = await _context.Books.FindAsync(id);
        if (book == null) return NotFound();

        ViewBag.Authors = new SelectList(_context.Authors, "AuthorId", "Name", book.AuthorId);
        ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name", book.CategoryId);
        return View(book);
    }

    [HttpPost]
    public async Task<IActionResult> EditBook(int id, Book book)
    {
        if (id != book.BookId) return NotFound();

        try
        {
            _context.Update(book);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Books.Any(e => e.BookId == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        return RedirectToAction(nameof(ManageBooks));

    }


    public async Task<IActionResult> ListOrders(string sortOrder, DateTime? startDate, DateTime? endDate, string statusFilter)
    {
        ViewData["OrderIdSortParam"] = string.IsNullOrEmpty(sortOrder) ? "orderid_desc" : "";
        ViewData["UsernameSortParam"] = sortOrder == "username" ? "username_desc" : "username";
        ViewData["DateSortParam"] = sortOrder == "date" ? "date_desc" : "date";
        ViewData["StatusSortParam"] = sortOrder == "status" ? "status_desc" : "status";

        var ordersQuery = _context.Orders
            .Include(o => o.OrderDetails)
            .Include(o => o.User)
            .AsQueryable();

        if (startDate.HasValue && endDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate.Value && o.OrderDate <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse(statusFilter, out OrderStatus status))
        {
            ordersQuery = ordersQuery.Where(o => o.Status == status);
        }

        switch (sortOrder)
        {
            case "orderid_desc":
                ordersQuery = ordersQuery.OrderByDescending(o => o.OrderId);
                break;
            case "username":
                ordersQuery = ordersQuery.OrderBy(o => o.User.UserName);
                break;
            case "username_desc":
                ordersQuery = ordersQuery.OrderByDescending(o => o.User.UserName);
                break;
            case "date":
                ordersQuery = ordersQuery.OrderBy(o => o.OrderDate);
                break;
            case "date_desc":
                ordersQuery = ordersQuery.OrderByDescending(o => o.OrderDate);
                break;
            case "status":
                ordersQuery = ordersQuery.OrderBy(o => o.Status);
                break;
            case "status_desc":
                ordersQuery = ordersQuery.OrderByDescending(o => o.Status);
                break;
            default:
                ordersQuery = ordersQuery.OrderBy(o => o.OrderId);
                break;
        }

        var orders = await ordersQuery.ToListAsync();
        return View(orders);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = status;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(ListOrders));
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // User Management
    public async Task<IActionResult> ListUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        return View(users);
    }

    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new EditUserViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        user.UserName = model.UserName;
        user.Email = model.Email;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            return RedirectToAction("ListUsers");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }

}
