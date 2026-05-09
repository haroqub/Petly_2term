using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Petly.Business.Services;
using Petly.Models;
using Petly.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace Petly.Controllers;

[Authorize]
public class FavoritesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public FavoritesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        return await _userManager.GetUserAsync(User);
    }

[HttpPost]
public async Task<IActionResult> Add(int petId)
{
    var user = await GetCurrentUserAsync();
    if (user == null) return RedirectToAction("Login", "Account");

    var pet = await _context.Pets
        .FirstOrDefaultAsync(p => p.PetId == petId);

    if (pet == null) return NotFound();

    var exists = await _context.Favorites
        .AnyAsync(f => f.UserId == user.Id && f.PetId == petId);

    if (!exists)
    {
        _context.Favorites.Add(new Favorite
        {
            UserId = user.Id,
            PetId = petId
        });

        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(
            user.Id,
            $"Обране: {pet.PetName}",
            $"Ви додали в обране {pet.PetName}"
        );
    }

    return RedirectToAction("Index");
}

    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("Role");

        if (role == "shelter_admin" || role == "system_admin")
        {
            return Forbid();
        }

        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        var favorites = await _context.Favorites
            .Include(f => f.Pet)
            .Where(f => f.UserId == user.Id)
            .ToListAsync();

        return View(favorites);
    }

    public async Task<IActionResult> Remove(int id)
{
    var user = await GetCurrentUserAsync();
    if (user == null) return RedirectToAction("Login", "Account");

    var fav = await _context.Favorites
        .Include(f => f.Pet)
        .FirstOrDefaultAsync(f => f.Id == id && f.UserId == user.Id);

    if (fav != null)
    {
        var petName = fav.Pet?.PetName;

        _context.Favorites.Remove(fav);
        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(
            user.Id,
            $"Обране: {petName}",
            $"Ви видалили з обраного {petName}"
        );
    }

    return RedirectToAction("Index");
}
}