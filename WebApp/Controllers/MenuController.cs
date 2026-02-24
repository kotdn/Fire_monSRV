using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

[Route("[controller]")]
public class MenuController : Controller
{
    private readonly AppDbContext _context;

    public MenuController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin()
    {
        var isAdmin = HttpContext.Session.GetString("IsAdmin");
        return isAdmin == "True";
    }

    private string GenerateSlug(string text)
    {
        // Транслитерация русских букв
        var translitMap = new Dictionary<char, string>
        {
            {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"},
            {'е', "e"}, {'ё', "yo"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
            {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
            {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"},
            {'у', "u"}, {'ф', "f"}, {'х', "h"}, {'ц', "ts"}, {'ч', "ch"},
            {'ш', "sh"}, {'щ', "sch"}, {'ъ', ""}, {'ы', "y"}, {'ь', ""},
            {'э', "e"}, {'ю', "yu"}, {'я', "ya"},
            {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "G"}, {'Д', "D"},
            {'Е', "E"}, {'Ё', "Yo"}, {'Ж', "Zh"}, {'З', "Z"}, {'И', "I"},
            {'Й', "Y"}, {'К', "K"}, {'Л', "L"}, {'М', "M"}, {'Н', "N"},
            {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"}, {'Т', "T"},
            {'У', "U"}, {'Ф', "F"}, {'Х', "H"}, {'Ц', "Ts"}, {'Ч', "Ch"},
            {'Ш', "Sh"}, {'Щ', "Sch"}, {'Ъ', ""}, {'Ы', "Y"}, {'Ь', ""},
            {'Э', "E"}, {'Ю', "Yu"}, {'Я', "Ya"}
        };

        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (translitMap.ContainsKey(c))
                sb.Append(translitMap[c]);
            else
                sb.Append(c);
        }

        var slug = sb.ToString().ToLower();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return "/" + slug;
    }

    [HttpGet]
    [Route("AddItem")]
    public IActionResult AddItem()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return Forbid();

        // Предопределенные группы меню
        ViewBag.Groups = new List<string>
        {
            "Администрирование",
            "Управление Юзарями",
            "Логи"
        };

        return View();
    }

    [HttpPost]
    [Route("AddItem")]
    public async Task<IActionResult> AddItem(string name, string parentGroup, int order = 0)
    {
        if (!IsAdmin())
            return Forbid();

        ViewBag.Groups = new List<string>
        {
            "Администрирование",
            "Управление Юзарями",
            "Логи"
        };

        if (string.IsNullOrWhiteSpace(name))
        {
            ViewBag.Error = "Название не может быть пустым";
            return View();
        }

        // Автоматическая генерация URL из названия
        var url = GenerateSlug(name);

        var menuItem = new MenuItem
        {
            Name = name,
            Url = url,
            ParentGroup = parentGroup,
            Order = order,
            IsActive = true
        };

        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync();

        ViewBag.Success = $"Пункт меню '{name}' успешно создан с URL: {url}";
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

        var allMenuItems = await _context.MenuItems
            .OrderBy(m => m.Order)
            .ToListAsync();

        // Разделяем на админку и фронт
        var adminItems = allMenuItems
            .Where(m => m.ParentGroup == "Администрирование" || m.ParentGroup == "Управление Юзарями" || m.ParentGroup == "Логи")
            .GroupBy(m => m.ParentGroup)
            .OrderBy(g => g.Key == "Администрирование" ? 0 : g.Key == "Управление Юзарями" ? 1 : 2)
            .ToList();

        var frontendItems = allMenuItems
            .Where(m => m.ParentGroup != "Администрирование" && m.ParentGroup != "Управление Юзарями" && m.ParentGroup != "Логи")
            .GroupBy(m => m.ParentGroup)
            .ToList();

        ViewBag.AdminItems = adminItems;
        ViewBag.FrontendItems = frontendItems;
        return View();
    }

    [HttpPost]
    [Route("Delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin())
            return Forbid();

        var menuItem = await _context.MenuItems.FindAsync(id);
        if (menuItem != null)
        {
            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("List");
    }
}
