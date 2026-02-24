using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Controllers;

[Route("[controller]")]
public class LogsController : Controller
{
    private readonly AppDbContext _context;

    public LogsController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin()
    {
        var isAdmin = HttpContext.Session.GetString("IsAdmin");
        return isAdmin == "True";
    }

    [HttpGet]
    [Route("")]
    [Route("Logins")]
    public async Task<IActionResult> Logins(int page = 1)
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return Forbid();

        int pageSize = 50;
        var totalCount = await _context.LogAccess.CountAsync();
        var logins = await _context.LogAccess
            .OrderByDescending(l => l.LoginTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Logins = logins;
        ViewBag.TotalCount = totalCount;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (totalCount + pageSize - 1) / pageSize;

        return View();
    }
}
