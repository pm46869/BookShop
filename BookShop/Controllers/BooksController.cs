using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookShop.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;

    public BooksController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchString, int? categoryId, int? authorId, decimal? minPrice, decimal? maxPrice)
    {
        ViewData["Categories"] = new SelectList(_context.Categories, "CategoryId", "Name");
        ViewData["Authors"] = new SelectList(_context.Authors, "AuthorId", "Name");

        var booksQuery = from b in _context.Books
                         .Include(b => b.Author)
                         .Include(b => b.Category)
                         .Include(b => b.Reviews)
                         where b.Stock > 0
                         select b;

        if (!string.IsNullOrEmpty(searchString))
        {
            booksQuery = booksQuery.Where(s => s.Title.Contains(searchString));
        }

        if (categoryId.HasValue && categoryId > 0)
        {
            booksQuery = booksQuery.Where(x => x.CategoryId == categoryId);
        }

        if (authorId.HasValue && authorId > 0)
        {
            booksQuery = booksQuery.Where(x => x.AuthorId == authorId);
        }

        if (minPrice.HasValue)
        {
            booksQuery = booksQuery.Where(x => x.Price >= minPrice);
        }

        if (maxPrice.HasValue)
        {
            booksQuery = booksQuery.Where(x => x.Price <= maxPrice);
        }

        var books = await booksQuery.ToListAsync();
        var booksWithAverageRatings = books.Select(b => new
        {
            Book = b,
            AverageRating = b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0
        });

        ViewBag.BooksWithAverageRatings = booksWithAverageRatings;
        return View(books);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Include(b => b.Reviews)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.BookId == id);

        if (book == null)
        {
            return NotFound();
        }

        double? averageRating = book.Reviews?.Count > 0 ? (double?)book.Reviews.Average(r => r.Rating) : null;
        ViewBag.AverageRating = averageRating;

        return View(book);
    }

    public async Task<IActionResult> AddReview(int bookId, string content, int rating)
    {
        if (ModelState.IsValid)
        {
            Review newReview = new Review
            {
                BookId = bookId,
                Content = content,
                Rating = rating,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            _context.Add(newReview);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = bookId });
        }
        return View();
    }

}
