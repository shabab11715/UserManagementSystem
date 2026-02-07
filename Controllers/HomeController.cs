using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task4.Data;
using Task4.Filters;

namespace Task4.Controllers
{
    [RequireVerifiedUser]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q = null, string? status = "all", int page = 1, int pageSize = 10)
        {
            q = (q ?? "").Trim().ToLower();
            status = (status ?? "all").Trim().ToLower();

            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5;
            if (pageSize > 50) pageSize = 50;

            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(u => u.Email.ToLower().Contains(q));
            }

            if (status == "active")
                query = query.Where(u => !u.IsBlocked && u.IsEmailVerified);
            else if (status == "blocked")
                query = query.Where(u => u.IsBlocked);
            else if (status == "unverified")
                query = query.Where(u => !u.IsEmailVerified);

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.LastLoginAt == null)
                .ThenByDescending(u => u.LastLoginAt)
                .ThenByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Query = q;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> BlockSelected(int[] selectedIds)
        {
            var count = 0;

            if (selectedIds != null && selectedIds.Length > 0)
            {
                var users = await _db.Users.Where(u => selectedIds.Contains(u.Id)).ToListAsync();
                foreach (var u in users) u.IsBlocked = true;
                count = users.Count;
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = count > 0 ? $"{count} user(s) blocked." : "No users selected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UnblockSelected(int[] selectedIds)
        {
            var count = 0;

            if (selectedIds != null && selectedIds.Length > 0)
            {
                var users = await _db.Users.Where(u => selectedIds.Contains(u.Id)).ToListAsync();
                foreach (var u in users) u.IsBlocked = false;
                count = users.Count;
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = count > 0 ? $"{count} user(s) unblocked." : "No users selected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelected(int[] selectedIds)
        {
            var count = 0;

            if (selectedIds != null && selectedIds.Length > 0)
            {
                var users = await _db.Users.Where(u => selectedIds.Contains(u.Id)).ToListAsync();
                count = users.Count;
                _db.Users.RemoveRange(users);
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = count > 0 ? $"{count} user(s) deleted." : "No users selected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUnverified()
        {
            var users = await _db.Users.Where(u => !u.IsEmailVerified).ToListAsync();
            var count = users.Count;

            if (count > 0)
            {
                _db.Users.RemoveRange(users);
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = count > 0 ? $"{count} unverified user(s) deleted." : "No unverified users found.";
            return RedirectToAction(nameof(Index));
        }
    }
}
