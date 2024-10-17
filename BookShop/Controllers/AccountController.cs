using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookShop.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Text.Encodings.Web;

namespace BookShop.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> OrderHistory(string sortOrder)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return View("Error");
            }

            IQueryable<Order> ordersQuery = _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book);

            ViewData["DateSortParam"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["StatusSortParam"] = sortOrder == "status" ? "status_desc" : "status";

            switch (sortOrder)
            {
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
                    ordersQuery = ordersQuery.OrderBy(o => o.OrderDate);
                    break;
            }

            var orders = await ordersQuery.ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                                .Include(o => o.OrderDetails)
                                    .ThenInclude(od => od.Book)
                                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == _userManager.GetUserId(User));

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


        public IActionResult EditProfile()
        {
            return View();
        }

        public IActionResult ChangeUsername()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUsername(ChangeUsernameViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return View("Error");
            }

            var result = await _userManager.SetUserNameAsync(user, model.NewUsername);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction(nameof(EditProfile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        public IActionResult ChangeEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return View("Error");
            }

            var setEmailResult = await _userManager.SetEmailAsync(user, model.NewEmail);
            if (setEmailResult.Succeeded)
            {
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);
                var callbackUrl = Url.Action("ConfirmEmailChange", "Account",
                    new { userId = user.Id, email = model.NewEmail, code = code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(model.NewEmail, "Confirm your email change",
                    $"Please confirm your email change by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                return RedirectToPage("/Account/ConfirmEmailChange", new { email = model.NewEmail });
            }

            foreach (var error in setEmailResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return View("Error");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (changePasswordResult.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction(nameof(EditProfile));
            }

            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }



    }
}
