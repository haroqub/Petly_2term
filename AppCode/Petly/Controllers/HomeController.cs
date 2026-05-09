using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Petly.Business.Services;
using Petly.Models;
using Petly.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace Petly.Controllers;

public class HomeController : Controller
{
    private const string SystemAdminEmail = "admin@petly.com";

    private readonly PetService _petService;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public HomeController(
        PetService petService,
        IWebHostEnvironment environment,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _petService = petService;
        _environment = environment;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(string typeFilter, string searchTerm)
    {
        var pets = await _petService.GetPetsAsync(typeFilter, searchTerm);

        ViewBag.TypeFilter = typeFilter ?? "Усі";
        ViewBag.SearchTerm = searchTerm ?? "";

        var user = await _userManager.GetUserAsync(User);
        if (user != null && User.IsInRole("user"))
        {
            var favoriteIds = await _context.Favorites
                .Where(f => f.UserId == user.Id)
                .Select(f => f.PetId)
                .ToListAsync();

            ViewBag.FavoriteIds = favoriteIds;
        }
        else
        {
            ViewBag.FavoriteIds = new List<int>();
        }

        return View(pets);
    }

    public async Task<IActionResult> About()
    {
        var model = await LoadAboutPageAsync();
        model.CanEdit = await CanEditAboutPageAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditAbout()
    {
        if (!await CanEditAboutPageAsync())
        {
            return Forbid();
        }

        return View(await LoadAboutPageAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAbout(AboutPageViewModel model)
    {
        if (!await CanEditAboutPageAsync())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            model.CanEdit = true;
            return View(model);
        }

        NormalizeAboutPage(model);
        await SaveAboutPageAsync(model);
        TempData["Success"] = "Дані сторінки \"Про Нас\" оновлено.";
        return RedirectToAction(nameof(About));
    }

    public IActionResult Needs()
    {
        return RedirectToAction("Index", "Needs");
    }

    public IActionResult Adoption()
    {
        return RedirectToAction("Index", "Adoption");
    }

    private async Task<bool> CanEditAboutPageAsync()
    {
        if (!User.IsInRole("system_admin"))
        {
            return false;
        }

        var user = await _userManager.GetUserAsync(User);
        return string.Equals(user?.Email, SystemAdminEmail, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<AboutPageViewModel> LoadAboutPageAsync()
    {
        var path = GetAboutPagePath();
        if (!System.IO.File.Exists(path))
        {
            return new AboutPageViewModel();
        }

        await using var stream = System.IO.File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<AboutPageViewModel>(stream) ?? new AboutPageViewModel();
    }

    private async Task SaveAboutPageAsync(AboutPageViewModel model)
    {
        var path = GetAboutPagePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = System.IO.File.Create(path);
        await JsonSerializer.SerializeAsync(stream, model, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private string GetAboutPagePath()
    {
        return Path.Combine(_environment.ContentRootPath, "App_Data", "about-page.json");
    }

    private static void NormalizeAboutPage(AboutPageViewModel model)
    {
        model.Eyebrow = model.Eyebrow.Trim();
        model.HeroTitle = model.HeroTitle.Trim();
        model.HeroText = model.HeroText.Trim();
        model.ContentTitle = model.ContentTitle.Trim();
        model.ContentText = model.ContentText.Trim();
        model.ContactsTitle = model.ContactsTitle.Trim();
        model.Contacts = model.Contacts.Trim();
        model.CanEdit = false;
    }
}