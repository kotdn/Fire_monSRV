using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using BCrypt.Net;

namespace WebApp.Controllers;

[Route("[controller]")]
public class UsersController : Controller
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin()
    {
        var isAdmin = HttpContext.Session.GetString("IsAdmin");
        return isAdmin == "True";
    }

    [HttpGet]
    [Route("AddUser")]
    public IActionResult AddUser()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return Forbid();

        return View();
    }

    [HttpPost]
    [Route("AddUser")]
    public async Task<IActionResult> AddUser(string username, string password, string passwordConfirm, bool isAdmin = false)
    {
        if (!IsAdmin())
            return Forbid();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Имя пользователя и пароль не могут быть пустыми";
            return View();
        }

        if (password != passwordConfirm)
        {
            ViewBag.Error = "Пароли не совпадают";
            return View();
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (existingUser != null)
        {
            ViewBag.Error = "Пользователь с таким именем уже существует";
            return View();
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var newUser = new User
        {
            Username = username,
            Password = hashedPassword,
            IsAdmin = isAdmin
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Assign default permission (view_messages) to new user
        var viewMessagesRule = await _context.AccessRules.FirstOrDefaultAsync(r => r.Name == "view_messages");
        if (viewMessagesRule != null)
        {
            var userAccess = new UserAccess { UserId = newUser.Id, AccessRuleId = viewMessagesRule.Id };
            _context.UserAccesses.Add(userAccess);
            await _context.SaveChangesAsync();
        }

        ViewBag.Success = $"Пользователь '{username}' успешно создан";
        return View();
    }

    [HttpGet]
    [Route("ManageRights")]
    public async Task<IActionResult> ManageRights()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return Forbid();

        var users = await _context.Users
            .Include(u => u.UserAccesses)
            .ThenInclude(ua => ua.AccessRule)
            .ToListAsync();

        var accessRules = await _context.AccessRules.ToListAsync();

        ViewBag.Users = users;
        ViewBag.AccessRules = accessRules;

        return View();
    }

    [HttpGet]
    [Route("List")]
    public async Task<IActionResult> List()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return Forbid();

        var users = await _context.Users
            .Include(u => u.UserAccesses)
            .ThenInclude(ua => ua.AccessRule)
            .ToListAsync();

        ViewBag.Users = users;

        return View();
    }

    [HttpPost]
    [Route("UpdateUserAccess")]
    public async Task<IActionResult> UpdateUserAccess(int userId, int accessRuleId, bool hasAccess)
    {
        if (!IsAdmin())
            return Forbid();

        var userAccess = await _context.UserAccesses
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AccessRuleId == accessRuleId);

        if (hasAccess && userAccess == null)
        {
            // Add access
            var newAccess = new UserAccess { UserId = userId, AccessRuleId = accessRuleId };
            _context.UserAccesses.Add(newAccess);
        }
        else if (!hasAccess && userAccess != null)
        {
            // Remove access
            _context.UserAccesses.Remove(userAccess);
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost]
    [Route("ResetPassword/{id}")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        if (!IsAdmin())
            return Forbid();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        // Generate random password
        var newPassword = GenerateRandomPassword(12);
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
        
        user.Password = hashedPassword;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, password = newPassword, username = user.Username });
    }

    [HttpPost]
    [Route("SetPassword")]
    public async Task<IActionResult> SetPassword(int userId, string newPassword)
    {
        if (!IsAdmin())
            return Forbid();

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            return BadRequest(new { success = false, message = "Пароль должен содержать минимум 4 символа" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.Password = hashedPassword;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, username = user.Username });
    }

    private string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
