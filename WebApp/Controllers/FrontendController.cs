using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

[Route("[controller]")]
public class FrontendController : Controller
{
    private readonly AppDbContext _context;

    public FrontendController(AppDbContext context)
    {
        _context = context;
    }

    private bool HasFrontendAccess()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return false;

        var userIdInt = int.Parse(userId);
        var hasAccess = _context.UserAccesses
            .Include(ua => ua.AccessRule)
            .Any(ua => ua.UserId == userIdInt && ua.AccessRule != null && ua.AccessRule.Name == "frontend_access");

        return hasAccess || HttpContext.Session.GetString("IsAdmin") == "True";
    }

    [HttpGet]
    [Route("")]
    [Route("Index")]
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!HasFrontendAccess())
            return Forbid();

        ViewBag.Username = HttpContext.Session.GetString("Username");
        return View();
    }
}
