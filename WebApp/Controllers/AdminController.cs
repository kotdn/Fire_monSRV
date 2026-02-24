using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

[Route("[controller]")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
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
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return Forbid();

        // Загружаем пункты меню для админки
        var menuItems = await _context.MenuItems
            .Where(m => m.ParentGroup == "Администрирование" || m.ParentGroup == "Управление Юзарями" || m.ParentGroup == "Логи")
            .OrderBy(m => m.ParentGroup)
            .ThenBy(m => m.Order)
            .ToListAsync();

        ViewBag.MenuItems = menuItems.GroupBy(m => m.ParentGroup).ToList();
        return View();
    }

    [HttpPost]
    [Route("AddMessage")]
    public async Task<IActionResult> AddMessage(string message)
    {
        if (!IsAdmin())
            return Forbid();

        if (string.IsNullOrEmpty(message))
        {
            ViewBag.Error = "Сообщение не может быть пустым";
            return RedirectToAction("Index");
        }

        var msg = new Message { TxtMes = message };
        _context.Messages.Add(msg);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Route("UpdateMessage/{id}")]
    public async Task<IActionResult> UpdateMessage(int id, string message)
    {
        if (!IsAdmin())
            return Forbid();

        var msg = await _context.Messages.FindAsync(id);
        if (msg == null)
            return NotFound();

        msg.TxtMes = message;
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Route("DeleteMessage/{id}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        if (!IsAdmin())
            return Forbid();

        var msg = await _context.Messages.FindAsync(id);
        if (msg == null)
            return NotFound();

        _context.Messages.Remove(msg);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}
