using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("")]
    [Route("Login")]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("UserId") != null)
            return RedirectToAction("Index", "Home");

        return View();
    }
[Route("Login")]
    
    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await LogUserLogin(ip, username ?? "empty", false);
            ViewBag.Error = "Логин и пароль обязательны";
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            await LogUserLogin(ip, username, false);
            ViewBag.Error = "Неверные учетные данные";
            return View();
        }

        await LogUserLogin(ip, username, true);

        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

        // Если суперадмин - показываем выбор, иначе сразу на фронт
        if (user.IsAdmin)
            return RedirectToAction("Choice");
        else
            return RedirectToAction("Index", "Frontend");
    }

    private async Task LogUserLogin(string ip, string username, bool successful)
    {
        var log = new LogAccess
        {
            Ip = ip,
            LoginTime = DateTime.UtcNow,
            Username = username,
            Successful = successful
        };
        _context.LogAccess.Add(log);
        await _context.SaveChangesAsync();
    }

    [Route("Choice")]
    [HttpGet]
    public IActionResult Choice()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login");

        var isAdmin = HttpContext.Session.GetString("IsAdmin");
        if (isAdmin != "True")
            return RedirectToAction("Index", "Frontend");

        return View();
    }

    [Route("Logout")]
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
